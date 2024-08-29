using System;
using System.Threading.Tasks;

namespace Meadow.Foundation.Radio.LoRa
{
    /// <summary>
    /// An interface describing a LoRa radio
    /// </summary>
    public interface ILoRaRadio : IRadio
    {
        /// <summary>
        /// Set the LoRa parameters for the radio
        /// </summary>
        /// <param name="parameters">The <see cref="LoRaParameters"/> for the radio</param>
        /// <returns>A <see cref="ValueTask"/> representing the operation</returns>
        public ValueTask SetLoRaParameters(LoRaParameters parameters);
    }

    /// <summary>
    /// A radio that can send and receive data
    /// </summary>
    public interface IRadio
    {
        /// <summary>
        /// The minimum transmit power of the radio in dBm
        /// </summary>
        public float MinimumTxPower { get; }

        /// <summary>
        /// The maximum transmit power of the radio in dBm
        /// </summary>
        public float MaximumTxPower { get; }

        /// <summary>
        /// Initialize the radio
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> representing the operation</returns>
        public ValueTask Initialize();

        /// <summary>
        /// Send a message with the radio
        /// </summary>
        /// <param name="messagePayload">The message to send</param>
        /// <returns>A <see cref="ValueTask"/> representing the operation</returns>
        public ValueTask Send(byte[] messagePayload);

        /// <summary>
        /// Receive a message with the radio
        /// </summary>
        /// <param name="timeout">The <see cref="TimeSpan"/> to wait for a message</param>
        /// <returns>A <see cref="ValueTask{Envelope}"/> representing the operation</returns>
        public ValueTask<Envelope> Receive(TimeSpan timeout);

        /// <summary>
        /// An event handler for when data is received
        /// </summary>
        public event EventHandler<RadioDataReceived>? OnReceived;

        /// <summary>
        /// An event handler for when data is transmitted
        /// </summary>
        public event EventHandler<RadioDataTransmitted>? OnTransmitted;
    }
}
