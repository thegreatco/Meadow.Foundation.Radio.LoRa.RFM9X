using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.Foundation.Radio.LoRaWan
{
    public class MicMismatchException(Mic expected, Mic actual) : Exception($"Invalid Message Integrity Code (MIC) detected. Expected: {expected.Value.ToHexString(false)}, Actual: {actual.Value.ToHexString(false)}");
}
