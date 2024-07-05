using System;
using System.Threading.Tasks;

namespace Meadow.Foundation.Radio.LoRa
{
    /// <summary>
    /// An interface describing a LoRa radio
    /// </summary>
    public interface ILoRaRadio : IRadio
    {
        public ValueTask SetLoRaParameters(LoRaParameters parameters);
    }

    /// <summary>
    /// A radio that can send and receive data
    /// </summary>
    public interface IRadio
    {
        public ValueTask Initialize();
        public ValueTask Send(ReadOnlyMemory<byte> messagePayload);
        public ValueTask<Envelope> Receive(TimeSpan timeout);
        public event EventHandler<RadioDataReceived>? OnReceived;
        public event EventHandler<RadioDataTransmitted>? OnTransmitted;
    }
}
