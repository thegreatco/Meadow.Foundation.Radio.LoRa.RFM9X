using Meadow.Logging;

namespace Meadow.Foundation.Radio.LoRaWan
{
    /// <summary>
    /// Create a new instance of The Things Network
    /// </summary>
    /// <param name="logger">a logger to log with</param>
    /// <param name="radio">the radio to use to communicate with the network</param>
    /// <param name="devEui">the devEUI, in LSB format</param>
    /// <param name="appKey">the appKey, in MSB format</param>
    /// <param name="appEui">the appEUI, which i'm not sure if it does anything</param>
    public class TheThingsNetwork(Logger logger, IPlatformOS os, ILoRaRadio radio, byte[] devEui, AppKey appKey, byte[]? appEui = null)
        : LoRaWanNetwork(logger, os, radio, devEui, appKey, appEui);
}
