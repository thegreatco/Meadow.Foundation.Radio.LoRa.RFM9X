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

    public readonly record struct Envelope(MessageType MessageType, byte[] MessagePayload)
    {
        public MessageType MessageType { get; } = MessageType;
        public byte[] MessagePayload { get; } = MessagePayload;
    }

    public enum MessageType : byte
    {
        JoinRequest = 0b00000000,
        JoinAccept = 0b00100000,
        UnconfirmedDataUp = 0b01000000,
        UnconfirmedDataDown = 0b01100000,
        ConfirmedDataUp = 0b10000000,
        ConfirmedDataDown = 0b10100000,
        Rfu = 0b11000000,
        Reserved = 0b11100000
    }
}
