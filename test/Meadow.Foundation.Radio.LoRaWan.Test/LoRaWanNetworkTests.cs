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
            var parameters = new LoRaWanParameters(LoRaWanChannel.Us915Fsb2, appKey, devEui, joinEui);
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
            var parameters = new LoRaWanParameters(LoRaWanChannel.Us915Fsb2, appKey, devEui, joinEui);
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
            var mockPacketFactory = new Mock<IPacketFactory>();
            mockPacketFactory.Setup(x => x.Parse(It.IsAny<byte[]>())).Returns(dm);
            var parameters = new LoRaWanParameters(LoRaWanChannel.Us915Fsb2, appKey, devEui, joinEui);
            var network = new TestLoRaWanNetwork(logger, mockRadio.Object, parameters);
            network.SetSettings(settings);
            network.SetPacketFactory(mockPacketFactory.Object);

            var envelope = new Envelope(new byte[] { 0x02, 0x02, 0x02, 0x02 }, 0);
            
            Assert.DoesNotThrowAsync(async () => await network.HandleMacCommands(envelope));
            Assert.That(mockRadio.Invocations.Count, Is.EqualTo(1));
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
