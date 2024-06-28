using System;
using System.Threading.Tasks;

namespace Meadow.Foundation.Radio.LoRaWan
{
    public interface ILoRaRadio
    {
        public ValueTask Initialize();
        public ValueTask Send(byte[] messagePayload);
        public ValueTask<Envelope> SendAndReceive(byte[] messagePayload, TimeSpan timeout);
        public ValueTask<Envelope> Receive(TimeSpan timeout);
    }

    public readonly record struct Envelope(byte[] Address, byte[] MessagePayload)
    {
        public byte[] Address { get; } = Address;
        public byte[] MessagePayload { get; } = MessagePayload;
    }
}
