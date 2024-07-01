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
                                                     "test"u8.ToArray(),
                                                     Convert.FromHexString("44024241ed4ce9a68c6a8bc055233fd3"),
                                                     Convert.FromHexString("ec925802ae430ca77fd3dd73cb2cc588"));
            Assert.That(packet.MacPayload.ToHexString(), Is.EqualTo("040302012003000198A92F3B"));
        }
    }
}