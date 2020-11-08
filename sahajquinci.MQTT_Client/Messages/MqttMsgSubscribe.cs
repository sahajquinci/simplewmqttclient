using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using sahajquinci.MQTT_Client.Exceptions;
using Crestron.SimplSharp.CrestronLogger;

namespace sahajquinci.MQTT_Client.Messages
{
    /// <summary>
    /// Class for SUBSCRIBE message from client to broker
    /// </summary>
    public class MqttMsgSubscribe : MqttMsgBase
    {
        #region Properties...

        /// <summary>
        /// List of topics to subscribe
        /// </summary>
        public string[] Topics
        {
            get { return this.topics; }
            set { this.topics = value; }
        }

        /// <summary>
        /// List of QOS Levels related to topics
        /// </summary>
        public byte[] QoSLevels
        {
            get { return this.qosLevels; }
            set { this.qosLevels = value; }
        }

        #endregion

        // topics to subscribe
        string[] topics;
        // QOS levels related to topics
        byte[] qosLevels;

        /// <summary>
        /// Constructor
        /// </summary>
        public MqttMsgSubscribe()
        {
            this.type = MQTT_MSG_SUBSCRIBE_TYPE;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="topics">List of topics to subscribe</param>
        /// <param name="qosLevels">List of QOS Levels related to topics</param>
        public MqttMsgSubscribe(string[] topics, byte[] qosLevels)
        {
            this.type = MQTT_MSG_SUBSCRIBE_TYPE;

            this.topics = topics;
            this.qosLevels = qosLevels;

            // SUBSCRIBE message uses QoS Level 1 (not "officially" in 3.1.1)
            this.qosLevel = QOS_LEVEL_AT_LEAST_ONCE;
        }

        /// <summary>
        /// Parse bytes for a SUBSCRIBE message
        /// </summary>
        /// <param name="fixedHeaderFirstByte">First fixed header byte</param>
        /// <param name="protocolVersion">Protocol Version</param>
        /// <param name="channel">Channel connected to the broker</param>
        /// <returns>SUBSCRIBE message instance</returns>
        public static MqttMsgSubscribe Parse(byte[] data)
        {
            byte[] buffer;
            int index = 0;
            byte[] topicUtf8;
            int topicUtf8Length;
            MqttMsgSubscribe msg = new MqttMsgSubscribe();
            byte fixedHeaderFirstByte = data[0];
            // [v3.1.1] check flag bits
            if ((fixedHeaderFirstByte & MSG_FLAG_BITS_MASK) != MQTT_MSG_SUBSCRIBE_FLAG_BITS)
                throw new MqttClientException(MqttClientErrorCode.InvalidFlagBits);

            // get remaining length and allocate buffer
            int remainingLength = MqttMsgBase.decodeRemainingLength(data);
            MqttMsgBase tmp = new MqttMsgSubscribe();
            int remainingLenghtIndex = tmp.encodeRemainingLength(remainingLength, data, 1);
            buffer = new byte[remainingLength];
            // buffer is filled with remaing lenght...
            for (int i = remainingLenghtIndex, j = 0; j < remainingLength; i++, j++)
            {
                buffer[j] = data[i];
            }
            // read bytes from socket...
            int received = data.Length;

            // message id
            msg.messageId = (ushort)((buffer[index++] << 8) & 0xFF00);
            msg.messageId |= (buffer[index++]);
            // payload contains topics and QoS levels
            // NOTE : before, I don't know how many topics will be in the payload (so use List)

            // if .Net Micro Framework
            IList<String> tmpTopics = new List<String>();
            IList<byte> tmpQosLevels = new List<byte>();
            do
            {
                // topic name
                topicUtf8Length = ((buffer[index++] << 8) & 0xFF00);
                topicUtf8Length |= buffer[index++];
                topicUtf8 = new byte[topicUtf8Length];
                Array.Copy(buffer, index, topicUtf8, 0, topicUtf8Length);
                index += topicUtf8Length;
                tmpTopics.Add(new String(Encoding.UTF8.GetChars(topicUtf8)));

                // QoS level
                tmpQosLevels.Add(buffer[index++]);

            } while (index < remainingLength);
            // copy from list to array
            msg.topics = new string[tmpTopics.Count];
            msg.qosLevels = new byte[tmpQosLevels.Count];
            for (int i = 0; i < tmpTopics.Count; i++)
            {
                msg.topics[i] = (string)tmpTopics[i];
                msg.qosLevels[i] = (byte)tmpQosLevels[i];
            }
            foreach (var t in msg.topics)
            {
                CrestronLogger.WriteToLog("SUBSCRIBE ,Topics : " + t, 6);
            }
            CrestronLogger.WriteToLog("SUBSCRIBE ,parse SUCCESS", 6);
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

            // qos levels list empty
            if ((this.qosLevels == null) || (this.qosLevels.Length == 0))
                throw new MqttClientException(MqttClientErrorCode.QosLevelsEmpty);

            // topics and qos levels lists length don't match
            if (this.topics.Length != this.qosLevels.Length)
                throw new MqttClientException(MqttClientErrorCode.TopicsQosLevelsNotMatch);

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
                payloadSize++; // byte for QoS
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
                buffer[index++] = (MQTT_MSG_SUBSCRIBE_TYPE << MSG_TYPE_OFFSET) | MQTT_MSG_SUBSCRIBE_FLAG_BITS; // [v.3.1.1]
            else
            {
                buffer[index] = (byte)((MQTT_MSG_SUBSCRIBE_TYPE << MSG_TYPE_OFFSET) |
                                   (this.qosLevel << QOS_LEVEL_OFFSET));
                buffer[index] |= this.dupFlag ? (byte)(1 << DUP_FLAG_OFFSET) : (byte)0x00;
                index++;
            }

            // encode remaining length
            index = this.encodeRemainingLength(remainingLength, buffer, index);

            // check message identifier assigned (SUBSCRIBE uses QoS Level 1, so message id is mandatory)
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

                // requested QoS
                buffer[index++] = this.qosLevels[topicIdx];
            }

            return buffer;
        }

        public override string ToString()
        {
#if TRACE
            return this.GetTraceString(
                "SUBSCRIBE",
                new object[] { "messageId", "topics", "qosLevels" },
                new object[] { this.messageId, this.topics, this.qosLevels });
#else
            return base.ToString();
#endif
        }
    }
}
