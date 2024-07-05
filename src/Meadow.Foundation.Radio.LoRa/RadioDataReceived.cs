namespace Meadow.Foundation.Radio.LoRa
{
    public record RadioDataReceived(Envelope Envelope)
    {
        public Envelope Envelope { get; } = Envelope;
    }
}
