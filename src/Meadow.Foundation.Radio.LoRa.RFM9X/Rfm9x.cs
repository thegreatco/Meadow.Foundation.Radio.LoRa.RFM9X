using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using Meadow.Foundation.Radio.LoRaWan;
using Meadow.Hardware;
using Meadow.Logging;
using Meadow.Units;

using static Meadow.Foundation.Radio.LoRa.RFM9X.LoRaRegisters;

namespace Meadow.Foundation.Radio.LoRa.RFM9X
{
    public partial class Rfm9X : ILoRaRadio
    {
        public class OnDataReceivedEventArgs : EventArgs
        {
            public Envelope Envelope { get; set; }
        }

        public class OnDataTransmittedEventArgs : EventArgs;

        public delegate void OnDataReceivedEventHandler(object sender, OnDataReceivedEventArgs e);
        public event OnDataReceivedEventHandler? OnReceived;

        public delegate void OnDataTransmittedEventHandler(object sender, OnDataTransmittedEventArgs e);
        public event OnDataTransmittedEventHandler? OnTransmitted;

        private const int AddressHeaderLength = 1;
        private const double RfMidBandThreshold = 525000000.0;
        private readonly Logger _logger;

        private TaskCompletionSource<Envelope>? _receiveCompleteTask;
        private TaskCompletionSource<bool>? _transmitCompleteTask;

        private readonly SemaphoreSlim _opSemaphore = new(1);
        private readonly SemaphoreSlim _fifoSemaphore = new(1);

        private readonly IDigitalInterruptPort _dio0;
        private readonly IDigitalInterruptPort? _dio1;
        private readonly IDigitalInterruptPort? _dio2;
        private readonly IDigitalInterruptPort? _dio3;
        private readonly IDigitalInterruptPort? _dio4;
        private readonly IDigitalInterruptPort? _dio5;

        private readonly IDigitalOutputPort _resetPin;
        private readonly IDigitalOutputPort _chipSelect;
#if !CUSTOM_SPI
        private readonly ISpiCommunications _comms;
#endif
        private readonly Rfm9XConfiguration _config;
        private readonly LoRaFrequencyManager _frequencyManager;

        private readonly Frequency _lowFrequencyMax = new(525, Frequency.UnitType.Megahertz);
        private const int RssiHighFrequencyAdjust = -157;
        private const int RssiLowFrequencyAdjust = -164;

        public byte[] DeviceAddress { get; }

        public Rfm9X(Logger logger,
                     Rfm9XConfiguration config)
        {
            _logger = logger;
#if !CUSTOM_SPI
            _comms = new SpiCommunications(config.SpiBus, _chipSelect, config.SpiFrequency, writeBufferSize: 256);
#endif
            _chipSelect = config.Device.CreateDigitalOutputPort(config.ChipSelectPin);
            _resetPin = config.Device.CreateDigitalOutputPort(config.ResetPin, true);
            _dio0 = config.Device.CreateDigitalInterruptPort(config.Dio0, InterruptMode.EdgeRising);
            _dio0.Changed += TransceiverInterrupt;

            // TODO: these interrupts seem to fire immediately?
            #region Optional Interrupts
            if (config.Dio1 != null)
            {
                _dio1 = config.Device.CreateDigitalInterruptPort(config.Dio1, InterruptMode.EdgeRising);
                _dio1.Changed += TransceiverInterrupt;
            }

            if (config.Dio2 != null)
            {
                _dio2 = config.Device.CreateDigitalInterruptPort(config.Dio2, InterruptMode.EdgeRising);
                _dio2.Changed += TransceiverInterrupt;
            }

            if (config.Dio3 != null)
            {
                _dio3 = config.Device.CreateDigitalInterruptPort(config.Dio3, InterruptMode.EdgeRising);
                _dio3.Changed += TransceiverInterrupt;
            }

            if (config.Dio4 != null)
            {
                _dio4 = config.Device.CreateDigitalInterruptPort(config.Dio4, InterruptMode.EdgeRising);
                _dio4.Changed += TransceiverInterrupt;
            }

            if (config.Dio5 != null)
            {
                _dio5 = config.Device.CreateDigitalInterruptPort(config.Dio5, InterruptMode.EdgeRising);
                _dio5.Changed += TransceiverInterrupt;
            }
            #endregion

            _config = config;
            DeviceAddress = config.DeviceAddress;
            _frequencyManager = new LoRaFrequencyManager(config.Channels);
            OnTransmitted += (sender, args) => _transmitCompleteTask?.SetResult(true);
            OnReceived += (sender, args) => _receiveCompleteTask?.SetResult(args.Envelope);
        }

