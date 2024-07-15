using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

using Meadow.Foundation.Radio.LoRa;
using Meadow.Logging;

namespace Meadow.Foundation.Radio.LoRaWan
{
    // TODO: We need to have a method for getting battery info, the devStatusReq/devStatusAns commands require battery info
    public abstract class LoRaWanNetwork(Logger logger, ILoRaRadio radio, LoRaWanParameters parameters)
    {
        private static TimeSpan _sleepOffset = new(500);
        private static readonly JoinEui DefaultAppEui = new([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        private readonly JoinEui _appEui = parameters.AppEui ?? DefaultAppEui;
        protected OtaaSettings? Settings;
        protected IPacketFactory? PacketFactory;

        public async ValueTask Initialize()
        {
            await radio.Initialize().ConfigureAwait(false);

            var settings = await OtaaSettings.LoadSettings();
            if (settings == null)
            {
            retry:
                try
                {
                    logger.Debug("Activating new device");
                    var devNonce = DeviceNonce.GenerateNewNonce();
                    var joinResponse = await SendJoinRequest(devNonce)
                                           .ConfigureAwait(false);

                    logger.Debug("Device activated successfully");
                    settings = new OtaaSettings(parameters.AppKey, joinResponse, devNonce);
                    await settings.SaveSettings().ConfigureAwait(false);
                    logger.Debug("Wrote settings to file");
                }
                catch (TimeoutException tex)
                {
                    logger.Error(tex, "Join request timed out");
                    // This is bad, should probably not blindly loop forever.
                    goto retry;
                }
            }
            Settings = settings;
            PacketFactory = new PacketFactory(Settings);
            logger.Debug("Settings");
            logger.Debug(settings.ToString());

            radio.OnReceived += Radio_OnReceived;
            // This is just an insanity check
            ThrowIfSettingsNull();
        }

        private void Radio_OnReceived(object sender, RadioDataReceived e)
        {
            // TODO: Handle unsolicited downlink messages
        }

        private TimeSpan GetSleepTime(TimeSpan rxDelay) => rxDelay - _sleepOffset;

        public async ValueTask SendMessage(byte[] payload)
        {
            ThrowIfSettingsNull();
            var message = PacketFactory!.CreateUnconfirmedDataUpMessage(payload);

            var uplinkFrequency = parameters.FrequencyManager.GetNextUplinkFrequency();
            var loRaParameters = new LoRaParameters(uplinkFrequency.Frequency, uplinkFrequency.Bandwidth, parameters.Plan.CodingRate, parameters.Plan.UpstreamSpreadingFactor, false, true, false);
            await radio.SetLoRaParameters(loRaParameters);
            logger.Debug("Sending message");
            await radio.Send(message.PhyPayload).ConfigureAwait(false);

            Settings!.IncUplinkFrameCounter();

            try
            {
                // TODO: This needs to be made background async, so we can hand off control to the app and still process the incoming data
                await Task.Delay(GetSleepTime(parameters.Plan.ReceiveDelay1)).ConfigureAwait(false);
                // Wait for a downlink message just in case
                var downlinkFrequency = parameters.FrequencyManager.GetDownlinkFrequency();
                loRaParameters = new LoRaParameters(downlinkFrequency.Frequency, downlinkFrequency.Bandwidth, parameters.Plan.CodingRate, parameters.Plan.UpstreamSpreadingFactor, false, true, false);
                await radio.SetLoRaParameters(loRaParameters);

                var downlinkData = await radio.Receive(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

                await HandleReceivedPacket(downlinkData).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                logger.Error("No downlink received in window");
            }
            logger.Debug("Finished sending message");
        }

        protected async ValueTask<JoinAccept> SendJoinRequest(DeviceNonce devNonce)
        {
            logger.Info("Sending join-request");
            var request = new JoinRequest(parameters.AppKey, _appEui, parameters.DevEui, devNonce);
            var joinFrequency = parameters.FrequencyManager.GetJoinFrequency();

            // TODO: Sort out the data rate for each frequency, DR0 for 0..64, DR4 for 65..72
            var loRaParameters = new LoRaParameters(joinFrequency.Frequency, joinFrequency.Bandwidth, parameters.Plan.CodingRate, SpreadingFactor.Sf10, false, true, false);
            await radio.SetLoRaParameters(loRaParameters);
            await radio.Send(request.PhyPayload);
            logger.Debug("join-request sent, waiting for response");

            // Wait for the join-accept message
            await Task.Delay(GetSleepTime(parameters.Plan.JoinAcceptDelay1)).ConfigureAwait(false);

            var receiveFrequency = parameters.FrequencyManager.GetDownlinkFrequency();
            // TODO: The data rate is what drives the bandwidth and spreading factor, these are hard coded for join-accept messages to DR0 for 0..63 and DR4 for 64..71
            loRaParameters = new LoRaParameters(receiveFrequency.Frequency, receiveFrequency.Bandwidth, parameters.Plan.CodingRate, SpreadingFactor.Sf10, false, true, true);
            await radio.SetLoRaParameters(loRaParameters);
            var res = await radio.Receive(TimeSpan.FromSeconds(10));
            logger.Debug($"Join accept received: {Convert.ToBase64String(res.MessagePayload)}");
            return JoinAccept.FromPhy(parameters.AppKey, res.MessagePayload);
        }

        protected async ValueTask HandleReceivedPacket(Envelope envelope)
        {
            ThrowIfSettingsNull();
            await HandleMacCommands(envelope).ConfigureAwait(false);
            Settings!.IncDownlinkFrameCounter();
            await Task.Yield();
        }

        protected bool ValidateDownlinkMessage(DataMessage message)
        {
            ThrowIfSettingsNull();
            if (message.FrameHeader.DeviceAddress != Settings!.DeviceAddress)
            {
                logger.Trace("Received downlink message with incorrect device address");
                return false;
            }
            if (message.FCount < Settings.DownlinkFrameCounter)
            {
                logger.Trace("Received duplicate downlink message");
                return false;
            }
            return true;
        }

        protected async ValueTask CheckLinkStatus()
        {
            ThrowIfSettingsNull();
            var message = PacketFactory!.CreateLinkCheckRequestMessage();
            var uplinkFrequency = parameters.FrequencyManager.GetNextUplinkFrequency();
            var loRaParameters = new LoRaParameters(uplinkFrequency.Frequency, uplinkFrequency.Bandwidth, parameters.Plan.CodingRate, parameters.Plan.UpstreamSpreadingFactor, false, true, false);
            await radio.SetLoRaParameters(loRaParameters);
            await radio.Send(message.PhyPayload);
            // TODO: this setting needs to get parsed from the join-accept message.
            await Task.Delay(4000).ConfigureAwait(false);
            var response = await radio.Receive(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            await HandleMacCommands(response);
        }

        protected async ValueTask<DataMessage?> HandleMacCommands(Envelope envelope)
        {
            if (PacketFactory == null)
                throw new PacketFactoryNullException();
            var packet = PacketFactory!.Parse(envelope.MessagePayload);
            if (packet is not DataMessage)
                throw new InvalidOperationException("Expected DataMessage");
            var dataMessage = (DataMessage)packet;
            if (ValidateDownlinkMessage(dataMessage) == false) return null;
            var macCommands = dataMessage.FrameHeader.MacCommands;
            var responseMacCommands = new List<MacCommand>(macCommands.Count);
            foreach (var macCommand in macCommands)
            {
                switch (macCommand)
                {
                    case LinkCheckAns linkCheckAns:
                        logger.Info($"Link status Margin: {linkCheckAns.Margin}, Gateway Count: {linkCheckAns.GwCnt}");
                        break;
                    case LinkADRReq linkAdrReq:
                        responseMacCommands.Add(HandleAdrRequest(linkAdrReq));
                        break;
                    case DutyCycleReq dutyCycleReq:
                        break;
                    case RXParamSetupReq rXParamSetupReq:
                        break;
                    case DevStatusReq devStatusReq:
                        responseMacCommands.Add(HandleDevStatusRequest(devStatusReq, envelope.Snr));
                        break;
                    case NewChannelReq newChannelReq:
                        break;
                    case RXTimingSetupReq rXTimingSetupReq:
                        break;
                    case TxParamSetupReq txParamSetupReq:
                        break;
                    case DlChannelReq dlChannelReq:
                        break;
                    case DeviceTimeAns:
                        break;
                    default:
                        // Do nothing
                        break;
                }
            }
            if (responseMacCommands.Count > 0)
            {
                DataMessage response;
                var macCommandsLength = responseMacCommands.Sum(x => x.Length);
                if (macCommandsLength > 15)
                {
                    var frmPayload = new byte[macCommandsLength];
                    var offset = 0;
                    foreach (var macCommand in responseMacCommands)
                    {
                        Array.Copy(macCommand.Value, 0, frmPayload, offset, macCommand.Length);
                        offset += macCommand.Length;
                    }
                    // Needs a dedicated message with MacCommands in the payload
                    var frameControl = new UplinkFrameControl(false, false, false, false, 0);
                    var frameHeader = new FrameHeader(Settings!.DeviceAddress, frameControl, Settings.UplinkFrameCounter, (byte[]?)null);
                    response = new DataMessage(
                        Settings.AppSKey,
                        Settings.NetworkSKey,
                        new MacHeader(PacketType.UnconfirmedDataUp, 0x00),
                        frameHeader,
                        Settings.UplinkFrameCounter,
                        0,
                        frmPayload);
                }
                else
                {
                    // Response can be sent in the FOptions field
                    var frameControl = new UplinkFrameControl(false, false, false, false, (byte)macCommandsLength);
                    var frameHeader = new FrameHeader(Settings!.DeviceAddress, frameControl, Settings.UplinkFrameCounter, responseMacCommands);
                    response = new DataMessage(
                        Settings.AppSKey,
                        Settings.NetworkSKey,
                        new MacHeader(PacketType.UnconfirmedDataUp, 0x00),
                        frameHeader,
                        Settings.UplinkFrameCounter,
                        null,
                        Array.Empty<byte>());
                }

                await radio.Send(response.PhyPayload).ConfigureAwait(false);
            }

            return dataMessage;
        }

        protected MacCommand HandleAdrRequest(LinkADRReq request)
        {
            throw new NotImplementedException("Handling ADR requests is not yet supported");
            return new LinkADRAns(new byte[] { 0x00 });
        }

        protected MacCommand HandleDevStatusRequest(DevStatusReq request, int snr)
        {
            // TODO: we need to get the battery level from somewhere...
            return new DevStatusAns(255, (byte)snr);
        }

        private void ThrowIfSettingsNull()
        {
            if (Settings == null)
                throw new InvalidOperationException("Settings cannot be null");
            if (PacketFactory == null)
                throw new InvalidOperationException("PacketFactory cannot be null");
        }
    }
}
