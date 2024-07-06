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
        public void TestKnownUnconfirmedUplinkPacket()
        {
            var deviceAddress = new DeviceAddress(Convert.FromHexString("01020304"));
            var packet = new UnconfirmedDataUpPacket(deviceAddress,
                                                     new UplinkFrameControl(false, false, true, false),
                                                     3,
                                                     ReadOnlyMemory<byte>.Empty,
                                                     0x01,
                                                     "Hello!"u8.ToArray(),
                                                     new AppSKey(Convert.FromHexString("ec925802ae430ca77fd3dd73cb2cc588")),
                                                     new NetworkSKey(Convert.FromHexString("44024241ed4ce9a68c6a8bc055233fd3")));
            Console.WriteLine(packet);
            Assert.That(packet.PhyPayload.ToHexString(), Is.EqualTo("400403020120030001A4A93023B19A5C5F0828"));
            var appSKey = new AppSKey(Convert.FromHexString("ec925802ae430ca77fd3dd73cb2cc588"));
            var networkSKey = new NetworkSKey(Convert.FromHexString("44024241ed4ce9a68c6a8bc055233fd3"));
            var frameHeader = new FrameHeader(deviceAddress, new UplinkFrameControl(false, false, true, false), 3, Array.Empty<byte>());
            var newPacket = new UnconfirmedUplinkMessage(appSKey, networkSKey, frameHeader, 3, 1, "Hello!"u8.ToArray());
            Console.WriteLine(newPacket);
        }

        // YO+W/SeAAAAAql/24u9PtVHxm9zCW1Y6AFTv7aOWCf+y37ic27ja
        // YO2W/SeL5gADMAIAcQMwAP8BBtSofuY=
        // YO2W/SeAlQAAfRYZWdTIiZZyJbooISmMYfezihxBIneyzxiBXHHR
        [Test]
        public void TestUnconfirmedDownlinkPacket()
        {
            var path = @"D:\Dropbox\dev\GitHub\otaa_settings.bin";
            var contents = File.ReadAllBytes(path);
            var settings = new OtaaSettings(contents);
            Console.WriteLine(settings);
            var packet = new UnconfirmedDataDownPacket(Convert.FromBase64String("YO2W/SeA2wAAddAhjaldS7GTPXG8NpoSaMrwQ4DMYBsUHhV76kZ9"), settings.AppSKey, settings.NetworkSKey);
            //var packet = new UnconfirmedDataDownPacket(Convert.FromHexString("60ED96FD27800200003D6CB95322C0A717748E8609C2A47C1A8576F6BF9A79496CCBBE37CB642B"), settings.AppSKey, settings.NetworkSKey);
            Console.WriteLine(packet);
            Console.WriteLine(Convert.FromBase64String("g+io+Q==").ToHexString(false));
            Console.WriteLine(packet.EncryptedFrmPayload.ToHexString() == Convert.FromBase64String("SH5hA6WeDa0a0ZPUkxMoYgWu93cykVbzfK8=").ToHexString(false));
            Console.WriteLine(packet.FrmPayload.Length);
        }
    }
}