using SimplMqttClient.Exceptions;

namespace SimplMqttClient.Messages
{
    /// <summary>
    /// Class for DISCONNECT message from client to broker
    /// </summary>
    public class MqttMsgDisconnect : MqttMsgBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public MqttMsgDisconnect()
        {
            this.type = MQTT_MSG_DISCONNECT_TYPE;
        }

        /// <summary>
        /// Parse bytes for a DISCONNECT message
        /// </summary>
        /// <param name="fixedHeaderFirstByte">First fixed header byte</param>
        /// <param name="protocolVersion">Protocol Version</param>
        /// <param name="channel">Channel connected to the broker</param>
        /// <returns>DISCONNECT message instance</returns>
        public static MqttMsgDisconnect Parse(byte[] data)
        {
            MqttMsgDisconnect msg = new MqttMsgDisconnect();
            byte fixedHeaderFirstByte = data[0];
                // [v3.1.1] check flag bits
                if ((fixedHeaderFirstByte & MSG_FLAG_BITS_MASK) != MQTT_MSG_DISCONNECT_FLAG_BITS)
                    throw new MqttClientException(MqttClientErrorCode.InvalidFlagBits);

            // get remaining length and allocate buffer
            int remainingLength = MqttMsgBase.decodeRemainingLength(data);
            // NOTE : remainingLength must be 0

            return msg;
        }

        public override byte[] GetBytes(byte protocolVersion)
        {
            byte[] buffer = new byte[2];
            int index = 0;

            // first fixed header byte
            if (protocolVersion == MqttMsgConnect.PROTOCOL_VERSION_V3_1_1)
                buffer[index++] = (MQTT_MSG_DISCONNECT_TYPE << MSG_TYPE_OFFSET) | MQTT_MSG_DISCONNECT_FLAG_BITS; // [v.3.1.1]
            else
                buffer[index++] = (MQTT_MSG_DISCONNECT_TYPE << MSG_TYPE_OFFSET);
            buffer[index++] = 0x00;

            return buffer;
        }

        public override string ToString()
        {
#if TRACE
            return this.GetTraceString(
                "DISCONNECT",
                null,
                null);
#else
            return base.ToString();
#endif
        }
    }
}
