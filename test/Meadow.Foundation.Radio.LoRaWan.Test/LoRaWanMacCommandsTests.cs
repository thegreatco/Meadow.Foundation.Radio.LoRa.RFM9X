namespace Meadow.Foundation.Radio.LoRaWan.Test
{
    internal class LoRaWanMacCommandsTests
    {
        [Test]
        public void LinkADRReqTest_Basic()
        {
            var bytes = new byte[] { 0x03, 0x00, 0x01, 0x00, 0x00 };
            var factoryLinkADRReq = MacCommandFactory.Create(false, bytes);
            var linkADRReq = new LinkADRReq(bytes);
            // This is a sanity check to make sure the 2 code paths match
            Assert.That(factoryLinkADRReq[0].Value, Is.EqualTo(linkADRReq.Value));
            Assert.That(linkADRReq.DataRate, Is.EqualTo(0));
            Assert.That(linkADRReq.TxPower, Is.EqualTo(0));
            Assert.That(linkADRReq.ChMask, Is.EqualTo(new byte[]{ 0x01, 0x00 }));
        }
    }
}
