using System.Collections;
using sahajquinci.MQTT_Client.Messages;
using System.Collections.Generic;

namespace sahajquinci.MQTT_Broker.Session
{
    /// <summary>
    /// MQTT Session base class
    /// </summary>
    public abstract class MqttSession
    {
        /// <summary>
        /// Client Id
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Messages inflight during session , Key = MqttMsgContext , Value = packet identifier
        /// </summary>
        public Dictionary<ushort,MqttMsgContext> InflightMessages { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public MqttSession()
            : this(null)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="clientId">Client Id to create session</param>
        public MqttSession(string clientId)
        {
            this.ClientId = clientId;
            this.InflightMessages = new Dictionary<ushort, MqttMsgContext>();
        }

        /// <summary>
        /// Clean session
        /// </summary>
        public virtual void Clear()
        {
            this.ClientId = null;
            this.InflightMessages.Clear();
        }
    }
}
