namespace Meadow.Foundation.Radio.LoRaWan.Test
{
    public class PacketTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestJoinRequestMessage()
        {
            var settings = new OtaaSettings(new AppKey([0xA2, 0x66, 0xE8, 0x9F, 0x4E, 0x3A, 0xA7, 0x33, 0x18, 0x19, 0x94, 0x89, 0x38, 0xE5, 0x68, 0x67]),
                                            new JoinNonce([0, 0, 0, 0]),
                                            new NetworkId([0, 0, 0, 0]),
                                            new DeviceAddress([0, 0, 0, 0]),
                                            new DeviceNonce([0, 0]),
                                            0,
                                            0,
                                            new NetworkSKey([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]),
                                            new AppSKey([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]));
            var joinEui = new JoinEui([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
            var devEui = new DevEui([0x06, 0x31, 0x00, 0xD8, 0x7E, 0xD5, 0xB3, 0x70]);
            var devNonce = new DeviceNonce([0x41, 0x41]);
            var packet = new JoinRequest(settings.AppKey, joinEui, devEui, devNonce);
            Console.WriteLine(packet);
            Assert.That(packet.JoinEui.Value.ToHexString(), Is.EqualTo("0000000000000000"));
            Assert.That(packet.DeviceEui.Value.ToHexString(), Is.EqualTo("063100D87ED5B370"));
            Assert.That(packet.DeviceNonce.Value.ToHexString(), Is.EqualTo("4141"));
            Assert.That(packet.MacHeader.Value, Is.EqualTo(0x00));
            Assert.That(packet.MacPayload.ToHexString(), Is.EqualTo("0000000000000000063100D87ED5B3704141"));
            Assert.That(packet.Mic.Value.ToHexString(), Is.EqualTo("7978D605"));
            Assert.That(packet.PhyPayload.ToHexString(), Is.EqualTo("000000000000000000063100D87ED5B37041417978D605"));
        }
        //IFiMqt10HW1GYNewW/N3zlEtgeijNH3ilFV/OD7G2ISq
        //IHnhhPlfcrfzpOjcWoFrYgHtGMPkqUrBmVj93KxPq05k

        [Test]
        public void TestJoinResponseMessage()
        {
            var path = @"D:\Dropbox\dev\GitHub\otaa_settings.bin";
            var contents = File.ReadAllBytes(path);
            var settings = new OtaaSettings(contents);
            var packet = JoinAccept.FromPhy(settings.AppKey, Convert.FromBase64String("IJpYmZgCty9u9FDtCkbCG5E="));
            Assert.That(packet.MacHeader.Value, Is.EqualTo(0x20));
            Assert.That(packet.MacPayload.ToHexString(), Is.EqualTo("120000130000EA96FD270805"));
            Assert.That(packet.Mic.Value.ToHexString(), Is.EqualTo("60BF9C2F"));
            Assert.That(packet.ReceiveDelay, Is.EqualTo(5));
            Console.WriteLine(packet);
        }

        [Test]
        public void TestJoinResponseCFList()
        {
            var packet = JoinAccept.FromPhy(new AppKey(Convert.FromHexString("F1DE67E2DCF1BA6ED05B81682B7E7A51")), Convert.FromBase64String("IHMSXDqPI9YDLdQPLkRt/tgy10pq8IAsgM5gfpbFjOYi"));
            Console.WriteLine(packet);
            Console.WriteLine(packet.CFList.Value.ToHexString());
        }

        [Test]
        public void TestUnconfirmedUplinkPacket()
        {
            var deviceAddress = new DeviceAddress(Convert.FromHexString("01020304"));
            var appSKey = new AppSKey(Convert.FromHexString("ec925802ae430ca77fd3dd73cb2cc588"));
            var networkSKey = new NetworkSKey(Convert.FromHexString("44024241ed4ce9a68c6a8bc055233fd3"));
            var frameHeader = new FrameHeader(deviceAddress, new UplinkFrameControl(false, false, true, false), 3, Array.Empty<byte>());
            var packet = new DataMessage(appSKey, networkSKey, new MacHeader(PacketType.UnconfirmedDataUp, 0x00), frameHeader, 3, 1, "Hello!"u8.ToArray());
            Assert.That(packet.MacPayload.ToHexString(), Is.EqualTo("0403020120030001A4A93023B19A"));
            Assert.That(packet.FrameHeader.Value.ToHexString(), Is.EqualTo("04030201200300"));
            Assert.That(packet.Mic.Value.ToHexString(), Is.EqualTo("5C5F0828"));
            Assert.That(packet.FrameHeader.FrameControl.Ack, Is.True);
            Assert.That(packet.PhyPayload.ToHexString(), Is.EqualTo("400403020120030001A4A93023B19A5C5F0828"));
            Assert.That(packet.MacHeader.PacketType, Is.EqualTo(PacketType.UnconfirmedDataUp));
            Console.WriteLine(packet);
        }

        // YOiW/SeBAAAGZYSMww==
        // YOiW/SeBAQAGlDo4Wg==
        [Test]
        public void TestUnconfirmedDownlinkPacket()
        {
            var path = @"D:\Dropbox\dev\GitHub\otaa_settings.bin";
            var contents = File.ReadAllBytes(path);
            var settings = new OtaaSettings(contents);
            var packet = DataMessage.FromPhy(settings.AppSKey, settings.NetworkSKey, Convert.FromBase64String("YOiW/SeBAAAGZYSMww=="));
            Assert.That(packet.MacHeader.PacketType, Is.EqualTo(PacketType.UnconfirmedDataDown));
            Assert.That(packet.FrameHeader.FrameControl.FOptsLength, Is.EqualTo(1));
            Assert.That(packet.FrameHeader.MacCommands.Count, Is.EqualTo(1));
            Assert.That(packet.FrameHeader.MacCommands[0], Is.TypeOf<DevStatusReq>());
            Console.WriteLine(packet);
        }
    }
}