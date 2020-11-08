using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using sahajquinci.MQTT_Client.Messages;
using sahajquinci.MQTT_Client.Utility;
using sahajquinci.MQTT_Client.Exceptions;
using Crestron.SimplSharp.CrestronLogger;
using sahajquinci.MQTT_Client.Managers;
using sahajquinci.MQTT_Client.Events;
using Crestron.SimplSharp.Cryptography.X509Certificates;
using Crestron.SimplSharp.CrestronIO;

namespace sahajquinci.MQTT_Client
{
    public class MqttClient
    {
        private const int FIXED_HEADER_OFFSET = 2;
        private SecureTCPClient tcpClient;
        private Random rand = new Random();
        private List<ushort> packetIdentifiers = new List<ushort>();
        // private CTimer keepAliveTimer;
        // private CTimer timeout;
        private MqttPublisherManager publisherManager;
        private MqttSessionManager sessionManager;
        public PayloadMapper PayloadMapper { get; private set; }
        public PacketDecoder PacketDecoder { get; private set; }

        private delegate void RouteControlPacketDelegate(MqttMsgBase packet);
        
        #region client properties

        public uint PublishQoSLevel { get; private set; }

        public ushort KeepAlivePeriod { get; private set; }

        public Dictionary<string, byte> Topics { get; set; }

        public string ClientId { get; private set; }

        /// <summary>
        /// Clean session flag
        /// </summary>
        public bool CleanSession { get; private set; }

        /// <summary>
        /// Will flag
        /// </summary>
        public bool WillFlag { get; internal set; }

        /// <summary>
        /// Will QOS level
        /// </summary>
        public byte WillQosLevel { get; internal set; }

        /// <summary>
        /// Will topic
        /// </summary>
        public string WillTopic { get; internal set; }

        /// <summary>
        /// Will message
        /// </summary>
        public string WillMessage { get; internal set; }

        /// <summary>
        /// Will retain
        /// </summary>
        public bool WillRetain { get; internal set; }


        /// <summary>
        /// MQTT protocol version
        /// </summary>
        public static byte ProtocolVersion { get { return MqttSettings.PROTOCOL_VERSION; } }

        public bool Retain { get; private set; }

        #endregion

        public event EventHandler<MessageReceivedEventArgs> MessageArrived;
        public event EventHandler<ErrorOccuredEventArgs> ErrorOccured;
        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        #region initialize

        public MqttClient()
        {
            CrestronLogger.Mode = LoggerModeEnum.DEFAULT;
            CrestronLogger.PrintTheLog(false);
            CrestronLogger.Initialize(10);
            CrestronLogger.LogOnlyCurrentDebugLevel = false;
        }

        public void Initialize(string username, string password, ushort port, string ipAddressOfTheServer, ushort bufferSize, string clientId,
            ushort willFlag, ushort willReatin, uint willQoS, string willTopic, string willMessage, ushort keepAlivePeriod, ClientType clientType, uint publishQoSLevel,
            uint retain, uint cleanSession, string certificateFileName, string privateKeyFileName)
        {
            {
                MqttSettings.Instance.Username = username;
                MqttSettings.Instance.Password = password;
                MqttSettings.Instance.BufferSize = Convert.ToInt32(bufferSize);
                MqttSettings.Instance.Port = Convert.ToInt32(port);
                MqttSettings.Instance.IPAddressOfTheServer = IPAddress.Parse(ipAddressOfTheServer);
            }
            CrestronLogger.WriteToLog("Settings initialized", 1);
            {
                KeepAlivePeriod = keepAlivePeriod;
                ClientId = clientId;
                WillFlag = willFlag == 0 ? false : true;
                WillRetain = willReatin == 0 ? false : true;
                WillQosLevel = (byte)willQoS;
                WillTopic = willTopic;
                WillMessage = willMessage;
                Topics = new Dictionary<string, byte>();
                PublishQoSLevel = publishQoSLevel;
                Retain = retain == 0 ? false : true;
                CleanSession = cleanSession == 0 ? false : true;
            }
            CrestronLogger.WriteToLog("CLIENT STUFF initialized", 1);
            {
                try
                {
                    tcpClient = new SecureTCPClient(ipAddressOfTheServer.ToString(), port, bufferSize);
                    if (certificateFileName != "//" && privateKeyFileName != "//")
                    {
                        var certificate = ReadFromResource(@"NVRAM\\" + certificateFileName);
                        X509Certificate2 x509Cert = new X509Certificate2(certificate);
                        tcpClient.SetClientCertificate(x509Cert);
                        tcpClient.SetClientPrivateKey(ReadFromResource(@"NVRAM\\" + privateKeyFileName));
                    }
                    tcpClient.SocketStatusChange += this.OnSocketStatusChange;
                    PayloadMapper = new PayloadMapper();
                    PayloadMapper.ClientType = clientType;
                    PacketDecoder = new PacketDecoder();
                    sessionManager = new MqttSessionManager(clientId);
                    publisherManager = new MqttPublisherManager(sessionManager);
                    publisherManager.PacketToSend += this.OnPacketToSend;
                }
                catch (Exception e)
                {
                    OnErrorOccured("ERROR DURING INITIALIZATION: " + e.Message);
                }
            }
            CrestronLogger.WriteToLog("MQTTCLIENT - Initialize - completed : " + clientId, 1);
        }

