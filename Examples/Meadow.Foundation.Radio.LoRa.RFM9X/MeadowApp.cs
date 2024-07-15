using System;
using System.Text;
using System.Threading.Tasks;
using Meadow.Devices;
using Meadow.Foundation.Radio.LoRaWan;
using Meadow.Units;

namespace Meadow.Foundation.Radio.Sx127X
{
    public class MeadowApp : App<F7FeatherV1>
    {
        private Sx127X _sx127X;
        private TheThingsNetwork _theThingsNetwork;
        public override async Task Initialize()
        {
            var config = new Sx172XConfiguration([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
                                                Device,
                                                Device.CreateSpiBus(new Frequency(10, Frequency.UnitType.Megahertz)),
                                                Device.Pins.D00,
                                                Device.Pins.D01,
                                                Device.Pins.D03,
                                                Dio3: Device.Pins.D06,
                                                Dio4: Device.Pins.D05);
            _sx127X = new Sx127X(Resolver.Log, config);

            // Needs to be in LSB format
            var devEui = new DevEui([0x06, 0x31, 0x00, 0xD8, 0x7E, 0xD5, 0xB3, 0x70]);
            var appKey = new AppKey([0xA2, 0x66, 0xE8, 0x9F, 0x4E, 0x3A, 0xA7, 0x33, 0x18, 0x19, 0x94, 0x89, 0x38, 0xE5, 0x68, 0x67]);
            var joinEui = new JoinEui([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
            
            var loRaWanParameters = new LoRaWanParameters(new US915ChannelPlan(DataRate.DR4), appKey, devEui, joinEui);
            _theThingsNetwork = new TheThingsNetwork(Resolver.Log, _sx127X, loRaWanParameters);
            await _theThingsNetwork.Initialize().ConfigureAwait(false);
            await base.Initialize().ConfigureAwait(false);
        }

        public override async Task Run()
        {
            while (true)
            {
                var str = $"Hello";
                Resolver.Log.Info(str);
                await _theThingsNetwork.SendMessage(Encoding.UTF8.GetBytes(str))
                                       .ConfigureAwait(false);

                await Task.Delay(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            }
        }
    }
}