        public async ValueTask Initialize()
        {
            _logger.Debug("Initializing Modem");
            await ResetChip();
            var val = ReadRegister(Register.Version);
            if (val != 0x12)
                throw new InvalidOperationException($"Invalid version {val}");
            // more powah!
            WriteRegister(Register.PaConfig, 0xFF);
            WriteRegister(Register.PaRamp, (byte)((ReadRegister(Register.PaRamp) & 0xF0) | 0x80));

            SetMode(RegOpMode.OpMode.Sleep);
            _logger.Debug("Initialization complete");
        }

        private void SetFrequency(Frequency frequency)
        {
            var registerFrequency = Convert.ToInt64(((uint)frequency.Hertz) / (32000000.0 / 524288.0));
            var bytes = BitConverter.GetBytes(registerFrequency);
            WriteRegister(Register.FrfMsb, bytes[2]);
            WriteRegister(Register.FrfMid, bytes[1]);
            WriteRegister(Register.FrfLsb, bytes[0]);
        }

        private Frequency GetFrequency()
        {
            var msb = ReadRegister(Register.FrfMsb);
            var mid = ReadRegister(Register.FrfMid);
            var lsb = ReadRegister(Register.FrfLsb);
            var frequency = ((msb << 16) | (mid << 8) | lsb) * (32000000.0 / 524288.0);
            return new Frequency(frequency);
        }

        internal async ValueTask ResetChip()
        {
            _logger.Trace("Resetting chip");
            _resetPin.State = false;
            await Task.Delay(10).ConfigureAwait(false);
            _resetPin.State = true;
            await Task.Delay(1000).ConfigureAwait(false);
            _logger.Trace("Reset complete");
        }

        public async ValueTask Send(byte[] addressBytes, byte[] messageBytes)
        {
            var payload = ArrayPool<byte>.Shared.Rent(AddressHeaderLength + addressBytes.Length + DeviceAddress.Length + messageBytes.Length);
            var toAddressLength = (byte)(addressBytes.Length << 4);
            var fromAddressLength = (byte)DeviceAddress.Length;
            payload[0] = (byte)(toAddressLength | fromAddressLength);
            var payloadIndex = 1;
            Array.Copy(addressBytes, 0, payload, payloadIndex, addressBytes.Length);
            payloadIndex += addressBytes.Length;
            Array.Copy(DeviceAddress, 0, payload, payloadIndex, DeviceAddress.Length);
            payloadIndex += DeviceAddress.Length;
            Array.Copy(messageBytes, 0, payload, payloadIndex, messageBytes.Length);
            payloadIndex += messageBytes.Length;
            await Send(payload[..payloadIndex]);
            ArrayPool<byte>.Shared.Return(payload);
        }

        public async ValueTask Send(byte[] payload)
        {
            await SendInternal(payload);
            SetMode(RegOpMode.OpMode.StandBy);
        }

        public async ValueTask<Envelope> SendAndReceive(byte[] messagePayload, TimeSpan timeout)
        {
            await _opSemaphore.WaitAsync();
            try
            {
                // Create the TCS before we send a message to make sure we hear back properly.
                _receiveCompleteTask = new TaskCompletionSource<Envelope>();
                await SendInternal(messagePayload).ConfigureAwait(false);
                // TODO: need precise timing to wait for the receive window to save power.
                return await ReceiveInternal(timeout);
            }
            finally
            {
                _opSemaphore.Release();
            }
        }

        public async ValueTask<Envelope> Receive(TimeSpan timeout)
        {
            await _opSemaphore.WaitAsync();
            try
            {
                // Create the TCS before we send a message to make sure we hear back properly.
                // This is probably a bad setup...
                _receiveCompleteTask = new TaskCompletionSource<Envelope>();
                return await ReceiveInternal(timeout);
            }
            finally
            {
                _opSemaphore.Release();
            }
        }