        private byte[] ReadFromResource(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            stream.Close();
            return bytes;
        }
        public void AddTopic(string topic)
        {
            try
            {
                Topics.Add(topic, (byte)PublishQoSLevel);
            }
            catch (Exception e)
            {
                OnErrorOccured("AddTopic - Error occured : " + e.Message);
            }
        }

        #endregion

        #region FROM_TO_SIMPL_PLUS_MODULE

        public void Start()
        {
            if (tcpClient.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                Stop();
            }
            Connect();
        }

        public void Stop()
        {
            if (tcpClient.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                Send(MsgBuilder.BuildDisconnect());
                tcpClient.DisconnectFromServer();
            }
            //tcpClient.Dispose();
        }


        /// <summary>
        /// Set the value of the CrestronLogger.PrintTheLog to true or false.
        /// </summary>
        /// <param name="val"> val = 0 equal false , val = 1 equals true</param>
        public void Log(ushort val)
        {
            bool printTheLog = val == 0 ? false : true;
            CrestronLogger.PrintTheLog(printTheLog);
            if (!printTheLog)
                CrestronLogger.ShutdownLogger();
            else if (!CrestronLogger.LoggerInitialized)
            {
                CrestronLogger.Initialize(10);
                CrestronLogger.LogOnlyCurrentDebugLevel = false;
            }
        }

        public void SetLogLevel(uint logLevel)
        {
            if (logLevel == 0)
            {
                CrestronLogger.DebugLevel = 10;
                CrestronLogger.LogOnlyCurrentDebugLevel = false;
            }
            else
            {
                logLevel = (logLevel > 10) ? 10 : logLevel;
                if (logLevel < 0)
                {
                    SetLogLevel(0);
                }
                else
                {
                    CrestronLogger.LogOnlyCurrentDebugLevel = true;
                    CrestronLogger.DebugLevel = logLevel;
                }
            }
        }

        public void OnMessageArrived(string topic, string value)
        {
            if (MessageArrived != null)
                MessageArrived(this, new MessageReceivedEventArgs(topic, value));
        }

        public void OnErrorOccured(string errorMessage)
        {
            if (ErrorOccured != null)
                ErrorOccured(this, new ErrorOccuredEventArgs(errorMessage));
        }

        private void OnSocketStatusChange(SecureTCPClient myTCPClient, SocketStatus serverSocketStatus)
        {
            CrestronLogger.WriteToLog("MQTTCLIENT - OnSocketStatusChange - " + PayloadMapper.ClientType + " socket status : " + serverSocketStatus, 1);
            if (serverSocketStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                OnConnectionStateChanged(1);
            }
            else
            {
                OnConnectionStateChanged(0);
                CTimer timer = new CTimer(DisconnectTimerCallback, 5000);
            }
        }

        public void Publish(string topic, string value)
        {
            byte[] payload = Encoding.ASCII.GetBytes(value);
            MqttMsgPublish msg = MsgBuilder.BuildPublish(topic, false, (byte)this.PublishQoSLevel, true, payload, GetNewPacketIdentifier());
            publisherManager.Publish(msg);
            if (this.PublishQoSLevel == 0x00)
                FreePacketIdentifier(msg.MessageId);
        }

        #endregion

        #region CONNECTION_TO_BROKER


