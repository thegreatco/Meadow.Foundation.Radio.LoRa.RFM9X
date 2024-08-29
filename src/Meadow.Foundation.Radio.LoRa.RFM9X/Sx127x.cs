using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using Meadow.Foundation.Radio.LoRa;
using Meadow.Foundation.Radio.LoRa.RFM9X;
using Meadow.Foundation.Radio.LoRaWan;
using Meadow.Hardware;
using Meadow.Logging;
using Meadow.Units;

using static Meadow.Foundation.Radio.LoRa.RFM9X.LoRaRegisters;

using SpreadingFactor = Meadow.Foundation.Radio.LoRa.SpreadingFactor;

namespace Meadow.Foundation.Radio.Sx127X
{
    public partial class Sx127X : LoRaRadio
    {
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
        private readonly Sx172XConfiguration _config;

        private readonly Frequency _lowFrequencyMax = new(525, Frequency.UnitType.Megahertz);
        private const int RssiHighFrequencyAdjust = -157;
        private const int RssiLowFrequencyAdjust = -164;

        #region LoRa Settings
        private Frequency _frequency;
        private Frequency _bandwidth;
        private int _txPower;
        private ErrorCodingRate _errorCodingRate;
        private LoRaRegisters.SpreadingFactor _spreadingFactor;
        private ImplicitHeaderMode _implicitHeaderMode;
        private PayloadCrcMode _payloadCrcMode;
        private byte _syncWord;
        private bool _invertIq;

        public override float MinimumTxPower { get; } = 5;
        public override float MaximumTxPower { get; } = 20;
        #endregion

        public Sx127X(Logger logger, Sx172XConfiguration config)
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
            OnTransmitted += (sender, args) => _transmitCompleteTask?.SetResult(true);
            OnReceived += (sender, args) => _receiveCompleteTask?.SetResult(args.Envelope);
        }

        public override async ValueTask Initialize()
        {
            _logger.Debug("Initializing Modem");
            await ResetChip();
            var val = ReadRegister(Register.Version);
            if (val != 0x12)
                throw new InvalidOperationException($"Invalid version {val}");
            // turn on PA_BOOST pin
            WriteRegister(Register.PaConfig, (byte)((ReadRegister(Register.PaConfig) & 0b01111111) | 0b10000000));
            WriteRegister(Register.PaRamp, (byte)((ReadRegister(Register.PaRamp) & 0xF0) | 0x80));

            SetMode(RegOpMode.OpMode.Sleep);
            _logger.Debug("Initialization complete");
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

        public override ValueTask SetLoRaParameters(LoRaParameters parameters)
        {
            _logger.Debug("Setting LoRa Parameters");
            _logger.Trace(parameters.ToString());
            _frequency = parameters.Frequency;
            _bandwidth = parameters.Bandwidth;
            _errorCodingRate = parameters.CodingRate switch
            {
                CodingRate.Cr45 => ErrorCodingRate.ECR4_5,
                CodingRate.Cr46 => ErrorCodingRate.ECR4_6,
                CodingRate.Cr47 => ErrorCodingRate.ECR4_7,
                CodingRate.Cr48 => ErrorCodingRate.ECR4_8,
                _ => throw new ArgumentOutOfRangeException()
            };

            _spreadingFactor = parameters.SpreadingFactor switch
            {
                SpreadingFactor.Sf6 => LoRaRegisters.SpreadingFactor.SF6,
                SpreadingFactor.Sf7 => LoRaRegisters.SpreadingFactor.SF7,
                SpreadingFactor.Sf8 => LoRaRegisters.SpreadingFactor.SF8,
                SpreadingFactor.Sf9 => LoRaRegisters.SpreadingFactor.SF9,
                SpreadingFactor.Sf10 => LoRaRegisters.SpreadingFactor.SF10,
                SpreadingFactor.Sf11 => LoRaRegisters.SpreadingFactor.SF11,
                SpreadingFactor.Sf12 => LoRaRegisters.SpreadingFactor.SF12,
                _ => throw new ArgumentOutOfRangeException()
            };

            _implicitHeaderMode = parameters.ImplicitHeaderMode ? ImplicitHeaderMode.On : ImplicitHeaderMode.Off;
            _payloadCrcMode = parameters.CrcMode ? PayloadCrcMode.On : PayloadCrcMode.Off;
            _syncWord = parameters.SyncWord;
            _invertIq = parameters.InvertIq;
            _txPower = parameters.TxPower;
            return new ValueTask();
        }

        public override async ValueTask Send(byte[] payload)
        {
            _logger.Debug($"{DateTime.UtcNow} Sending message of {payload.Length}bytes");
            _logger.Trace(payload.ToBase64());
            byte currentMode;
            do
            {
                _transmitCompleteTask = new TaskCompletionSource<bool>();
                // Wake up the modem so we can fill the FIFO
                SetMode(RegOpMode.OpMode.StandBy);

                await Task.Delay(10).ConfigureAwait(false);

                currentMode = ReadRegister(Register.OpMode);
                _logger.Debug($"Current Mode: {currentMode.ToHexString()}");
            } while ((currentMode & 0b00000111) != 0b00000001);

            await _fifoSemaphore.WaitAsync()
                                .ConfigureAwait(false);

            try
            {
                // Set the transmit frequency
                SetFrequency(_frequency);
                SetTxPower(_txPower);
                WriteModemConfig1(_bandwidth, _errorCodingRate, _implicitHeaderMode);
                // The CRC mode should be on, but I'm having trouble with my gateway
                WriteModemConfig2(_spreadingFactor, _payloadCrcMode);
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
                SetMode(RegOpMode.OpMode.StandBy);
                _fifoSemaphore.Release();
            }
        }

        public override async ValueTask<Envelope> Receive(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);
            await _opSemaphore.WaitAsync();
            try
            {
                // Create the TCS before we send a message to make sure we hear back properly.
                // This is probably a bad setup...
                _receiveCompleteTask = new TaskCompletionSource<Envelope>();
                cts.Token.Register(() => _receiveCompleteTask.SetException(new TimeoutException("Receive timed out")));
                _logger.Debug($"{DateTime.UtcNow} Waiting for message");
                // Make sure the modem is awake
                SetMode(RegOpMode.OpMode.StandBy);

                // Set the downlink frequency
                SetFrequency(_frequency);
                // Make sure the bandwidth, error coding rate, and header mode are right
                WriteModemConfig1(_bandwidth, _errorCodingRate, _implicitHeaderMode);
                WriteModemConfig2(_spreadingFactor, _payloadCrcMode);
                WriteModemConfig3();
                // Maybe we need to set gain to 0x20|0x3?
                //WriteRegister(Register.MaxPayloadLength, 255);
                var detectOptimize = ReadRegister(Register.DetectOptimize) & 0x78 | 0x03;
                if (_bandwidth < LoRaChannels.Bandwidth500kHz)
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
                if (_invertIq)
                    WriteRegister(Register.InvertIq, (byte)(ReadRegister(Register.InvertIq) | (1 << 6)));
                else
                    WriteRegister(Register.InvertIq, (byte)(ReadRegister(Register.InvertIq) & ~(1 << 6)));
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
                SetMode(RegOpMode.OpMode.Sleep);
                _receiveCompleteTask = null;
                _opSemaphore.Release();
            }
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

                // TODO: fix SNR calculation as it's more complex than just reading the value
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
                var envelope = new Envelope(payload, snr);
                OnReceivedHandler(new RadioDataReceived(envelope));

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
            OnTransmittedHandler(new RadioDataTransmitted());
        }
    }
}