        private async ValueTask<Envelope> ReceiveInternal(TimeSpan timeout)
        {
            try
            {
                _logger.Debug($"{DateTime.UtcNow} Waiting for message");
                // Make sure the modem is awake
                SetMode(RegOpMode.OpMode.StandBy);

                // Set the downlink frequency
                SetFrequency(_frequencyManager.DownlinkBaseFrequency);
                // Make sure the bandwidth, error coding rate, and header mode are right
                WriteModemConfig1(_frequencyManager.DownlinkBandwidth, ErrorCodingRate.ECR4_5, ImplicitHeaderMode.Off);
                WriteModemConfig2(SpreadingFactor.SF7, PayloadCrcMode.Off);
                WriteModemConfig3();
                // Maybe we need to set gain to 0x20|0x3?
                //WriteRegister(Register.MaxPayloadLength, 255);
                var detectOptimize = ReadRegister(Register.DetectOptimize) & 0x78 | 0x03;
                if (_frequencyManager.DownlinkBandwidth < LoRaChannels.Bandwidth500kHz)
                {
                    WriteRegister(Register.DetectOptimize, (byte)detectOptimize);
                    WriteRegister(Register.IfFreq1, 0x40);
                    WriteRegister(Register.IfFreq2, 0x40);
                }
                else
                {
                    WriteRegister(Register.DetectOptimize, (byte)(detectOptimize | 0x80));
                }
                //WriteRegister(Register.ReceiveTimeoutLsb, );
                WriteRegister(Register.InvertIq, (byte)(ReadRegister(Register.InvertIq) | (1 << 6)));
                WriteRegister(Register.SyncWord, 0x34);
                WriteRegister(Register.FifoAddressPointer, 0x00);
                WriteRegister(Register.FifoRxByteAddress, 0x00);

                // Put the interrupt pin into receive mode
                WriteRegister(Register.DioMapping1, (byte)(DioMapping1.Dio0RxDone | DioMapping1.Dio1RxTimeout));

                // Set the modem to receive
                SetMode(RegOpMode.OpMode.ReceiveContinuous);

                if (_receiveCompleteTask == null)
                    throw new InvalidOperationException("ReceiveTask cannot be null");

                var envelope = await _receiveCompleteTask.Task;
                return envelope;
            }
            finally
            {
                _receiveCompleteTask = null;
            }
        }

        private async ValueTask SendInternal(byte[] payload)
        {
            _logger.Debug($"{DateTime.UtcNow} Sending message of {payload.Length}bytes");
            byte currentMode;
            do
            {
                _transmitCompleteTask = new TaskCompletionSource<bool>();
                // Wake up the modem so we can fill the FIFO
                SetMode(RegOpMode.OpMode.StandBy);

                // Set the transmit frequency
                var frequency = _frequencyManager.GetNextUplinkFrequency();
                _logger.Debug($"Setting frequency to {frequency.Megahertz}MHz");
                SetFrequency(frequency);

                await Task.Delay(10)
                          .ConfigureAwait(false);

                currentMode = ReadRegister(Register.OpMode);
                _logger.Debug($"Current Mode: {currentMode.ToHexString()}");
            } while (currentMode != 0x81);

            await _fifoSemaphore.WaitAsync()
                                .ConfigureAwait(false);

            try
            {
                WriteModemConfig1(_frequencyManager.UplinkBandwidth, ErrorCodingRate.ECR4_5, ImplicitHeaderMode.Off);
                // The CRC mode should be on, but I'm having trouble with my gateway
                WriteModemConfig2(SpreadingFactor.SF7, PayloadCrcMode.On);
                WriteModemConfig3();
                WriteRegister(Register.SyncWord, 0x34);
                WriteRegister(Register.FifoTransmitBaseAddress, 0x00);
                WriteRegister(Register.FifoAddressPointer, 0x00);
                WriteRegister(Register.PayloadLength, (byte)payload.Length);
                WriteRegister(Register.Fifo, payload);
                WriteRegister(Register.DioMapping1, (byte)DioMapping1.Dio0TxDone);

                // Now actually transmit
                SetMode(RegOpMode.OpMode.Transmit);
                // Now wait for the transmit task to complete
                await _transmitCompleteTask.Task;
                _transmitCompleteTask = null;
            }
            finally
            {
                _fifoSemaphore.Release();
            }
        }

        public void SetMode(RegOpMode.OpMode opMode)
        {
            var mode = new RegOpMode()
            {
                LongRangeMode = true,
                LowFrequencyModeOn = _frequencyManager.UplinkBaseFrequency.Hertz < RfMidBandThreshold,
                Mode = opMode
            };
            WriteRegister(Register.OpMode, mode);
        }

