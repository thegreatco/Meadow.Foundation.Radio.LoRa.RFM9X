using Meadow.Units;

using System.Text;

namespace Meadow.Foundation.Radio.LoRa
{
    public record struct LoRaParameters(
        Frequency Frequency,
        Frequency Bandwidth,
        int TxPower,
        CodingRate CodingRate,
        SpreadingFactor SpreadingFactor,
        bool ImplicitHeaderMode,
        bool CrcMode,
        bool InvertIq,
        byte SyncWord = 0x34)
    {
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Frequency          {Frequency}");
            sb.AppendLine($"Bandwidth          {Bandwidth}");
            sb.AppendLine($"Power              {TxPower} dBm");
            sb.AppendLine($"CodingRate         {CodingRate}");
            sb.AppendLine($"SpreadingFactor    {SpreadingFactor}");
            sb.AppendLine($"ImplicitHeaderMode {ImplicitHeaderMode}");
            sb.AppendLine($"CrcMode            {CrcMode}");
            sb.AppendLine($"InvertIq           {InvertIq}");
            sb.AppendLine($"SyncWord           {SyncWord:X2}");
            return sb.ToString();
        }
    }

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
