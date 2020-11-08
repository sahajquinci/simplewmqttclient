using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace sahajquinci.MQTT_Client.Events
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