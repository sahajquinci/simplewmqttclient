using System;
using System.Collections.Generic;
using System.Text;
using SimplMqttClient.Exceptions;
// if NOT .Net Micro Framework
#if (!MF_FRAMEWORK_VERSION_V4_2 && !MF_FRAMEWORK_VERSION_V4_3)
#endif

namespace SimplMqttClient.Messages
{
    /// <summary>
    /// Class for UNSUBSCRIBE message from client to broker
    /// </summary>
    public class MqttMsgUnsubscribe : MqttMsgBase
    {
        #region Properties...

        /// <summary>
        /// List of topics to unsubscribe
        /// </summary>
        public string[] Topics
        {
            get { return this.topics; }
            set { this.topics = value; }
        }

        #endregion

        // topics to unsubscribe
        string[] topics;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public MqttMsgUnsubscribe()
        {
            this.type = MQTT_MSG_UNSUBSCRIBE_TYPE;
        }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="topics">List of topics to unsubscribe</param>
        public MqttMsgUnsubscribe(string[] topics)
        {
            this.type = MQTT_MSG_UNSUBSCRIBE_TYPE;

            this.topics = topics;

            // UNSUBSCRIBE message uses QoS Level 1 (not "officially" in 3.1.1)
            this.qosLevel = QOS_LEVEL_AT_LEAST_ONCE;
        }

        /// <summary>
        /// Parse bytes for a UNSUBSCRIBE message
        /// </summary>
        /// <param name="fixedHeaderFirstByte">First fixed header byte</param>
        /// <param name="protocolVersion">Protocol Version</param>
        /// <param name="channel">Channel connected to the broker</param>
        /// <returns>UNSUBSCRIBE message instance</returns>
        public static MqttMsgUnsubscribe Parse(byte[] data)
        {
            byte[] buffer;
            int index = 0;
            byte[] topicUtf8;
            int topicUtf8Length;
            MqttMsgUnsubscribe msg = new MqttMsgUnsubscribe();
            byte fixedHeaderFirstByte = data[0];
                // [v3.1.1] check flag bits
                if ((fixedHeaderFirstByte & MSG_FLAG_BITS_MASK) != MQTT_MSG_UNSUBSCRIBE_FLAG_BITS)
                    throw new MqttClientException(MqttClientErrorCode.InvalidFlagBits);

            // get remaining length and allocate buffer
            int remainingLength = MqttMsgBase.decodeRemainingLength(data);
            buffer = new byte[remainingLength];
            // buffer is filled with remaing lenght...
            for (int i = 2, j = 0; j < remainingLength; i++, j++)
            {
                buffer[j] = data[i];
            }
            // read bytes from socket...
            int received = data.Length;

            // message id
            msg.messageId = (ushort)((buffer[index++] << 8) & 0xFF00);
            msg.messageId |= (buffer[index++]);

            // payload contains topics
            // NOTE : before, I don't know how many topics will be in the payload (so use List)

            IList<String> tmpTopics = new List<String>();
            do
            {
                // topic name
                topicUtf8Length = ((buffer[index++] << 8) & 0xFF00);
                topicUtf8Length |= buffer[index++];
                topicUtf8 = new byte[topicUtf8Length];
                Array.Copy(buffer, index, topicUtf8, 0, topicUtf8Length);
                index += topicUtf8Length;
                tmpTopics.Add(new String(Encoding.UTF8.GetChars(topicUtf8)));
            } while (index < remainingLength);

            // copy from list to array
            msg.topics = new string[tmpTopics.Count];
            for (int i = 0; i < tmpTopics.Count; i++)
            {
                msg.topics[i] = (string)tmpTopics[i];
            }

            return msg;
        }

        public override byte[] GetBytes(byte protocolVersion)
        {
            int fixedHeaderSize = 0;
            int varHeaderSize = 0;
            int payloadSize = 0;
            int remainingLength = 0;
            byte[] buffer;
            int index = 0;

            // topics list empty
            if ((this.topics == null) || (this.topics.Length == 0))
                throw new MqttClientException(MqttClientErrorCode.TopicsEmpty);

            // message identifier
            varHeaderSize += MESSAGE_ID_SIZE;

            int topicIdx = 0;
            byte[][] topicsUtf8 = new byte[this.topics.Length][];

            for (topicIdx = 0; topicIdx < this.topics.Length; topicIdx++)
            {
                // check topic length
                if ((this.topics[topicIdx].Length < MIN_TOPIC_LENGTH) || (this.topics[topicIdx].Length > MAX_TOPIC_LENGTH))
                    throw new MqttClientException(MqttClientErrorCode.TopicLength);

                topicsUtf8[topicIdx] = Encoding.UTF8.GetBytes(this.topics[topicIdx]);
                payloadSize += 2; // topic size (MSB, LSB)
                payloadSize += topicsUtf8[topicIdx].Length;
            }

            remainingLength += (varHeaderSize + payloadSize);

            // first byte of fixed header
            fixedHeaderSize = 1;

            int temp = remainingLength;
            // increase fixed header size based on remaining length
            // (each remaining length byte can encode until 128)
            do
            {
                fixedHeaderSize++;
                temp = temp / 128;
            } while (temp > 0);

            // allocate buffer for message
            buffer = new byte[fixedHeaderSize + varHeaderSize + payloadSize];

            // first fixed header byte
            if (protocolVersion == MqttMsgConnect.PROTOCOL_VERSION_V3_1_1)
                buffer[index++] = (MQTT_MSG_UNSUBSCRIBE_TYPE << MSG_TYPE_OFFSET) | MQTT_MSG_UNSUBSCRIBE_FLAG_BITS; // [v.3.1.1]
            else
            {
                buffer[index] = (byte)((MQTT_MSG_UNSUBSCRIBE_TYPE << MSG_TYPE_OFFSET) |
                                   (this.qosLevel << QOS_LEVEL_OFFSET));
                buffer[index] |= this.dupFlag ? (byte)(1 << DUP_FLAG_OFFSET) : (byte)0x00;
                index++;
            }
            
            // encode remaining length
            index = this.encodeRemainingLength(remainingLength, buffer, index);

            // check message identifier assigned
            if (this.messageId == 0)
                throw new MqttClientException(MqttClientErrorCode.WrongMessageId);
            buffer[index++] = (byte)((messageId >> 8) & 0x00FF); // MSB
            buffer[index++] = (byte)(messageId & 0x00FF); // LSB 

            topicIdx = 0;
            for (topicIdx = 0; topicIdx < this.topics.Length; topicIdx++)
            {
                // topic name
                buffer[index++] = (byte)((topicsUtf8[topicIdx].Length >> 8) & 0x00FF); // MSB
                buffer[index++] = (byte)(topicsUtf8[topicIdx].Length & 0x00FF); // LSB
                Array.Copy(topicsUtf8[topicIdx], 0, buffer, index, topicsUtf8[topicIdx].Length);
                index += topicsUtf8[topicIdx].Length;
            }
            
            return buffer;
        }

        public override string ToString()
        {
#if TRACE
            return this.GetTraceString(
                "UNSUBSCRIBE",
                new object[] { "messageId", "topics" },
                new object[] { this.messageId, this.topics });
#else
            return base.ToString();
#endif
        }
    }
}