        private void TransceiverInterrupt(object sender, DigitalPortResult e)
        {
            _logger.Debug("Modem interrupt raised");
            // figure out what triggered it
            var flags = ReadRegister(Register.InterruptFlags);
            if ((flags & (byte)InterruptFlags.ReceiveTimeout) == (byte)InterruptFlags.ReceiveTimeout)
            {
                _logger.Debug($"{DateTime.UtcNow} Receive Timeout");
                _receiveCompleteTask?.SetException(new TimeoutException("Transceiver timed out waiting for packet"));
            }
            if ((flags & (byte)InterruptFlagsMask.ReceiveDone) == (byte)InterruptFlags.ReceiveDone)
            {
                HandleReceiveDone();
            }
            if ((flags & (byte)InterruptFlagsMask.TransmitDone) == (byte)InterruptFlags.TransmitDone)
            {
                HandleTransmitDone();
            }
        }

        private void HandleReceiveDone()
        {
            _logger.Debug("Handling Receive Done");
            // TODO: Check the CRC? The modem allegedly does this for us, no?
            _fifoSemaphore.Wait();
            try
            {
                // Check the interrupts
                var interruptFlags = ReadRegister(Register.InterruptFlags);
                if ((interruptFlags & (byte)InterruptFlags.ValidHeader) != (byte)InterruptFlags.ValidHeader)
                {
                    _logger.Debug("Invalid header");
                }
                if ((interruptFlags & (byte)InterruptFlags.PayloadCrcError) == (byte)InterruptFlags.PayloadCrcError)
                {
                    _logger.Debug("CRC Error");
                }
                if ((interruptFlags & (byte)InterruptFlags.ReceiveTimeout) == (byte)InterruptFlags.ReceiveTimeout)
                {
                    _logger.Debug("Receive Timeout, how did we get here??");
                }
                if ((interruptFlags & (byte)InterruptFlags.ReceiveDone) != (byte)InterruptFlags.ReceiveDone)
                {
                    _logger.Debug("Receive Done not set, how did we get here??");
                }
                // Clear all the interrupts
                WriteRegister(Register.InterruptFlags, (byte)InterruptFlags.ReceiveDone);

                var currentAddress = ReadRegister(Register.FifoReceiveCurrentAddress);
                _logger.Trace($"Current address: {currentAddress.ToHexString()}");
                var dataLength = (ReadRegister(Register.ModemConfig1) & (byte)ImplicitHeaderMode.On) == (byte)ImplicitHeaderMode.On
                                             ? ReadRegister(Register.PayloadLength)
                                             : ReadRegister(Register.NumberOfReceivedBytes);
                _logger.Trace($"Data length: {dataLength.ToHexString()}");

                WriteRegister(Register.FifoAddressPointer, currentAddress);
                // Adding 1 because the first byte is throw-away
                var payload = ReadRegister(Register.Fifo, dataLength);

                _logger.Trace($"Data: {payload.ToHexString()}");

                var snr = (int)ReadRegister(Register.LastPacketSnr);
                var packetRssi = (int)ReadRegister(Register.LastPacketRssi);
                var rssi = packetRssi;
                if (GetFrequency() > _lowFrequencyMax)
                {
                    rssi += RssiHighFrequencyAdjust;
                }
                else
                {
                    rssi += RssiLowFrequencyAdjust;
                }

                if (snr < 0)
                {
                    rssi = rssi - (-snr >> 2);
                }
                else if (rssi > -100)
                {
                    rssi += (packetRssi / 15);
                }

                if (PayloadHasMinimumLength(payload) == false)
                    return;

                // I'm pretty sure we have to ignore the first byte here
                var envelope = new Envelope((MessageType)payload[1], payload);
                OnReceived?.Invoke(this, new OnDataReceivedEventArgs { Envelope = envelope });

                SetMode(RegOpMode.OpMode.Sleep);
            }
            finally
            {
                _fifoSemaphore.Release();
            }
        }

        private bool PayloadHasMinimumLength(byte[] payload)
        {
            // TODO: Figure out what this should actually be......
            _logger.Info($"Payload receive size: {payload.Length} bytes");
            return true;
        }

        private void HandleTransmitDone()
        {
            // Clear all the interrupts
            WriteRegister(Register.InterruptFlags, (byte)InterruptFlags.ClearAll);

            _logger.Debug("Handling Transmit done");
            SetMode(RegOpMode.OpMode.Sleep);

            OnTransmitted?.Invoke(this, new OnDataTransmittedEventArgs());
        }
    }
}
