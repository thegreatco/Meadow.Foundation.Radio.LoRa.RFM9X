using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Meadow.Foundation.Radio.LoRa;
using Meadow.Logging;

namespace Meadow.Foundation.Radio.LoRaWan
{
    // TODO: We need to have a method for getting battery info, the devStatusReq/devStatusAns commands require battery info
    public abstract class LoRaWanNetwork(Logger logger, ILoRaRadio radio, LoRaWanParameters parameters)
    {
        private static TimeSpan _sleepOffset = new(1000);
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
            var message = PacketFactory!.CreateUnconfirmedDataUpMessage(payload);
            await SendMessage(message);
        }

        private async ValueTask SendMessage(DataMessage message)
        {
            ThrowIfSettingsNull();

            var uplinkFrequency = parameters.FrequencyManager.GetNextUplinkFrequency();
            var loRaParameters = new LoRaParameters(uplinkFrequency.Frequency, uplinkFrequency.Bandwidth, 5, parameters.Plan.CodingRate, parameters.Plan.UpstreamSpreadingFactor, false, true, false);
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
                loRaParameters = new LoRaParameters(downlinkFrequency.Frequency, downlinkFrequency.Bandwidth, 5, parameters.Plan.CodingRate, parameters.Plan.UpstreamSpreadingFactor, false, true, false);
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
            var loRaParameters = new LoRaParameters(joinFrequency.Frequency, joinFrequency.Bandwidth, 5, parameters.Plan.CodingRate, SpreadingFactor.Sf10, false, true, false);
            await radio.SetLoRaParameters(loRaParameters);
            await radio.Send(request.PhyPayload);
            logger.Debug("join-request sent, waiting for response");

            // Wait for the join-accept message
            //await Task.Delay(GetSleepTime(parameters.Plan.JoinAcceptDelay1)).ConfigureAwait(false);

            var receiveFrequency = parameters.FrequencyManager.GetDownlinkFrequency();
            // TODO: The data rate is what drives the bandwidth and spreading factor, these are hard coded for join-accept messages to DR0 for 0..63 and DR4 for 64..71
            loRaParameters = new LoRaParameters(receiveFrequency.Frequency, receiveFrequency.Bandwidth, 5, parameters.Plan.CodingRate, SpreadingFactor.Sf10, false, true, true);
            await radio.SetLoRaParameters(loRaParameters);
            var res = await radio.Receive(TimeSpan.FromSeconds(10));
            logger.Debug($"Join accept received: {Convert.ToBase64String(res.MessagePayload)}");
            var ja = JoinAccept.FromPhy(parameters.AppKey, res.MessagePayload);
            // We need this to process settings contained in the message
            HandleJoinAcceptSettings(ja);
            return ja;
        }

        protected async ValueTask HandleReceivedPacket(Envelope envelope)
        {
            ThrowIfSettingsNull();
            try
            {
                await HandleMacCommands(envelope).ConfigureAwait(false);
                Settings!.IncDownlinkFrameCounter();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
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
            var loRaParameters = new LoRaParameters(uplinkFrequency.Frequency, uplinkFrequency.Bandwidth, 5, parameters.Plan.CodingRate, parameters.Plan.UpstreamSpreadingFactor, false, true, false);
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
            // TODO: Gracefully handle packets not meant for us
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
                        // TODO: Implementation is required by the spec
                        break;
                    case RXParamSetupReq rXParamSetupReq:
                        // TODO: Implementation is required by the spec
                        break;
                    case DevStatusReq devStatusReq:
                        responseMacCommands.Add(HandleDevStatusRequest(devStatusReq, envelope.Snr));
                        break;
                    case NewChannelReq newChannelReq:
                        // TODO: This is needed for dynamic channel allocation, but in the US we have a fixed channel plan
                        break;
                    case RXTimingSetupReq rXTimingSetupReq:
                        // TODO: Implementation is required by the spec
                        break;
                    case TxParamSetupReq txParamSetupReq:
                        // TODO: Implementation is NOT required by the spec
                        break;
                    case DlChannelReq dlChannelReq:
                        // TODO: This is needed for dynamic channel allocation, but in the US we have a fixed channel plan
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
                logger.Info($"Responding to {responseMacCommands.Count} MAC commands");
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

                logger.Info($"Transmitting response {response.PhyPayload}");
                await SendMessage(response).ConfigureAwait(false);
            }

            return dataMessage;
        }

        internal void HandleJoinAcceptSettings(JoinAccept joinAccept)
        {
            // TODO: Need to persist this list
            logger.Info("Handling join-accept settings");
            if (joinAccept.CFList != null)
            {
                var channels = new Dictionary<int, bool>();
                for (var i = 0; i < 4; i++)
                {
                    var start = i * 2;
                    var end = start + 2;
                    var channelSettings = joinAccept.CFList.Value[start..(start + 2)];
                    channels.Add((i * 16) + 0, (channelSettings[0] & 0x01) == 0x01);
                    channels.Add((i * 16) + 1, (channelSettings[0] & 0x02) == 0x02);
                    channels.Add((i * 16) + 2, (channelSettings[0] & 0x04) == 0x04);
                    channels.Add((i * 16) + 3, (channelSettings[0] & 0x08) == 0x08);
                    channels.Add((i * 16) + 4, (channelSettings[0] & 0x10) == 0x10);
                    channels.Add((i * 16) + 5, (channelSettings[0] & 0x20) == 0x20);
                    channels.Add((i * 16) + 6, (channelSettings[0] & 0x40) == 0x40);
                    channels.Add((i * 16) + 7, (channelSettings[0] & 0x80) == 0x80);
                    channels.Add((i * 16) + 8, (channelSettings[1] & 0x01) == 0x01);
                    channels.Add((i * 16) + 9, (channelSettings[1] & 0x02) == 0x02);
                    channels.Add((i * 16) + 10, (channelSettings[1] & 0x04) == 0x04);
                    channels.Add((i * 16) + 11, (channelSettings[1] & 0x08) == 0x08);
                    channels.Add((i * 16) + 12, (channelSettings[1] & 0x10) == 0x10);
                    channels.Add((i * 16) + 13, (channelSettings[1] & 0x20) == 0x20);
                    channels.Add((i * 16) + 14, (channelSettings[1] & 0x40) == 0x40);
                    channels.Add((i * 16) + 15, (channelSettings[1] & 0x80) == 0x80);
                }
                // We should ignore when no channels are enabled and use all available channels
                if (channels.All(x => x.Value == false))
                    return;
                foreach (var channel in channels)
                {
                    logger.Debug($"Setting channel {channel.Key} to {(channel.Value ? "enabled" : "disabled")}");
                    parameters.FrequencyManager.SetChannelState(channel.Key, channel.Value);
                }
            }
        }

