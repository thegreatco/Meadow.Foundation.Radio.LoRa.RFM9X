using Meadow.Logging;

namespace Meadow.Foundation.Radio.LoRaWan
{
    public class TheThingsNetwork(Logger logger, ILoRaRadio radio, byte[] devEui, byte[] appKey, byte[]? appEui = null)
        : LoRaWanNetwork(logger, radio, devEui, appKey, appEui);
}
