using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.Foundation.Radio.LoRa
{
    /// <summary>
    /// An envelope that contains the message payload and metadata about the received signal
    /// </summary>
    /// <param name="MessagePayload">The bytes received by the radio</param>
    /// <param name="Snr">The signal-to-noise ratio in dBm</param>
    public readonly record struct Envelope(byte[] MessagePayload, int Snr)
    {
        public byte[] MessagePayload { get; } = MessagePayload;
        public int Snr {get;} = Snr;
    }
}
