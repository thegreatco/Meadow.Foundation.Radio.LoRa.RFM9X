using Meadow.Units;

namespace Meadow.Foundation.Radio.LoRa
{
    public record struct LoRaParameters(
        Frequency Frequency,
        Frequency Bandwidth,
        CodingRate CodingRate,
        SpreadingFactor SpreadingFactor,
        bool ImplicitHeaderMode,
        bool CrcMode,
        bool InvertIq,
        byte SyncWord = 0x34);

    public enum SpreadingFactor
    {
        Sf6,
        Sf7,
        Sf8,
        Sf9,
        Sf10,
        Sf11,
        Sf12
    }

    public enum CodingRate
    {
        Cr45,
        Cr46,
        Cr47,
        Cr48
    }
}
