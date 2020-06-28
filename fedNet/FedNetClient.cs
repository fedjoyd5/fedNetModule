using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;

namespace fedNet
{
    public class FedNetClient
    {
        private IFedNetLogger _logSystem = new DefaultLogger();
        public void ChangeLogger(IFedNetLogger newlogSystem) { _logSystem = newlogSystem; }

        private IMqttClient _theGameClient;

        private MqttClientOptionsBuilder _ClientConfiguration;
        private shortEndpoint _MQTTHostAndPort;
        private bool reconnectOnDisco;

        public FedNetClient(ConnectorData MQTTConnectorData, IFedNetLogger newlogSystem = null)
        {
            _logSystem = newlogSystem;
            if (_logSystem == null) { _logSystem = new DefaultLogger(); }

            _MQTTHostAndPort = MQTTConnectorData.getHasSE();

            _ClientConfiguration = new MqttClientOptionsBuilder();
            _ClientConfiguration.WithClientId(String.Format("{0}-{1}_{2}", MQTTConnectorData.Username, DateTime.Now.ToString("ffffmmHHss"), FedNetWorker.getRandomString(10)));
            if (MQTTConnectorData.useCreditential) { _ClientConfiguration.WithCredentials(MQTTConnectorData.Username, MQTTConnectorData.Password); }
            _ClientConfiguration.WithTcpServer(MQTTConnectorData.Host, MQTTConnectorData.Port);

            _theGameClient = new MqttFactory().CreateMqttClient();
            _theGameClient.UseDisconnectedHandler(e => {
                _logSystem.Info("Disconnected (reason : " + (e.AuthenticateResult != null ? e.AuthenticateResult.ResultCode.ToString() : "unknow") + ")");
                if (reconnectOnDisco) {
                    Task.Delay(1000).Wait();
                    Connect();
                }
            });
            _theGameClient.UseConnectedHandler(e => {
                _logSystem.Info("Connected !!");
                _theGameClient.SubscribeAsync(FedNetConstante.SERVER_TO_CLIENT + FedNetConstante.DEFAULT_TOPIC_SEPARATOR + ClientId + FedNetConstante.DEFAULT_TOPIC_SEPARATOR + FedNetConstante.DEFAULT_TOPIC_JOKER, (MqttQualityOfServiceLevel)FedNetConstante.PRIORITY_SERVER_TO_CLIENT);
                _theGameClient.SubscribeAsync(FedNetConstante.SERVER_BROADCAST + FedNetConstante.DEFAULT_TOPIC_SEPARATOR + FedNetConstante.DEFAULT_TOPIC_JOKER, (MqttQualityOfServiceLevel)FedNetConstante.PRIORITY_SERVER_BROADCAST);
            });
            _theGameClient.UseApplicationMessageReceivedHandler(e => {
                if (MessageReceived != null) { MessageReceived.Invoke(this, Message.getMessage(e.ApplicationMessage)); }
            });

            reconnectOnDisco = true;

            _logSystem.Info("Client initialized !");
        }

        ~FedNetClient() { _theGameClient.Dispose(); }

        public void changeClientConfig(ConnectorData MQTTConnectorData)
        {
            _MQTTHostAndPort = MQTTConnectorData.getHasSE();
            _ClientConfiguration = new MqttClientOptionsBuilder();
            _ClientConfiguration.WithClientId(String.Format("{0}-{1}_{2}", MQTTConnectorData.Username, DateTime.Now.ToString("ffffmmHHss"), FedNetWorker.getRandomString(10)));
            if (MQTTConnectorData.useCreditential) { _ClientConfiguration.WithCredentials(MQTTConnectorData.Username, MQTTConnectorData.Password); }
            if (!MQTTConnectorData.useCreditential) { _ClientConfiguration.WithCredentials(MQTTConnectorData.Username, ""); }
            _ClientConfiguration.WithTcpServer(MQTTConnectorData.Host, MQTTConnectorData.Port);

            _logSystem.Info("MQTT Client reinitialized !");
        }

        public void Connect()
        {
            IMqttClientOptions theOpt = _ClientConfiguration.Build();
            _theGameClient.ConnectAsync(theOpt);
            _logSystem.Info(String.Format("Start connection to '{0}:{1}' ...", _MQTTHostAndPort.Host, _MQTTHostAndPort.Port.ToString()));
            _logSystem.Info((theOpt.Credentials != null ? "Username : '" + theOpt.Credentials.Username + "', " : "") + "Client ID : " + theOpt.ClientId);
        }
        public void Reconnect()
        {
            IMqttClientOptions theOpt = _ClientConfiguration.Build();
            _logSystem.Clear();
            asyncReconnect();
            _logSystem.Info(String.Format("Start connection to '{0}:{1}' ...", _MQTTHostAndPort.Host, _MQTTHostAndPort.Port.ToString()));
            _logSystem.Info((theOpt.Credentials != null ? "Username : '" + theOpt.Credentials.Username + "', " : "") + "Client ID : " + theOpt.ClientId);
        }
        private async void asyncReconnect()
        {
            await _theGameClient.DisconnectAsync();
            await _theGameClient.ConnectAsync(_ClientConfiguration.Build());
        }
        public void Disconnect()
        {
            reconnectOnDisco = false;
            _logSystem.Info("Stop connection ...");
            _theGameClient.DisconnectAsync();
        }

        public void dontReconnect() { reconnectOnDisco = false; }
        public void doReconnect() { reconnectOnDisco = true; }

        public string ClientId { get { if(_theGameClient.IsConnected) { return _theGameClient.Options.ClientId; } return ""; } }
        public bool IsConnected { get { return _theGameClient.IsConnected; } }
        public bool tryReconnect { get { return reconnectOnDisco; } }

        public event EventHandler<Message> MessageReceived;

        public bool sendMessage(List<string> lisTopic, string Data, MessagePriority priority = FedNetConstante.DEFAULT_PRIORITY) { return sendMessage(lisTopic, Encoding.ASCII.GetBytes(Data), priority); }
        public bool sendMessage(List<string> lisTopic, byte[] Data, MessagePriority priority = FedNetConstante.DEFAULT_PRIORITY) { return sendMessage(FedNetWorker.getTopicByList(lisTopic), Data, priority); }
        public bool sendMessage(string theTopic, string Data, MessagePriority priority = FedNetConstante.DEFAULT_PRIORITY) { return sendMessage(theTopic, Encoding.ASCII.GetBytes(Data), priority); }
        public bool sendMessage(string theTopic, byte[] Data, MessagePriority priority = FedNetConstante.DEFAULT_PRIORITY) {
            if (!_theGameClient.IsConnected) { return false; }
            if (theTopic == "") { return false; }
            MqttApplicationMessageBuilder theMsgBuilder = new MqttApplicationMessageBuilder();
            theMsgBuilder.WithTopic(FedNetConstante.CLIENT_TO_SERVER + FedNetConstante.DEFAULT_TOPIC_SEPARATOR + ClientId + FedNetConstante.DEFAULT_TOPIC_SEPARATOR + theTopic);
            theMsgBuilder.WithQualityOfServiceLevel((MqttQualityOfServiceLevel)priority);
            theMsgBuilder.WithPayload(Data);
            _theGameClient.PublishAsync(theMsgBuilder.Build());
            return true;
        }
    }
}
