using System;
using SimplMqttClient.Messages;

namespace SimplMqttClient.Events
{
    public class PacketToSendEventArgs : EventArgs
    {
        public MqttMsgBase Packet { get; private set; }

        public PacketToSendEventArgs(MqttMsgBase packet)
        {
            this.Packet = packet;
        }
    }
}