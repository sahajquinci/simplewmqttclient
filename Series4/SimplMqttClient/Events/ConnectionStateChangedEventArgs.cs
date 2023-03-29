using System;

namespace SimplMqttClient.Events
{
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public ushort State { get; private set; }

        public ConnectionStateChangedEventArgs()
        {
            ;
        }

        public ConnectionStateChangedEventArgs(ushort state)
        {
            State = state;
        }
    }
}