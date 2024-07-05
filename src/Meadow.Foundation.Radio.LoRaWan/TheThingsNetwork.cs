using Meadow.Foundation.Radio.LoRa;
using Meadow.Logging;

namespace Meadow.Foundation.Radio.LoRaWan
{
    /// <param name="radio">the radio to use to communicate with the network</param>
    public class TheThingsNetwork(Logger logger, ILoRaRadio radio, LoRaWanParameters parameters)
        : LoRaWanNetwork(logger, radio, parameters);
}
