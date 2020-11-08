using System;
using Crestron.SimplSharp;

namespace sahajquinci.MQTT_Client.Messages
{
    /// <summary>
    /// MQTT message state
    /// </summary>
    public enum MqttMsgState
    {
        /// <summary>
        /// QOS = 0, Message queued
        /// </summary>
        QueuedQos0,

        /// <summary>
        /// QOS = 1, Message queued
        /// </summary>
        QueuedQos1,

        /// <summary>
        /// QOS = 2, Message queued
        /// </summary>
        QueuedQos2,

        /// <summary>
        /// QOS = 1, PUBLISH sent, wait for PUBACK
        /// </summary>
        WaitForPuback,

        /// <summary>
        /// QOS = 2, PUBLISH sent, wait for PUBREC
        /// </summary>
        WaitForPubrec,

        /// <summary>
        /// QOS = 2, PUBREC sent, wait for PUBREL
        /// </summary>
        WaitForPubrel,

        /// <summary>
        /// QOS = 2, PUBREL sent, wait for PUBCOMP
        /// </summary>
        WaitForPubcomp,

        /// <summary>
        /// QOS = 2, start first phase handshake send PUBREC
        /// </summary>
        SendPubrec,

        /// <summary>
        /// QOS = 2, start second phase handshake send PUBREL
        /// </summary>
        SendPubrel,

        /// <summary>
        /// QOS = 2, end second phase handshake send PUBCOMP
        /// </summary>
        SendPubcomp,

        /// <summary>
        /// QOS = 1, PUBLISH received, send PUBACK
        /// </summary>
        SendPuback,

        // [v3.1.1] SUBSCRIBE isn't "officially" QOS = 1
        /// <summary>
        /// Send SUBSCRIBE message
        /// </summary>
        SendSubscribe,

        // [v3.1.1] UNSUBSCRIBE isn't "officially" QOS = 1
        /// <summary>
        /// Send UNSUBSCRIBE message
        /// </summary>
        SendUnsubscribe,

        /// <summary>
        /// (QOS = 1), SUBSCRIBE sent, wait for SUBACK
        /// </summary>
        WaitForSuback,

        /// <summary>
        /// (QOS = 1), UNSUBSCRIBE sent, wait for UNSUBACK
        /// </summary>
        WaitForUnsuback
    }
}