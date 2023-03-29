using System;
using SimplMqttClient.Messages;

namespace SimplMqttClient.Utility
{
    public class PacketDecoder
    {
        internal const byte MSG_TYPE_OFFSET = 0x04;

        public MqttMsgBase DecodeControlPacket(byte[] data)
        {
            byte fixedHeaderFirstByte = (byte)(data[0] >> MSG_TYPE_OFFSET);
            switch (fixedHeaderFirstByte)
            {
                case MqttMsgBase.MQTT_MSG_CONNECT_TYPE:
                    {
                        return MqttMsgConnect.Parse(data);
                    }
                case MqttMsgBase.MQTT_MSG_CONNACK_TYPE:
                    {
                        return MqttMsgConnack.Parse(data);
                    }
                case MqttMsgBase.MQTT_MSG_PUBLISH_TYPE:
                    {
                        return MqttMsgPublish.Parse(data);
                    }
                case MqttMsgBase.MQTT_MSG_PUBACK_TYPE:
                    {
                        return MqttMsgPuback.Parse(data);
                    }
                case MqttMsgBase.MQTT_MSG_PUBREC_TYPE:
                    {
                        return MqttMsgPubrec.Parse(data);
                    }
                case MqttMsgBase.MQTT_MSG_PUBREL_TYPE:
                    {
                        return MqttMsgPubrel.Parse(data);
                    }
                case MqttMsgBase.MQTT_MSG_PUBCOMP_TYPE:
                    {
                        return MqttMsgPubcomp.Parse(data);
                    }
                case MqttMsgBase.MQTT_MSG_SUBSCRIBE_TYPE:
                    {
                        return MqttMsgSubscribe.Parse(data);
                    }
                 case MqttMsgBase.MQTT_MSG_SUBACK_TYPE:
                      {
                          return MqttMsgSuback.Parse(data);
                      }
                case MqttMsgBase.MQTT_MSG_UNSUBSCRIBE_TYPE:
                    {
                        return MqttMsgUnsubscribe.Parse(data);
                    }
                  case MqttMsgBase.MQTT_MSG_UNSUBACK_TYPE:
                     {
                         return MqttMsgUnsuback.Parse(data);
                     }
                case MqttMsgBase.MQTT_MSG_PINGREQ_TYPE:
                    {
                        return MqttMsgPingReq.Parse(data);
                    }
                case MqttMsgBase.MQTT_MSG_PINGRESP_TYPE:
                    {
                        return MqttMsgPingResp.Parse(data);
                    }
                case MqttMsgBase.MQTT_MSG_DISCONNECT_TYPE:
                    {
                        return MqttMsgDisconnect.Parse(data);
                    }
                default:
                    {
                        throw new FormatException(" First byte shifted : " + fixedHeaderFirstByte);
                    }
            }
        }
    }
}
