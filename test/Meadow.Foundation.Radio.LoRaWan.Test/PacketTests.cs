using System.Text;

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
            var settings = new OtaaSettings(new AppKey([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]),
                                            new JoinNonce([0, 0, 0, 0]),
                                            new NetworkId([0, 0, 0, 0]),
                                            new DeviceAddress([0, 0, 0, 0]),
                                            new DeviceNonce([0, 0]),
                                            0,
                                            0,
                                            new NetworkSKey([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]),
                                            new AppSKey([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]));
            var joinEui = new JoinEui([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]);
            var devEui = new DevEui([0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x10, 0x11]);
            var devNonce = new DeviceNonce([0x12, 0x13]);
            var packet = new JoinRequest(settings.AppKey, joinEui, devEui, devNonce);
            Assert.That(packet.JoinEui.Value.ToHexString(), Is.EqualTo("0102030405060708"));
            Assert.That(packet.DeviceEui.Value.ToHexString(), Is.EqualTo("090A0B0C0D0E1011"));
            Assert.That(packet.DeviceNonce.Value.ToHexString(), Is.EqualTo("1213"));
            Assert.That(packet.MacHeader.Value, Is.EqualTo(0x00));
            Assert.That(packet.MacPayload.ToHexString(), Is.EqualTo("0102030405060708090A0B0C0D0E10111213"));
            Assert.That(packet.Mic.Value.ToHexString(), Is.EqualTo("1D2D332E"));
            Assert.That(packet.PhyPayload.ToHexString(), Is.EqualTo("000102030405060708090A0B0C0D0E101112131D2D332E"));
            Console.WriteLine(packet);
        }

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