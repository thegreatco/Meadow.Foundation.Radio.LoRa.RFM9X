using Meadow.Logging;

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
            var settings = new OtaaSettings([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
                                            [0, 0, 0, 0],
                                            [0, 0, 0, 0],
                                            [0, 0, 0, 0],
                                            [0, 0],
                                            0);
            byte[] appEui = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
            byte[] devEui = [0x06, 0x31, 0x00, 0xD8, 0x7E, 0xD5, 0xB3, 0x70];
            var packet = new JoinRequestPacket(settings.AppKey, appEui, devEui, new byte[] { 0x00, 0x00 });
            var payload = Convert.ToBase64String(packet.MacPayloadWithMic.ToArray());
            Assert.That(payload, Is.EqualTo("AAAAAAAAAAAABjEA2H7Vs3AAAPQxuTw="));
        }

        [Test]
        public void TestKnownUnconfirmedUplinkPacket()
        {
            var packet = new UnconfirmedDataUpPacket(Convert.FromHexString("01020304"),
                                                     new UplinkFrameControl(false, false, true, false),
                                                     Convert.FromHexString("0003"),
                                                     ReadOnlyMemory<byte>.Empty,
                                                     0x01,
                                                     "Hello!"u8.ToArray(),
                                                     Convert.FromHexString("44024241ed4ce9a68c6a8bc055233fd3"),
                                                     Convert.FromHexString("ec925802ae430ca77fd3dd73cb2cc588"));
            Console.WriteLine(packet);
            Assert.That(packet.PhyPayload.ToHexString(), Is.EqualTo("400403020120030001A4A93023B19A5C5F0828"));
        }

        //YO+W/SeAAAAAql/24u9PtVHxm9zCW1Y6AFTv7aOWCf+y37ic27ja
        [Test]
        public void TestUnconfirmedDownlinkPacket()
        {
            byte[] appKey = [0xA2, 0x66, 0xE8, 0x9F, 0x4E, 0x3A, 0xA7, 0x33, 0x18, 0x19, 0x94, 0x89, 0x38, 0xE5, 0x68, 0x67];
            var packet = Packet.DecodePacket(appKey, Convert.FromBase64String("YO+W/SeAAAAAql/24u9PtVHxm9zCW1Y6AFTv7aOWCf+y37ic27ja"));
            Console.WriteLine(packet);
        }
    }
}