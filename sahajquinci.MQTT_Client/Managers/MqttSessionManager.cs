using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using sahajquinci.MQTT_Broker.Session;
using sahajquinci.MQTT_Client.Messages;

namespace sahajquinci.MQTT_Client.Managers
{
    public class MqttSessionManager
    {
        private string clientId;
        private MqttClientSession session;
        public MqttSessionManager(string clientId)
        {
            this.clientId = clientId;
            session = new MqttClientSession(clientId);
        }

        public void CleanSession()
        {
            session.Clear();
            session = new MqttClientSession(clientId);
        }

        delegate MqttMsgState Del();
        public void AddInflightMessage(MqttMsgBase packet)
        {
            MqttMsgContext context = new MqttMsgContext();
            Del d = delegate() { return packet.QosLevel == (byte)0x01 ? MqttMsgState.QueuedQos1 : MqttMsgState.QueuedQos2; };
            MqttMsgState state = packet.QosLevel == (byte)0x00 ? MqttMsgState.QueuedQos0 : d();
            context.State = state;
            context.Message = packet;
            session.InflightMessages.Add(packet.MessageId, context);
        }
        public void ChangeMsgState(ushort messageId, MqttMsgState state)
        {
            session.InflightMessages[messageId].State = state;
        }

        public void RemoveInflightMessage(ushort messageId)
        {
            if (session.InflightMessages.ContainsKey(messageId))
            {
                session.InflightMessages.Remove(messageId);
            }
            else
            {
                throw new ArgumentNullException();
            }
        }

    }
}