        private void OnConnectionStateChanged(ushort connectionStatus)
        {
            if (ConnectionStateChanged != null)
                ConnectionStateChanged(this, new ConnectionStateChangedEventArgs(connectionStatus));
        }

        /// <summary>
        /// Establishes a connection with the Broker
        /// </summary>
        public void Connect()
        {
            CrestronLogger.WriteToLog("MQTTCLIENT - Connect , attempting connection to " + MqttSettings.Instance.IPAddressOfTheServer.ToString(), 1);
            tcpClient.ConnectToServerAsync(ConnectToServerCallback);
        }

        private void ConnectToServerCallback(SecureTCPClient myTCPClient)
        {
            try
            {
                if (myTCPClient.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
                {
                    MqttMsgConnect connect = MsgBuilder.BuildConnect(this.ClientId, MqttSettings.Instance.Username, MqttSettings.Instance.Password, this.WillRetain,
                         this.WillQosLevel, this.WillFlag, this.WillTopic, this.WillMessage, this.CleanSession, this.KeepAlivePeriod, ProtocolVersion);
                    Send(connect);
                    //TODO: timer for connack
                    tcpClient.ReceiveData();
                    MqttMsgBase packet = PacketDecoder.DecodeControlPacket(tcpClient.IncomingDataBuffer);
                    if (packet.Type == MqttMsgBase.MQTT_MSG_CONNACK_TYPE)
                    {
                        RouteControlPacketToMethodHandler(packet);
                    }
                    else
                    {
                        throw new MqttConnectionException("MQTTCLIENT - ConnectToServerCallback - " + PayloadMapper.ClientType + " , Expected CONNACK , received " + packet, new ArgumentException());
                    }
                }
            }
            catch (MqttClientException e)
            {
                CrestronLogger.WriteToLog("MQTTCLIENT - ConnectToServerCallback - Error occured : " + e.ErrorCode, 7);
                CrestronLogger.WriteToLog("MQTTCLIENT - ConnectToServerCallback - Error occured : " + e.StackTrace, 7);
            }
            catch (Exception e)
            {
                CrestronLogger.WriteToLog("MQTTCLIENT - ConnectToServerCallback - Error occured : " + e.Message, 7);
                CrestronLogger.WriteToLog("MQTTCLIENT - ConnectToServerCallback - Error occured : " + e.StackTrace, 7);
                //Disconnect from server , signal error at module lvl;
            }

        }

        private void HandleCONNACKType(MqttMsgConnack mqttMsgConnack)
        {
            SubscribeToTopics();
            //StartKeepAlive();
            tcpClient.ReceiveDataAsync(ReceiveCallback);
        }



        #endregion

        #region SEND_CONTROL_PACKETS

        public void OnPacketToSend(object sender, PacketToSendEventArgs args)
        {
            Send(args.Packet);
        }

        public void Send(MqttMsgBase packet)
        {
            CrestronLogger.WriteToLog("MQTTCLIENT - SEND - Sending packet :" + packet, 2);
            byte[] pBufferToSend = packet.GetBytes(ProtocolVersion);
            tcpClient.SendDataAsync(pBufferToSend, pBufferToSend.Length, SendCallback);
        }

        private void SendCallback(SecureTCPClient myTCPClient, int numberOfBytesSent)
        {
            ;
        }

        #endregion

        #region RECEIVE_CONTROL_PACKETS       

        private void ReceiveCallback(SecureTCPClient myClient, int numberOfBytesReceived)
        {
            try
            {
                if (numberOfBytesReceived != 0)
                {
                    byte[] incomingDataBuffer = new byte[numberOfBytesReceived];
                    Array.Copy(myClient.IncomingDataBuffer, 0, incomingDataBuffer, 0, numberOfBytesReceived);
                    tcpClient.ReceiveDataAsync(ReceiveCallback);
                    DecodeMultiplePacketsByteArray(incomingDataBuffer);
                }
            }
            catch (Exception e)
            {
                CrestronLogger.WriteToLog("MQTTCLIENT - ReceiveCallback - Error occured : " + e.InnerException + " " + e.Message, 7);
                CrestronLogger.WriteToLog("MQTTCLIENT - ReceiveCallback - Error occured : " + e.StackTrace, 7);
                OnErrorOccured(e.Message);
                Disconnect(false);
            }

        }

        public void DecodeMultiplePacketsByteArray(byte[] data)
        {
            List<MqttMsgBase> packetsInTheByteArray = new List<MqttMsgBase>();
            int numberOfBytesProcessed = 0;
            int numberOfBytesToProcess = 0;
            int numberOfBytesReceived = data.Length;
            byte[] packetByteArray;
            MqttMsgBase tmpPacket = new MqttMsgSubscribe();
            while (numberOfBytesProcessed != numberOfBytesReceived)
            {
                int remainingLength = MqttMsgBase.decodeRemainingLength(data);
                int remainingLenghtIndex = tmpPacket.encodeRemainingLength(remainingLength, data, 1);
                numberOfBytesToProcess = remainingLength + remainingLenghtIndex;
                packetByteArray = new byte[numberOfBytesToProcess];
                Array.Copy(data, 0, packetByteArray, 0, numberOfBytesToProcess);
                {
                    byte[] tmp = new byte[data.Length - numberOfBytesToProcess];
                    Array.Copy(data, numberOfBytesToProcess, tmp, 0, tmp.Length);
                    data = tmp;
                }
                numberOfBytesProcessed += numberOfBytesToProcess;
                MqttMsgBase packet = PacketDecoder.DecodeControlPacket(packetByteArray);
                //RouteControlPacketDelegate r = new RouteControlPacketDelegate(RouteControlPacketToMethodHandler);
                //r.Invoke(packet);
                CrestronInvoke.BeginInvoke(RouteControlPacketToMethodHandler,packet);
            }            
        }

        
        private void RouteControlPacketToMethodHandler(object p)
        {
            MqttMsgBase packet = (MqttMsgBase)p;
            switch (packet.Type)
            {
                case MqttMsgBase.MQTT_MSG_CONNACK_TYPE:
                    {
                        HandleCONNACKType((MqttMsgConnack)packet);
                        break;
                    }
                case MqttMsgBase.MQTT_MSG_PUBLISH_TYPE:
                    {
                        HandlePUBLISHType((MqttMsgPublish)packet);
                        break;
                    }
                case MqttMsgBase.MQTT_MSG_PUBACK_TYPE:
                    {
                        HandlePUBACKType((MqttMsgPuback)packet);
                        break;
                    }
                case MqttMsgBase.MQTT_MSG_PUBREC_TYPE:
                    {
                        HandlePUBRECType((MqttMsgPubrec)packet);
                        break;
                    }
                case MqttMsgBase.MQTT_MSG_PUBREL_TYPE:
                    {
                        HandlePUBRELType((MqttMsgPubrel)packet);
                        break;
                    }
                case MqttMsgBase.MQTT_MSG_PUBCOMP_TYPE:
                    {
                        HandlePUBCOMPType((MqttMsgPubcomp)packet);
                        break;
                    }
                case MqttMsgBase.MQTT_MSG_SUBACK_TYPE:
                    {
                        HandleSUBACKype((MqttMsgSuback)packet);
                        break;
                    }
                case MqttMsgBase.MQTT_MSG_UNSUBACK_TYPE:
                    {
                        HandleUNSUBACKype((MqttMsgUnsuback)packet);
                        break;
                    }
                case MqttMsgBase.MQTT_MSG_PINGRESP_TYPE:
                    {
                        HandlePINGRESPType((MqttMsgPingResp)packet);
                        break;
                    }
                default:
                    {
                        throw new MqttCommunicationException(new FormatException("MQTTCLIENT -Pacchetto non valido" + packet));
                    }
            }
        }


        #endregion

        #region PING

        private void HandlePINGREQType(MqttMsgPingReq mqttMsgPingReq)
        {
            Disconnect(false);
        }

        /* private void StartKeepAlive()
         {
             Send(MsgBuilder.BuildPingReq());
             keepAliveTimer = new CTimer(KeepAliveTimerCallback, false, long.Parse(KeepAlivePeriod.ToString()));
         }*/


        /*private void KeepAliveTimerCallback(object userSpecific)
        {
            bool hasPingResponseBeenReceived = (bool)userSpecific;
            if (hasPingResponseBeenReceived)
            {

            }
            else
            {
                OnErrorOccured("The server didn't respond on time ,disconnecting");
                Disconnect(false);
            }
        }

        private void TimoutTimerCallBack(object userSpecific)
        {

        }*/

        private void HandlePINGRESPType(MqttMsgPingResp mqttMsgPingResp)
        {
            /* keepAliveTimer.Stop();
             keepAliveTimer.Dispose();
             keepAliveTimer = new CTimer(KeepAliveTimerCallback, true, long.Parse(KeepAlivePeriod.ToString()));*/
        }

        #endregion

        #region PUBLISH

        private void HandlePUBCOMPType(MqttMsgPubcomp pubComp)
        {
            throw new NotImplementedException();
            //publisherManager.ManagePubComp(pubComp);
        }

        private void HandlePUBRELType(MqttMsgPubrel pubRel)
        {
            throw new NotImplementedException();
            //MqttMsgPublish publish = sessionManager.GetPublishMessage(pubRel.MessageId);
            //string publishPayload = System.Text.Encoding.ASCII.GetString(publish.Message, 0, publish.Message.Length);
            //OnMessageArrived(publish.Topic, PayloadMapper.Map(publishPayload));
        }

        private void HandlePUBRECType(MqttMsgPubrec pubRec)
        {
            throw new NotImplementedException();
            //publisherManager.ManagePubRec(pubRec);
        }

        private void HandlePUBACKType(MqttMsgPuback pubAck)
        {
            publisherManager.ManagePubAck(pubAck);
        }

        private void HandlePUBLISHType(MqttMsgPublish publish)
        {
            try
            {
                switch (publish.QosLevel)
                {
                    case MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE:
                        {
                            CrestronLogger.WriteToLog("MQTTCLIENT - HandlePUBLISHType - Routing qos0 message", 5);
                            string publishPayload = System.Text.Encoding.ASCII.GetString(publish.Message, 0, publish.Message.Length);
                            OnMessageArrived(publish.Topic, PayloadMapper.Map(publishPayload));
                            break;
                        }
                    case MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE:
                        {
                            CrestronLogger.WriteToLog("MQTTCLIENT - HandlePUBLISHType - Routing qos1 message", 5);
                            string publishPayload = System.Text.Encoding.ASCII.GetString(publish.Message, 0, publish.Message.Length);
                            Send(MsgBuilder.BuildPubAck(publish.MessageId));
                            OnMessageArrived(publish.Topic, PayloadMapper.Map(publishPayload));
                            break;
                        }
                    case MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE:
                        {
                            CrestronLogger.WriteToLog("MQTTCLIENT - HandlePUBLISHType - Routing qos2 message", 5);
                            //ManageQoS2(publish);
                            break;
                        }
                    default:
                        break;
                }
                //TODO: Raise MessageArrived event , handle the necessary responses with the publisher manager.
            }
            catch (ArgumentException e)
            {
                OnErrorOccured(e.Message);
            }

        }


        #endregion

        #region SUBSCRIBE

        private void HandleUNSUBACKype(MqttMsgUnsuback mqttMsgUnsuback)
        {
            throw new NotImplementedException();
        }

        private void HandleSUBACKype(MqttMsgSuback mqttMsgSuback)
        {
            CrestronLogger.WriteToLog("MQTTCLIENT - HANDLESUBACK -", 6);
        }

        private void SubscribeToTopics()
        {
            Send(MsgBuilder.BuildSubscribe(Topics.Keys.ToArray(), Topics.Values.ToArray(), GetNewPacketIdentifier()));
        }


        #endregion

        #region DISCONNECT

        private void Disconnect(bool withDisconnectPacket)
        {
            CrestronLogger.WriteToLog("MQTTCLIENT - DISCONNECT - Restarting client", 8);
            Stop();
        }

        public void DisconnectTimerCallback(object userSpecific)
        {
            if (tcpClient.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                CTimer timer = new CTimer(DisconnectTimerCallback, 5000);
                Connect();
            }
        }
        #endregion


        internal ushort GetNewPacketIdentifier()
        {
            lock (packetIdentifiers)
            {
                ushort identifier = (ushort)rand.Next(0, 65535);
                while (packetIdentifiers.Contains(identifier))
                {
                    identifier = identifier = (ushort)rand.Next(0, 65535);
                }
                packetIdentifiers.Add(identifier);
                return identifier;
            }
        }

        internal void FreePacketIdentifier(ushort identifier)
        {
            if (packetIdentifiers.Contains(identifier))
                packetIdentifiers.Remove(identifier);
        }

    }
}