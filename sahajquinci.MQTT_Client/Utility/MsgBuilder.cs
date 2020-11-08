using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using sahajquinci.MQTT_Client.Messages;
using Crestron.SimplSharp.CrestronLogger;
namespace sahajquinci.MQTT_Client.Utility
{
    /// <summary>
    /// Each method returns an mqtt control packet 
    /// </summary>
    public static class MsgBuilder
    {

        public static MqttMsgConnack BuildConnack(bool sp, byte returnCode)
        {
            MqttMsgConnack connack = new MqttMsgConnack();
            //Fixed header first byte
            byte fixedHeaderB1 = (byte)(MqttMsgBase.MQTT_MSG_CONNECT_TYPE << MqttMsgBase.MSG_TYPE_OFFSET);
            //Variable header bytes
            byte variableHeaderB1 = (sp) ? (byte)0x01 : (byte)0x00;
            byte variableHeaderB2 = returnCode;

            int remainingLenght = variableHeaderB1 + variableHeaderB2;

            byte[] data = new byte[fixedHeaderB1 + remainingLenght];
            data[0] = fixedHeaderB1;
            connack.encodeRemainingLength(remainingLenght, data, 1);
            return connack;
        }

        public static MqttMsgPublish BuildPublish(string topic, bool dupFlag, byte qosLevel, bool retain, byte[] message, ushort messageId)
        {
            MqttMsgPublish publish = new MqttMsgPublish();
            publish.DupFlag = dupFlag;
            publish.QosLevel = qosLevel;
            publish.Retain = retain;
            publish.Topic = topic;
            publish.MessageId = messageId;
            publish.Message = message;
            return publish;
        }

        public static MqttMsgPingReq BuildPingReq()
        {
            MqttMsgPingReq request = new MqttMsgPingReq();
            return request;
        }

        public static MqttMsgPingResp BuildPingResp()
        {
            MqttMsgPingResp request = new MqttMsgPingResp();
            return request;
        }

        public static MqttMsgPuback BuildPubAck(ushort messageId)
        {
            MqttMsgPuback pubAck = new MqttMsgPuback();
            pubAck.MessageId = messageId;
            return pubAck;
        }


        public static MqttMsgPubrec BuildPubRec(ushort messagId)
        {
            MqttMsgPubrec pubRec = new MqttMsgPubrec();
            pubRec.MessageId = messagId;
            return pubRec;
        }

        public static MqttMsgPubcomp BuildPubComp(ushort messageId)
        {
            MqttMsgPubcomp pubComp = new MqttMsgPubcomp();
            pubComp.MessageId = messageId;
            return pubComp;
        }

        public static MqttMsgSuback BuildSubAck(ushort messageId, byte[] qosLevels)
        {
            MqttMsgSuback subAck = new MqttMsgSuback();
            subAck.MessageId = messageId;
            subAck.GrantedQoSLevels = qosLevels;
            return subAck;
        }

        public static MqttMsgUnsuback BuildUnSubAck(ushort messageId)
        {
            MqttMsgUnsuback unSubAck = new MqttMsgUnsuback();
            unSubAck.MessageId = messageId;
            return unSubAck;
        }

        public static MqttMsgPubrel BuildPubRel(ushort messageId)
        {
            MqttMsgPubrel pubRel = new MqttMsgPubrel();
            pubRel.MessageId = messageId;
            return pubRel;
        }

        public static MqttMsgConnect BuildConnect(string clientId,
            string username,
            string password,
            bool willRetain,
            byte willQosLevel,
            bool willFlag,
            string willTopic,
            string willMessage,
            bool cleanSession,
            ushort keepAlivePeriod,
            byte protocolVersion)
        {
            return new MqttMsgConnect(clientId, username, password, willRetain, willQosLevel, willFlag, willTopic, willMessage, cleanSession, keepAlivePeriod, protocolVersion);
        }

        internal static MqttMsgSubscribe BuildSubscribe(string[] topics , byte[] qosLevels , ushort messageId)
        {
            MqttMsgSubscribe subscribe = new MqttMsgSubscribe();
            subscribe.Topics = topics.ToArray();
            subscribe.QoSLevels = qosLevels;
            subscribe.MessageId = messageId;
            return subscribe;
        }

        internal static MqttMsgDisconnect BuildDisconnect()
        {
            return new MqttMsgDisconnect();
        }
    }
}