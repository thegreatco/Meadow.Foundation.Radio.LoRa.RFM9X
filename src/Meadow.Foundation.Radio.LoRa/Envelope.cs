using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.Foundation.Radio.LoRa
{
    public readonly record struct Envelope(byte[] MessagePayload)
    {
        public byte[] MessagePayload { get; } = MessagePayload;
    }
}
