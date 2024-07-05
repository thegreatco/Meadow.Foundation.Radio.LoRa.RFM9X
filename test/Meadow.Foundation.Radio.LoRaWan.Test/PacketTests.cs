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
                                            new AppNonce([0, 0, 0, 0]),
                                            new NetworkId([0, 0, 0, 0]),
                                            new DeviceAddress([0, 0, 0, 0]),
                                            new DeviceNonce([0, 0]),
                                            0,
                                            0,
                                            new NetworkSKey([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]),
                                            new AppSKey([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]));
            var appEui = new AppEui([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
            var devEui = new DevEui([0x06, 0x31, 0x00, 0xD8, 0x7E, 0xD5, 0xB3, 0x70]);
            var packet = new JoinRequestPacket(settings.AppKey, appEui, devEui, new DeviceNonce([0x00, 0x00]));
            var payload = Convert.ToBase64String(packet.MacPayloadWithMic.ToArray());
            Assert.That(payload, Is.EqualTo("AAAAAAAAAAAABjEA2H7Vs3AAAPQxuTw="));
        }

        [Test]
        public void TestJoinResponseMessage()
        {
            var path = @"D:\Dropbox\dev\GitHub\otaa_settings.bin";
            var contents = File.ReadAllBytes(path);
            var settings = new OtaaSettings(contents);
            var packet = new JoinAcceptPacket(settings.AppKey, Convert.FromBase64String("IJpYmZgCty9u9FDtCkbCG5E="));
            Console.WriteLine(packet);
        }

        [Test]
        public void TestKnownUnconfirmedUplinkPacket()
        {
            Console.WriteLine("Hello!"u8.ToArray().ToHexString(true));
            var packet = new UnconfirmedDataUpPacket(new DeviceAddress(Convert.FromHexString("01020304")),
                                                     new UplinkFrameControl(false, false, true, false),
                                                     3,
                                                     ReadOnlyMemory<byte>.Empty,
                                                     0x01,
                                                     "Hello!"u8.ToArray(),
                                                     new AppSKey(Convert.FromHexString("ec925802ae430ca77fd3dd73cb2cc588")),
                                                     new NetworkSKey(Convert.FromHexString("44024241ed4ce9a68c6a8bc055233fd3")));
            Console.WriteLine(packet);
            Assert.That(packet.PhyPayload.ToHexString(), Is.EqualTo("400403020120030001A4A93023B19A5C5F0828"));
        }

        //YO+W/SeAAAAAql/24u9PtVHxm9zCW1Y6AFTv7aOWCf+y37ic27ja
        [Test]
        public void TestUnconfirmedDownlinkPacket()
        {
            var path = @"D:\Dropbox\dev\GitHub\otaa_settings.bin";
            var contents = File.ReadAllBytes(path);
            var settings = new OtaaSettings(contents);
            var packet = new UnconfirmedDataDownPacket(Convert.FromBase64String("YO+W/SeAAQAAtovyH2fVLqss3+YrXTaSWY9W9d5nenqUeLZnCT4E"), settings.AppSKey, settings.NetworkSKey);
            Console.WriteLine(packet);
        }

        [Test]
        public void TestWhatever()
        {
            var path = @"D:\Dropbox\dev\GitHub\otaa_settings.bin";
            var contents = File.ReadAllBytes(path);
            var settings = new OtaaSettings(contents);
            Console.WriteLine(settings);
            //var joinPacket = new JoinAcceptPacket(settings.AppKey, Convert.FromBase64String("ILSMOAebHL6a+VyrMUCPH4E="));

            //Console.WriteLine(joinPacket);
            //var dataPacket = new UnconfirmedDataDownPacket(Convert.FromBase64String("YO+W/SeAAQAAtovyH2fVLqss3+YrXTaSWY9W9d5nenqUeLZnCT4E"), settings.AppSKey, settings.NetworkSKey);
            //Console.WriteLine(dataPacket);

            var payload = "06/10/2024 21:25:25 Hello!"u8.ToArray();
            var dataPacket = new UnconfirmedDataUpPacket(settings!.DeviceAddress,
                                                         new UplinkFrameControl(false, false, false, false),
                                                         settings.UplinkFrameCounter - 1u,
                                                         ReadOnlyMemory<byte>.Empty,
                                                         0x01,
                                                         payload,
                                                         settings.AppSKey,
                                                         settings.NetworkSKey);
            Console.WriteLine(dataPacket.PhyPayload.ToHexString(false));
        }
    }
}