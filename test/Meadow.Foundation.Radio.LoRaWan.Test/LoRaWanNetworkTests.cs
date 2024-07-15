using Meadow.Foundation.Radio.LoRa;
using Meadow.Logging;

using Mockable.Moq;

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
        private JoinNonce joinNonce = new JoinNonce([0,0,0,0]);
        private NetworkId networkId = new NetworkId([0,0,0,0]);
        private DeviceNonce deviceNonce = new DeviceNonce([0,0]);

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
            var parameters = new LoRaWanParameters(new US915ChannelPlan(DataRate.DR4), appKey, devEui, joinEui);
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
            // TODO: Mack these more mockable...
            var fh = new FrameHeader(deviceAddress, new UplinkFrameControl(false, false, true, false), 0, [new LinkCheckAns(new byte[]{0x02, 0xFF, 0xFF})]);
            var mh = new MacHeader(PacketType.UnconfirmedDataDown, 0x00);
            var dm = new DataMessage(appSKey, networkSKey, mh, fh, fh.FrameCount, null, Array.Empty<byte>());
            var logger = new Logger();
            var mockRadio = new Mock<ILoRaRadio>();
            var mockPacketFactory = new Mock<IPacketFactory>();
            mockPacketFactory.Setup(x => x.Parse(It.IsAny<byte[]>())).Returns(dm);
            var parameters = new LoRaWanParameters(new US915ChannelPlan(DataRate.DR4), appKey, devEui, joinEui);
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
            // TODO: Mack these more mockable...
            var fh = new FrameHeader(deviceAddress, new UplinkFrameControl(false, false, true, false), 0, [new DevStatusReq(new byte[]{0x06})]);
            var mh = new MacHeader(PacketType.UnconfirmedDataDown, 0x00);
            var dm = new DataMessage(appSKey, networkSKey, mh, fh, fh.FrameCount, null, Array.Empty<byte>());
            var logger = new Logger();
            var mockRadio = new Mock<ILoRaRadio>();
            byte[]? dataSent = null;
            mockRadio.Setup(x => x.Send(It.IsAny<byte[]>())).Callback<byte[]>(x => dataSent = x).Returns(new ValueTask());
            var mockPacketFactory = new Mock<IPacketFactory>();
            mockPacketFactory.Setup(x => x.Parse(It.IsAny<byte[]>())).Returns(dm);
            var parameters = new LoRaWanParameters(new US915ChannelPlan(DataRate.DR4), appKey, devEui, joinEui);
            var network = new TestLoRaWanNetwork(logger, mockRadio.Object, parameters);
            network.SetSettings(settings);
            network.SetPacketFactory(mockPacketFactory.Object);

            var envelope = new Envelope(new byte[] { 0x02, 0x02, 0x02, 0x02 }, 5);
            
            Assert.DoesNotThrowAsync(async () => await network.HandleMacCommands(envelope));
            Assert.That(mockRadio.Invocations.Count, Is.EqualTo(1));
            Assert.That(dataSent, Is.Not.Null);
            Assert.That(dataSent[8..11], Is.EqualTo(new byte[]{0x06, 0xFF, 0x05}));

            var reconstituted = DataMessage.FromPhy(appSKey, networkSKey, dataSent);
            Console.WriteLine(reconstituted);
            Assert.That(reconstituted.FrameHeader.MacCommands.Count, Is.EqualTo(1));
            Assert.That(reconstituted.FrameHeader.MacCommands[0], Is.TypeOf<DevStatusAns>());
            var devStatus = (DevStatusAns)reconstituted.FrameHeader.MacCommands[0];
            Assert.That(devStatus.Battery, Is.EqualTo(0xFF));
            Assert.That(devStatus.RadioStatus, Is.EqualTo(5));
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
        }
    }
}
