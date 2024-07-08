using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

using Meadow.Foundation.Radio.LoRa;
using Meadow.Logging;

namespace Meadow.Foundation.Radio.LoRaWan
{
    public abstract class LoRaWanNetwork(Logger logger, ILoRaRadio radio, LoRaWanParameters parameters)
    {
        private static readonly JoinEui DefaultAppEui = new([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        private readonly JoinEui _appEui = parameters.AppEui ?? DefaultAppEui;
        public OtaaSettings? Settings;
        private PacketFactory? packetFactory;

        private readonly Func<LoRaParameters> _defaultUplinkParameters =
            () => new(parameters.FrequencyManager.GetNextUplinkFrequency(),
                      parameters.FrequencyManager.UplinkBandwidth,
                      CodingRate.Cr45,
                      SpreadingFactor.Sf7,
                      false,
                      true,
                      false);

        private readonly LoRaParameters _defaultDownlinkParameters =
            new(parameters.FrequencyManager.DownlinkBaseFrequency,
                parameters.FrequencyManager.DownlinkBandwidth,
                CodingRate.Cr45,
                SpreadingFactor.Sf7,
                false,
                true,
                true);

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
            packetFactory = new PacketFactory(Settings);
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

        public async ValueTask SendMessage(byte[] payload)
        {
            ThrowIfSettingsNull();
            var message = packetFactory!.CreateUnconfirmedDataUpMessage(payload);

            await radio.SetLoRaParameters(_defaultUplinkParameters());
            logger.Debug("Sending message");
            await radio.Send(message.PhyPayload).ConfigureAwait(false);

            Settings!.IncUplinkFrameCounter();

            try
            {
                // Wait for a downlink message just in case
                await radio.SetLoRaParameters(_defaultDownlinkParameters);

                var downlinkData = await radio.Receive(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

                var packet = DataMessage.FromPhy(Settings.AppSKey, Settings.NetworkSKey, downlinkData.MessagePayload);
                logger.Debug($"Received message: {packet}");
                await HandleReceivedPacket(packet).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                logger.Error("No downlink received in window");
            }
            logger.Debug("Finished sending message");
        }

        private async ValueTask<JoinAccept> SendJoinRequest(DeviceNonce devNonce)
        {
            logger.Info("Sending join-request");
            var request = new JoinRequest(parameters.AppKey, _appEui, parameters.DevEui, devNonce);
            await radio.SetLoRaParameters(_defaultUplinkParameters());
            await radio.Send(request.PhyPayload);
            logger.Debug("join-request sent, waiting for response");
            // TODO: sleep until downlink time
            await Task.Delay(4000).ConfigureAwait(false);
            await radio.SetLoRaParameters(_defaultDownlinkParameters);
            var res = await radio.Receive(TimeSpan.FromSeconds(10));
            logger.Debug($"Join accept received: {Convert.ToBase64String(res.MessagePayload)}");
            return JoinAccept.FromPhy(parameters.AppKey, res.MessagePayload);
        }

        private async ValueTask HandleReceivedPacket(DataMessage message)
        {
            if (ValidateDownlinkMessage(message) == false) return;
            if (message.FrameHeader.MacCommands.Count > 0)
            {
                logger.Trace("Found MAC Commands in header");
                var responseCommands = new List<MacCommand>();
                foreach (var command in message.FrameHeader.MacCommands)
                {

                }
            }
            await Task.Yield();
        }

        private bool ValidateDownlinkMessage(DataMessage message)
        {
            if (Settings == null)
                throw new InvalidOperationException("Settings cannot be null");
            if (message.FrameHeader.DeviceAddress != Settings.DeviceAddress)
            {
                logger.Trace("Received downlink message with incorrect device address");
                return false;
            }
            return true;
        }

        private async ValueTask CheckLinkStatus()
        {
            ThrowIfSettingsNull();
            var message = packetFactory!.CreateLinkCheckRequestMessage();
            await radio.SetLoRaParameters(_defaultUplinkParameters());
            await radio.Send(message.PhyPayload);
            // TODO: this setting needs to get parsed from the join-accept message.
            await Task.Delay(4000).ConfigureAwait(false);
            var response = await radio.Receive(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            await HandleMacCommands(response);
        }

        private async ValueTask HandleMacCommands(Envelope envelope)
        {
            ThrowIfSettingsNull();
            var packet = packetFactory!.Parse(envelope.MessagePayload);
            if (packet is not DataMessage)
                throw new InvalidOperationException("Expected DataMessage");
            var dataMessage = (DataMessage)packet;
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
                    var frameHeader = new FrameHeader(Settings!.DeviceAddress, frameControl, Settings.UplinkFrameCounter, []);
                    var response = new DataMessage(
                        Settings.AppSKey, 
                        Settings.NetworkSKey, 
                        new MacHeader(PacketType.UnconfirmedDataDown, 0x00), 
                        frameHeader, 
                        Settings.UplinkFrameCounter, 
                        null, 
                        frmPayload);
                }
                else
                {
                    // Response can be sent in the FOptions field
                    var frameControl = new UplinkFrameControl(false, false, false, false, (byte)macCommandsLength);
                    var frameHeader = new FrameHeader(Settings!.DeviceAddress, frameControl, Settings.UplinkFrameCounter, responseMacCommands);
                    var response = new DataMessage(
                        Settings.AppSKey, 
                        Settings.NetworkSKey, 
                        new MacHeader(PacketType.UnconfirmedDataDown, 0x00), 
                        frameHeader, 
                        Settings.UplinkFrameCounter, 
                        null, 
                        Array.Empty<byte>());
                }
            }
            await Task.Yield();
        }

        private MacCommand HandleAdrRequest(LinkADRReq request)
        {
            throw new NotImplementedException("Handling ADR requests is not yet supported");
            return new LinkADRAns(new byte[]{0x00});
        }

        private MacCommand HandleDevStatusRequest(DevStatusReq request, int snr)
        {
            // TODO: we need to get the battery level from somewhere...
            // TODO: we need to get the SNR from somewhere...
            return new DevStatusAns(255, (byte)snr);
        }

        private void ThrowIfSettingsNull()
        {
            if (Settings == null)
                throw new InvalidOperationException("Settings cannot be null");
            if (packetFactory == null)
                throw new InvalidOperationException("PacketFactory cannot be null");
        }
    }
}
