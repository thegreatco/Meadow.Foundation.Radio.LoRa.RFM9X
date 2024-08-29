using Meadow.Foundation.Radio.LoRa;
using Meadow.Logging;

using Moq;

namespace Meadow.Foundation.Radio.LoRaWan
{
    internal class LoRaWanNetworkTests
    {
        private AppSKey appSKey = new AppSKey([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
        private NetworkSKey networkSKey = new NetworkSKey([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
        private AppKey appKey = new AppKey([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
        private DevEui devEui = new DevEui([0, 0, 0, 0, 0, 0, 0, 0]);
        private JoinEui joinEui = new JoinEui([0, 0, 0, 0, 0, 0, 0, 0]);
        private DeviceAddress deviceAddress = new DeviceAddress([0, 0, 0, 0]);
        private JoinNonce joinNonce = new JoinNonce([0, 0, 0, 0]);
        private NetworkId networkId = new NetworkId([0, 0, 0, 0]);
        private DeviceNonce deviceNonce = new DeviceNonce([0, 0]);

        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void HandleMacCommands_EmptyEnvelope()
        {
            var logger = new Logger();
            var mockRadio = new Mock<ILoRaRadio>();
            var mockPacketFactory = new Mock<IPacketFactory>();
            var parameters = new LoRaWanParameters(new US915ChannelPlan(), appKey, devEui, joinEui);
            var network = new TestLoRaWanNetwork(logger, mockRadio.Object, parameters);
            var envelope = new Envelope(Array.Empty<byte>(), 0);
            Assert.ThrowsAsync<PacketFactoryNullException>(async () => await network.HandleMacCommands(envelope));

            network.SetPacketFactory(mockPacketFactory.Object);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await network.HandleMacCommands(envelope));
        }

        [Test]
        public void HandleMacCommands_LinkCheckAns()
        {
            var settings = new OtaaSettings(appKey, joinNonce, networkId, deviceAddress, deviceNonce, 0, 0, networkSKey, appSKey);
            // TODO: Make these more mockable...
            var fh = new FrameHeader(deviceAddress, new UplinkFrameControl(false, false, true, false), 0, [new LinkCheckAns(new byte[] { 0x02, 0xFF, 0xFF })]);
            var mh = new MacHeader(PacketType.UnconfirmedDataDown, 0x00);
            var dm = new DataMessage(appSKey, networkSKey, mh, fh, fh.FrameCount, null, Array.Empty<byte>());
            var logger = new Logger();
            var mockRadio = new Mock<ILoRaRadio>();
            var mockPacketFactory = new Mock<IPacketFactory>();
            mockPacketFactory.Setup(x => x.Parse(It.IsAny<byte[]>())).Returns(dm);
            var parameters = new LoRaWanParameters(new US915ChannelPlan(), appKey, devEui, joinEui);
            var network = new TestLoRaWanNetwork(logger, mockRadio.Object, parameters);
            network.SetSettings(settings);
            network.SetPacketFactory(mockPacketFactory.Object);

            var envelope = new Envelope(new byte[] { 0x02, 0x02, 0x02, 0x02 }, 0);

            Assert.DoesNotThrowAsync(async () => await network.HandleMacCommands(envelope));
            Assert.That(mockRadio.Invocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void HandleMacCommands_DevStatusReq()
        {
            var settings = new OtaaSettings(appKey, joinNonce, networkId, deviceAddress, deviceNonce, 0, 0, networkSKey, appSKey);
            // TODO: Make these more mockable...
            var fh = new FrameHeader(deviceAddress, new UplinkFrameControl(false, false, true, false), 0, [new DevStatusReq(new byte[] { 0x06 })]);
            var mh = new MacHeader(PacketType.UnconfirmedDataDown, 0x00);
            var dm = new DataMessage(appSKey, networkSKey, mh, fh, fh.FrameCount, null, Array.Empty<byte>());
            var logger = new Logger();
            var mockRadio = new Mock<ILoRaRadio>();
            byte[]? dataSent = null;
            mockRadio.Setup(x => x.Send(It.IsAny<byte[]>())).Callback<byte[]>(x => dataSent = x).Returns(new ValueTask());
            var mockPacketFactory = new Mock<IPacketFactory>();
            mockPacketFactory.Setup(x => x.Parse(It.IsAny<byte[]>())).Returns(dm);
            var parameters = new LoRaWanParameters(new US915ChannelPlan(), appKey, devEui, joinEui);
            var network = new TestLoRaWanNetwork(logger, mockRadio.Object, parameters);
            network.SetSettings(settings);
            network.SetPacketFactory(mockPacketFactory.Object);

            var envelope = new Envelope(new byte[] { 0x02, 0x02, 0x02, 0x02 }, 5);

            Assert.DoesNotThrowAsync(async () => await network.HandleMacCommands(envelope));
            Assert.That(mockRadio.Invocations.Count, Is.EqualTo(1));
            Assert.That(dataSent, Is.Not.Null);
            Assert.That(dataSent[8..11], Is.EqualTo(new byte[] { 0x06, 0xFF, 0x05 }));

            var reconstituted = DataMessage.FromPhy(appSKey, networkSKey, dataSent);
            Console.WriteLine(reconstituted);
            Assert.That(reconstituted.FrameHeader.MacCommands.Count, Is.EqualTo(1));
            Assert.That(reconstituted.FrameHeader.MacCommands[0], Is.TypeOf<DevStatusAns>());
            var devStatus = (DevStatusAns)reconstituted.FrameHeader.MacCommands[0];
            Assert.That(devStatus.Battery, Is.EqualTo(0xFF));
            Assert.That(devStatus.RadioStatus, Is.EqualTo(5));
        }

        [Test]
        [TestCase(new byte[] { 0x03, 0x00, 0x01, 0x00, 0x0F }, 0, new int[] { 0 }, TestName = "HandleMacCommands_LinkADRReq_ChMaskCntrl_1")]
        [TestCase(new byte[] { 0x03, 0x00, 0x01, 0x00, 0x1F }, 1, new int[] { 16 }, TestName = "HandleMacCommands_LinkADRReq_ChMaskCntrl_2")]
        [TestCase(new byte[] { 0x03, 0x00, 0x01, 0x00, 0x2F }, 2, new int[] { 32 }, TestName = "HandleMacCommands_LinkADRReq_ChMaskCntrl_3")]
        [TestCase(new byte[] { 0x03, 0x00, 0x01, 0x00, 0x3F }, 3, new int[] { 48 }, TestName = "HandleMacCommands_LinkADRReq_ChMaskCntrl_4")]
        public void HandleMacCommands_LinkADRReq_ChMaskCntrl(byte[] commandBody, int channelGroup, int[] enabledChannels)
        {
            var settings = new OtaaSettings(appKey, joinNonce, networkId, deviceAddress, deviceNonce, 0, 0, networkSKey, appSKey);
            // TODO: Make these more mockable...
            var fh = new FrameHeader(deviceAddress, new UplinkFrameControl(false, false, true, false), 0, [new LinkADRReq(commandBody)]);
            var mh = new MacHeader(PacketType.UnconfirmedDataDown, 0x00);
            var dm = new DataMessage(appSKey, networkSKey, mh, fh, fh.FrameCount, null, Array.Empty<byte>());
            var logger = new Logger();
            var mockRadio = new Mock<ILoRaRadio>();
            byte[]? dataSent = null;
            mockRadio.Setup(x => x.Send(It.IsAny<byte[]>())).Callback<byte[]>(x => dataSent = x).Returns(new ValueTask());
            var mockPacketFactory = new Mock<IPacketFactory>();
            mockPacketFactory.Setup(x => x.Parse(It.IsAny<byte[]>())).Returns(dm);
            var parameters = new LoRaWanParameters(new US915ChannelPlan(), appKey, devEui, joinEui);
            var network = new TestLoRaWanNetwork(logger, mockRadio.Object, parameters);
            network.SetSettings(settings);
            network.SetPacketFactory(mockPacketFactory.Object);

            var envelope = new Envelope(commandBody, 5);
            Assert.DoesNotThrowAsync(async () => await network.HandleMacCommands(envelope));
            Assert.That(mockRadio.Invocations.Count, Is.EqualTo(1));
            Assert.That(dataSent, Is.Not.Null);
            Assert.That(dataSent[8..10], Is.EqualTo(new byte[] { 0x03, 0x01 }));

            for (var i = 0; i < 64; i++)
            {
                var val = (i / 16) == channelGroup;
                if (val)
                {
                    if (enabledChannels.Contains(i))
                    {
                        Assert.That(parameters.FrequencyManager.GetChannelState(i), Is.True);
                    }
                    else
                    {
                        Assert.That(parameters.FrequencyManager.GetChannelState(i), Is.False);
                    }
                }
                else
                {
                    Assert.That(parameters.FrequencyManager.GetChannelState(i), Is.False);
                }
            }

            var reconstituted = DataMessage.FromPhy(appSKey, networkSKey, dataSent);
            Console.WriteLine(reconstituted);
            Assert.That(reconstituted.FrameHeader.MacCommands.Count, Is.EqualTo(1));
            Assert.That(reconstituted.FrameHeader.MacCommands[0], Is.TypeOf<LinkADRAns>());
            var response = (LinkADRAns)reconstituted.FrameHeader.MacCommands[0];
            Assert.That(response.Value, Is.EqualTo(new byte[] { 0x03, 0x01 }));
            Assert.That(response.ChannelMaskAck, Is.True);
            Assert.That(response.DataRateAck, Is.False);
            Assert.That(response.PowerAck, Is.False);
        }

        [Test]
        [TestCase(new byte[] { 0x03, 0x00, 0x12, 0x00, 0x0F }, 0, new int[] { 1, 4 }, TestName = "HandleMacCommands_LinkADRReq_ChMask_1")]
        [TestCase(new byte[] { 0x03, 0x00, 0x33, 0x43, 0x0F }, 0, new int[] { 0, 1, 4, 5, 8, 9, 14 }, TestName = "HandleMacCommands_LinkADRReq_ChMask_2")]
        [TestCase(new byte[] { 0x03, 0x00, 0x67, 0x96, 0x0F }, 0, new int[] { 0, 1, 2, 5, 6, 9, 10, 12, 15 }, TestName = "HandleMacCommands_LinkADRReq_ChMask_3")]
        public void HandleMacCommands_LinkADRReq_ChMask(byte[] commandBody, int channelGroup, int[] enabledChannels)
        {
            var settings = new OtaaSettings(appKey, joinNonce, networkId, deviceAddress, deviceNonce, 0, 0, networkSKey, appSKey);
            // TODO: Make these more mockable...
            var fh = new FrameHeader(deviceAddress, new UplinkFrameControl(false, false, true, false), 0, [new LinkADRReq(commandBody)]);
            var mh = new MacHeader(PacketType.UnconfirmedDataDown, 0x00);
            var dm = new DataMessage(appSKey, networkSKey, mh, fh, fh.FrameCount, null, Array.Empty<byte>());
            var logger = new Logger();
            var mockRadio = new Mock<ILoRaRadio>();
            byte[]? dataSent = null;
            mockRadio.Setup(x => x.Send(It.IsAny<byte[]>())).Callback<byte[]>(x => dataSent = x).Returns(new ValueTask());
            var mockPacketFactory = new Mock<IPacketFactory>();
            mockPacketFactory.Setup(x => x.Parse(It.IsAny<byte[]>())).Returns(dm);
            var parameters = new LoRaWanParameters(new US915ChannelPlan(), appKey, devEui, joinEui);
            var network = new TestLoRaWanNetwork(logger, mockRadio.Object, parameters);
            network.SetSettings(settings);
            network.SetPacketFactory(mockPacketFactory.Object);

            var envelope = new Envelope(commandBody, 5);
            Assert.DoesNotThrowAsync(async () => await network.HandleMacCommands(envelope));
            Assert.That(mockRadio.Invocations.Count, Is.EqualTo(1));
            Assert.That(dataSent, Is.Not.Null);
            Assert.That(dataSent[8..10], Is.EqualTo(new byte[] { 0x03, 0x01 }));

            for (var i = 0; i < 64; i++)
            {
                var val = (i / 16) == channelGroup;
                if (val)
                {
                    if (enabledChannels.Contains(i))
                    {
                        Assert.That(parameters.FrequencyManager.GetChannelState(i), Is.True);
                    }
                    else
                    {
                        Assert.That(parameters.FrequencyManager.GetChannelState(i), Is.False);
                    }
                }
                else
                {
                    Assert.That(parameters.FrequencyManager.GetChannelState(i), Is.False);
                }
            }

            var reconstituted = DataMessage.FromPhy(appSKey, networkSKey, dataSent);
            Console.WriteLine(reconstituted);
            Assert.That(reconstituted.FrameHeader.MacCommands.Count, Is.EqualTo(1));
            Assert.That(reconstituted.FrameHeader.MacCommands[0], Is.TypeOf<LinkADRAns>());
            var response = (LinkADRAns)reconstituted.FrameHeader.MacCommands[0];
            Assert.That(response.Value, Is.EqualTo(new byte[] { 0x03, 0x01 }));
            Assert.That(response.ChannelMaskAck, Is.True);
            Assert.That(response.DataRateAck, Is.False);
            Assert.That(response.PowerAck, Is.False);
        }

        [Test]
        public void HandleJoinAcceptSettings()
        {
            var packet = JoinAccept.FromPhy(new AppKey(Convert.FromHexString("F1DE67E2DCF1BA6ED05B81682B7E7A51")), Convert.FromBase64String("IHMSXDqPI9YDLdQPLkRt/tgy10pq8IAsgM5gfpbFjOYi"));
            var settings = new OtaaSettings(appKey, joinNonce, networkId, deviceAddress, deviceNonce, 0, 0, networkSKey, appSKey);
            var logger = new Logger();
            var mockRadio = new Mock<ILoRaRadio>();
            byte[]? dataSent = null;
            mockRadio.Setup(x => x.Send(It.IsAny<byte[]>())).Callback<byte[]>(x => dataSent = x).Returns(new ValueTask());
            var mockPacketFactory = new Mock<IPacketFactory>();
            var parameters = new LoRaWanParameters(new US915ChannelPlan(), appKey, devEui, joinEui);
            var network = new TestLoRaWanNetwork(logger, mockRadio.Object, parameters);
            network.SetSettings(settings);
            network.SetPacketFactory(mockPacketFactory.Object);
            network.HandleJoinAcceptSettings(packet);
            for (var i = 0; i < 72; i++)
            {
                if (i >= 8 && i < 16)
                {
                    Assert.That(parameters.FrequencyManager.EnabledUpstreamChannels[i], Is.Not.Null);
                }
                else if (i >= 64)
                {
                    Assert.That(parameters.FrequencyManager.EnabledUpstreamChannels[i], Is.Not.Null);
                }
                else
                {
                    Assert.That(parameters.FrequencyManager.EnabledUpstreamChannels[i], Is.Null);
                }
            }
        }

        [Test]
        [Ignore("not done yet")]
        public async Task SendJoinRequest()
        {
            var settings = new OtaaSettings(appKey, joinNonce, networkId, deviceAddress, deviceNonce, 0, 0, networkSKey, appSKey);
            var logger = new Logger();
            var mockRadio = new Mock<ILoRaRadio>();
            byte[]? dataSent = null;
            mockRadio.Setup(x => x.Send(It.IsAny<byte[]>())).Callback<byte[]>(x => dataSent = x).Returns(new ValueTask());
            mockRadio.Setup(x => x.Receive(It.IsAny<TimeSpan>())).Returns(new ValueTask<Envelope>(Task.FromResult(new Envelope([0x00, 0x00, 0x00, 0x00], 5))));
            var mockPacketFactory = new Mock<IPacketFactory>();
            //mockPacketFactory.Setup(x => x.Parse(It.IsAny<byte[]>())).Returns(dm);
            var parameters = new LoRaWanParameters(new US915ChannelPlan(), appKey, devEui, joinEui);
            var network = new TestLoRaWanNetwork(logger, mockRadio.Object, parameters);
            network.SetSettings(settings);
            network.SetPacketFactory(mockPacketFactory.Object);
            var resp = await network.SendJoinRequest(deviceNonce);
        }

        private class TestLoRaWanNetwork : TheThingsNetwork
        {
            public TestLoRaWanNetwork(Logger logger, ILoRaRadio radio, LoRaWanParameters parameters) : base(logger, radio, parameters)
            {
            }

            public new ValueTask<DataMessage?> HandleMacCommands(Envelope envelope)
            {
                return base.HandleMacCommands(envelope);
            }

            public void SetPacketFactory(IPacketFactory packetFactory)
            {
                PacketFactory = packetFactory;
            }

            public void SetSettings(OtaaSettings settings)
            {
                Settings = settings;
            }

            public new ValueTask<JoinAccept> SendJoinRequest(DeviceNonce devNonce)
            {
                return base.SendJoinRequest(devNonce);
            }
        }
    }
}
