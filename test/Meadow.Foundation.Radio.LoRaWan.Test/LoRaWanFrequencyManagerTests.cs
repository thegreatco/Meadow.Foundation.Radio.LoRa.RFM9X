namespace Meadow.Foundation.Radio.LoRaWan.Test
{
    internal class LoRaWanFrequencyManagerTests
    {
        [Test]
        public void TestNextUplinkChannel_AllChannelsEnabled()
        {
            var manager = new LoRaWanFrequencyManager(new US915ChannelPlan());
            foreach (var channel in Enumerable.Range(0, 72))
            {
                var frequency = manager.GetNextUplinkFrequency();
                Console.WriteLine(frequency);
                Assert.That(frequency.ChannelNumber, Is.EqualTo(channel));
            }
        }

        [Test]
        public void TestNextUplinkChannel_TwoChannelsDisabled()
        {
            var manager = new LoRaWanFrequencyManager(new US915ChannelPlan());
            manager.SetChannelState(0, false);
            manager.SetChannelState(62, false);
            var frequency = manager.GetNextUplinkFrequency();
            Console.WriteLine(frequency);
            Assert.That(frequency.ChannelNumber, Is.EqualTo(1));
            for(var i = 0; i < 60; i++)
            {
                Console.WriteLine(manager.GetNextUplinkFrequency());
            }
            frequency = manager.GetNextUplinkFrequency();
            Console.WriteLine(frequency);
            Assert.That(frequency.ChannelNumber, Is.EqualTo(63));
            for(var i = 63; i < 71; i++)
            {
                Console.WriteLine(manager.GetNextUplinkFrequency());
            }
        }

        [Test]
        public void TestNextUplinkChannel_AllChannelsDisabled()
        {
            var manager = new LoRaWanFrequencyManager(new US915ChannelPlan());
            foreach (var channel in Enumerable.Range(0, 72))
            {
                manager.SetChannelState(channel, false);
            }
            Assert.Throws<NoAvailableChannelsException>(() => manager.GetNextUplinkFrequency());
        }
    }
}