        private void SetChannelStateForMaskControl(int channelMaskControl, Dictionary<int, bool> channels)
        {
            var (channelStart, channelEnd) = channelMaskControl switch
            {
                0 => (0, 15),
                1 => (16, 31),
                2 => (32, 47),
                3 => (48, 63),
                4 => (64, 72),
                _ => throw new InvalidOperationException("Invalid channel mask control")
            };
            for (var i = 0; i < 72; i++)
            {
                if (i >= channelStart && i <= channelEnd)
                {
                    logger.Debug($"Setting channel {i} to {(channels[i - channelStart] ? "enabled" : "disabled")}");
                    parameters.FrequencyManager.SetChannelState(i, channels[i - channelStart]);
                }
                else
                {
                    logger.Debug($"Setting channel {i} to disabled");
                    parameters.FrequencyManager.SetChannelState(i, false);
                }
            }
        }

        private void EnableChannelGroup(int channelGroup)
        {
            var channelOffset = channelGroup switch
            {
                0 => 0,
                1 => 16,
                2 => 32,
                3 => 48,
                4 => 64,
                _ => throw new InvalidOperationException("Invalid channel mask control")
            };
            for (var i = 0; i < 72; i++)
            {
                if (i >= channelOffset && i < channelOffset + 16)
                {
                    logger.Debug($"Setting channel {i} to enabled");
                    parameters.FrequencyManager.SetChannelState(i, true);
                }
                else
                {
                    logger.Debug($"Setting channel {i} to disabled");
                    parameters.FrequencyManager.SetChannelState(i, false);
                }
            }
        }

        private void SetChannelStateFor10Lsb(byte[] chMask)
        {
            if ((chMask[0] & 0x01) == 0x01)
            {
                EnableChannelGroup(0);
            }
            if ((chMask[0] & 0x02) == 0x02)
            {
                EnableChannelGroup(1);
            }
            if ((chMask[0] & 0x04) == 0x04)
            {
                EnableChannelGroup(2);
            }
            if ((chMask[0] & 0x08) == 0x08)
            {
                EnableChannelGroup(3);
            }
            if ((chMask[0] & 0x10) == 0x10)
            {
                EnableChannelGroup(4);
            }
            if ((chMask[0] & 0x20) == 0x20)
            {
                EnableChannelGroup(5);
            }
            if ((chMask[0] & 0x40) == 0x40)
            {
                EnableChannelGroup(6);
            }
            if ((chMask[0] & 0x80) == 0x80)
            {
                EnableChannelGroup(7);
            }
            if ((chMask[1] & 0x01) == 0x01)
            {
                EnableChannelGroup(8);
            }
            if ((chMask[1] & 0x02) == 0x02)
            {
                EnableChannelGroup(9);
            }
        }

        private void EnableOnly125kHzChannels()
        {
            logger.Debug($"Enabling only 125kHz channels");
            for (var i = 0; i < 64; i++)
            {
                parameters.FrequencyManager.SetChannelState(i, true);
            }

            for (var i = 64; i < 72; i++)
            {
                parameters.FrequencyManager.SetChannelState(i, false);
            }
        }

        private void EnableOnly500kHzChannels()
        {
            logger.Debug($"Enabling only 500kHz channels");
            for (var i = 0; i < 64; i++)
            {
                parameters.FrequencyManager.SetChannelState(i, false);
            }

            for (var i = 64; i < 72; i++)
            {
                parameters.FrequencyManager.SetChannelState(i, true);
            }
        }

        // TODO: This needs to be extracted out to the plan level, because the channel mask control is plan specific
        protected MacCommand HandleAdrRequest(LinkADRReq request)
        {
            logger.Info($"Handling ADR request {request}");
            switch (request.ChannelMaskControl)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                    SetChannelStateForMaskControl(request.ChannelMaskControl, request.Channels);
                    break;
                case 5:
                    SetChannelStateFor10Lsb(request.ChMask);
                    break;
                case 6: // This is a special case, enable all 125kHz channels and disable the 500kHz channels
                    EnableOnly125kHzChannels();
                    break;
                case 7: // This is a special case, enable all 500kHz channels and disable the 125kHz channels
                    EnableOnly500kHzChannels();
                    break;
            }

            if (request.DataRate != 0xF)
            {
                // TODO: Handle setting data rate
            }

            if (request.TxPower != 0xF)
            {
                // TODO: Handle setting data rate
            }

            // TODO: Support NbTrans
            // This response indicates that we accepted the channel list, but nothing else
            return new LinkADRAns(0x01);
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
