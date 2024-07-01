using System.Text;
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
        public override async Task Initialize()
        {
            var config = new Rfm9XConfiguration([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
                                                LoRaChannels.Us915Fsb2,
                                                Device,
                                                Device.CreateSpiBus(new Frequency(10, Frequency.UnitType.Megahertz)),
                                                Device.Pins.D00,
                                                Device.Pins.D01,
                                                Device.Pins.D03,
                                                Dio3: Device.Pins.D06,
                                                Dio4: Device.Pins.D05);
            _rfm9X = new Rfm9X(Resolver.Log,
                               config);

            // Needs to be in LSB format
            byte[] devEui = [0x06, 0x31, 0x00, 0xD8, 0x7E, 0xD5, 0xB3, 0x70];
            byte[] appKey = [0xA2, 0x66, 0xE8, 0x9F, 0x4E, 0x3A, 0xA7, 0x33, 0x18, 0x19, 0x94, 0x89, 0x38, 0xE5, 0x68, 0x67];

            _theThingsNetwork = new TheThingsNetwork(Resolver.Log, Device.PlatformOS, _rfm9X, devEui, appKey);
            await _theThingsNetwork.Initialize().ConfigureAwait(false);
            await base.Initialize().ConfigureAwait(false);
        }

        public override async Task Run()
        {
            await _theThingsNetwork.SendMessage("Hello!"u8.ToArray()).ConfigureAwait(false);
        }
    }
}