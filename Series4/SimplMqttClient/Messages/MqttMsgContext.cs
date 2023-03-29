using SimplMqttClient.Messages.Enums;

namespace SimplMqttClient.Messages
{
    /// <summary>
    /// Context for MQTT message
    /// </summary>
    public class MqttMsgContext
    {
        /// <summary>
        /// MQTT message
        /// </summary>
        public MqttMsgBase Message { get; set; }

        /// <summary>
        /// MQTT message state
        /// </summary>
        public MqttMsgState State { get; set; }

        /// <summary>
        /// Attempt (for retry)
        /// </summary>
        public int Attempt { get; set; }

        /// <summary>
        /// Unique key
        /// </summary>
        public ushort Key
        {
            get { return this.Message.MessageId; }
        }
    }
}
