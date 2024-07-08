using System;
using System.Threading.Tasks;

namespace Meadow.Foundation.Radio.LoRa
{
    public abstract class LoRaRadio : ILoRaRadio
    {
        /// <summary>
        /// Initialize the hardware
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> that completes when the hardware has finished initializing</returns>
        public abstract ValueTask Initialize();

        /// <summary>
        /// Send a message
        /// </summary>
        /// <param name="messagePayload">The message to send</param>
        /// <returns>A <see cref="ValueTask"/> that completes when the radio is done transmitting</returns>
        public abstract ValueTask Send(byte[] messagePayload);

        /// <summary>
        /// Receive a message
        /// </summary>
        /// <param name="timeout">The amount of time to wait for the message</param>
        /// <returns>The <see cref="Envelope"/> containing the response</returns>
        public abstract ValueTask<Envelope> Receive(TimeSpan timeout);

        /// <summary>
        /// Send a message and wait for a response
        /// </summary>
        /// <param name="messagePayload">The message to send</param>
        /// <param name="timeout">The time to wait for a response</param>
        /// <returns>The <see cref="Envelope"/> containing the response</returns>
        public async ValueTask<Envelope> SendAndReceive(byte[] messagePayload, TimeSpan timeout)
        {
            await Send(messagePayload);
            return await Receive(timeout);
        }

        public abstract ValueTask SetLoRaParameters(LoRaParameters parameters);

        /// <summary>
        /// Invoke the <see cref="OnReceived"/> event
        /// </summary>
        /// <param name="e">The <see cref="RadioDataReceived"/> event args</param>
        protected void OnReceivedHandler(RadioDataReceived e)
        {
            OnReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Invoke the <see cref="OnTransmitted"/> event
        /// </summary>
        /// <param name="e">The <see cref="RadioDataTransmitted"/> event args</param>
        protected void OnTransmittedHandler(RadioDataTransmitted e)
        {
            OnTransmitted?.Invoke(this, e);
        }

        /// <summary>
        /// A handler that fires each time data is received by the radio
        /// </summary>
        public event EventHandler<RadioDataReceived>? OnReceived;

        /// <summary>
        /// A handler that fires each time data is transmitted by the radio
        /// </summary>
        public event EventHandler<RadioDataTransmitted>? OnTransmitted;
    }
}
