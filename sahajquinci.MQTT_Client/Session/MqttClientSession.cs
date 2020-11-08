

namespace sahajquinci.MQTT_Broker.Session
{
    /// <summary>
    /// MQTT Client Session
    /// </summary>
    public class MqttClientSession : MqttSession
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="clientId">Client Id to create session</param>
        public MqttClientSession(string clientId)
            : base(clientId)
        {
        }
    }
}
