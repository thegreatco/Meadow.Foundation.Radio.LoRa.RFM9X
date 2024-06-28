using Meadow.Devices;
using System.Threading.Tasks;
using Meadow.Foundation.Radio.LoRaWan;
using Meadow.Units;

namespace Meadow.Foundation.Radio.LoRa.RFM9X
{
    public class MeadowApp : App<F7FeatherV1>
    {
        private Rfm9X _rfm9X;
        private TheThingsNetwork _theThingsNetwork;
        public override Task Initialize()
        {
            var config = new Rfm9XConfiguration([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
                                                LoRaChannels.Us915Fsb2,
                                                Device,
                                                Device.CreateSpiBus(new Frequency(10, Frequency.UnitType.Megahertz)),
                                                Device.Pins.D00,
                                                Device.Pins.D01,
                                                Device.Pins.D03);
            _rfm9X = new Rfm9X(Resolver.Log,
                               config);

            byte[] devEui = [0xBC, 0x30, 0x00, 0xD8, 0x7E, 0xD5, 0xB3, 0x70];
            byte[] appKey =
                [0x21, 0xEE, 0xF7, 0xC6, 0x74, 0x14, 0x71, 0x72, 0xEB, 0x5F, 0x4C, 0x79, 0xA9, 0x46, 0xC9, 0xE2];

            _theThingsNetwork = new TheThingsNetwork(Resolver.Log, _rfm9X, devEui, appKey);
            return base.Initialize();
        }

        public override async Task Run()
        {
            await _theThingsNetwork.Initialize()
                                   .ConfigureAwait(false);
        }
    }
}