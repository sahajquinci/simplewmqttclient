using System;

namespace SimplMqttClient.Events
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public string Topic { get ; private set;}
        public string Value { get; private set; }

        /// <summary>
        /// è necessario definire il costruttore di default affinchè simpl+ veda le properties.
        /// </summary>
        public MessageReceivedEventArgs()
        {
            ;
        }

        public MessageReceivedEventArgs(string topic, string value)
        {
            this.Topic = topic;
            this.Value = value;
        }
    }
}