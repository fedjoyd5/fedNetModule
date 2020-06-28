using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using MQTTnet;
using MQTTnet.Server;
using MQTTnet.Protocol;

namespace fedNet
{
    public class FedNetServer : IMqttServerApplicationMessageInterceptor, IMqttServerSubscriptionInterceptor, IMqttServerConnectionValidator, IMqttServerStartedHandler, IMqttServerStoppedHandler, IMqttServerClientDisconnectedHandler
    {
        private IFedNetLogger _logSystem = new DefaultLogger();
        public void ChangeLogger(IFedNetLogger newlogSystem) { _logSystem = newlogSystem; }

        private IMqttServer _theGameServer;

        private MqttServerOptionsBuilder _ServerConfiguration;
        private IAuthenticator _theAuthenticator;
        private List<ClientData> _theClientList;

        public FedNetServer(ServerConfigData MQTTConfigData, IFedNetLogger newlogSystem = null)
        {
            _logSystem = newlogSystem;
            if(_logSystem == null) { _logSystem = new DefaultLogger(); }

            _theAuthenticator = MQTTConfigData.Authenticator;
            _ServerConfiguration = new MqttServerOptionsBuilder();
            _ServerConfiguration.WithConnectionValidator(this);
            _ServerConfiguration.WithApplicationMessageInterceptor(this);
            _ServerConfiguration.WithSubscriptionInterceptor(this);
            _ServerConfiguration.WithClientId("Server");
            _ServerConfiguration.WithDefaultEndpointBoundIPAddress(MQTTConfigData.listenAdress);
            _ServerConfiguration.WithDefaultEndpointPort(MQTTConfigData.Port);
            _ServerConfiguration.WithMaxPendingMessagesPerClient(FedNetConstante.MAX_MESSAGE_PENDING);

            _theClientList = new List<ClientData>();
            _theGameServer = new MqttFactory().CreateMqttServer();
            _theGameServer.UseApplicationMessageReceivedHandler(e => {
                if (MessageReceived != null) { MessageReceived.Invoke(this, Message.getMessage(e.ApplicationMessage)); }
            });
            _theGameServer.UseClientConnectedHandler(e => {
                _logSystem.Info("new client connected : " + e.ClientId);
                //_theGameServer.SubscribeAsync("Server", FedNetConstante.CLIENT_TO_SERVER + FedNetConstante.DEFAULT_TOPIC_SEPARATOR + FedNetConstante.DEFAULT_TOPIC_JOKER, (MqttQualityOfServiceLevel)FedNetConstante.PRIORITY_SERVER_TO_CLIENT);
            });
            _theGameServer.StartedHandler = this;
            _theGameServer.StoppedHandler = this;
            _theGameServer.ClientDisconnectedHandler = this;

            _logSystem.Info("initialisation finished !!");
        }
        public void changeServerConfig(ServerConfigData MQTTConfigData)
        {
            _theAuthenticator = MQTTConfigData.Authenticator;
            _ServerConfiguration = new MqttServerOptionsBuilder();
            _ServerConfiguration.WithConnectionValidator(this);
            _ServerConfiguration.WithApplicationMessageInterceptor(this);
            _ServerConfiguration.WithSubscriptionInterceptor(this);
            _ServerConfiguration.WithClientId("Server");
            _ServerConfiguration.WithDefaultEndpointBoundIPAddress(MQTTConfigData.listenAdress);
            _ServerConfiguration.WithDefaultEndpointPort(MQTTConfigData.Port);
            _ServerConfiguration.WithMaxPendingMessagesPerClient(FedNetConstante.MAX_MESSAGE_PENDING);

            _logSystem.Info("reinitialisation finnised !!");
        }

        public void StartServer()
        {
            _theClientList.Clear();
            _logSystem.Info("Starting server ...");
            _theGameServer.StartAsync(_ServerConfiguration.Build());
        }
        public void RestartServer()
        {
            _theClientList.Clear();
            _logSystem.Clear();
            _logSystem.Info("Restarting server ...");
            asyncRestartServer();
        }
        private async void asyncRestartServer()
        {
            await _theGameServer.StopAsync();
            await _theGameServer.StartAsync(_ServerConfiguration.Build());
        }
        public void StopServer()
        {
            _theClientList.Clear();
            _logSystem.Info("Stoping server ...");
            _theGameServer.StopAsync();
        }

        public Task InterceptApplicationMessagePublishAsync(MqttApplicationMessageInterceptorContext context)
        {
            if(!context.ApplicationMessage.Topic.StartsWith(FedNetConstante.CLIENT_TO_SERVER + FedNetConstante.DEFAULT_TOPIC_SEPARATOR + context.ClientId) && context.ClientId != _theGameServer.Options.ClientId) {
                context.AcceptPublish = false;
                /*context.CloseConnection = true;*/
                _logSystem.Warn(context.ClientId + " try to publish on topic : " + context.ApplicationMessage.Topic);
                return Task.Delay(1);
            }
            context.AcceptPublish = true;
            return Task.Delay(1);
        }

        public Task InterceptSubscriptionAsync(MqttSubscriptionInterceptorContext context)
        {
            context.AcceptSubscription = true;
            if (context.ClientId != _theGameServer.Options.ClientId) { context.AcceptSubscription = false; }
            if (context.TopicFilter.Topic.StartsWith(FedNetConstante.SERVER_TO_CLIENT + FedNetConstante.DEFAULT_TOPIC_SEPARATOR + context.ClientId)) { context.AcceptSubscription = true; }
            if (context.TopicFilter.Topic.StartsWith(FedNetConstante.SERVER_BROADCAST)) { context.AcceptSubscription = true; }

            if (!context.AcceptSubscription) { /*context.CloseConnection = true;*/ _logSystem.Warn(context.ClientId + " try to subscribe on topic : " + context.TopicFilter.Topic); }
            if (context.AcceptSubscription) { _logSystem.Info(context.ClientId + " has subscribe on topic : " + context.TopicFilter.Topic); }
            
            return Task.Delay(1);
        }

        public Task ValidateConnectionAsync(MqttConnectionValidatorContext context)
        {
            if (!_theAuthenticator.CanConnect()) { context.ReasonCode = MqttConnectReasonCode.ServerUnavailable; return Task.Delay(1); }
            if (_theAuthenticator.NeedAthentification()) {
                if (context.Username == "" || context.Password == "") { context.ReasonCode = MqttConnectReasonCode.NotAuthorized; return Task.Delay(1); }
                if (ClientData.IndexOfUsername(_theClientList, context.Username) != -1) { context.ReasonCode = MqttConnectReasonCode.QuotaExceeded; return Task.Delay(1); }
                if (_theAuthenticator.IsBanned(context.Username)) { context.ReasonCode = MqttConnectReasonCode.Banned; return Task.Delay(1); }
                if (!_theAuthenticator.Check(context.Username, context.Password)) { context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword; return Task.Delay(1); }
            }
            if (context.ClientId.Length < 5 || ClientData.IndexOfClientID(_theClientList, context.ClientId) != -1) { context.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid; return Task.Delay(1); }

            _theClientList.Add(new ClientData(context.ClientId, context.Username));

            return Task.Delay(1);
        }

        public Task HandleServerStartedAsync(EventArgs eventArgs)
        {
            _logSystem.Info(String.Format("The server started at {0}:{1}", _theGameServer.Options.DefaultEndpointOptions.BoundInterNetworkAddress.ToString(), _theGameServer.Options.DefaultEndpointOptions.Port.ToString()));
            return Task.Delay(1);
        }

        public Task HandleServerStoppedAsync(EventArgs eventArgs)
        {
            _logSystem.Info("The server stopped !!");
            return Task.Delay(1);
        }

        public Task HandleClientDisconnectedAsync(MqttServerClientDisconnectedEventArgs eventArgs)
        {
            _logSystem.Info(eventArgs.ClientId + " has disconnect !");
            int indexCl = ClientData.IndexOfClientID(_theClientList, eventArgs.ClientId);
            if (indexCl != -1) { _theClientList.RemoveAt(indexCl); }
            return Task.Delay(1);
        }

        public bool IsStarted { get { return _theGameServer.IsStarted; } }

        public event EventHandler<Message> MessageReceived;

        // ---------- simple message ----------

        public bool sendMessage(string ClientID, List<string> lisTopic, string Data, MessagePriority priority = FedNetConstante.DEFAULT_PRIORITY) { return sendMessage(ClientID, lisTopic, Encoding.ASCII.GetBytes(Data), priority); }
        public bool sendMessage(string ClientID, List<string> lisTopic, byte[] Data, MessagePriority priority = FedNetConstante.DEFAULT_PRIORITY) { return sendMessage(ClientID, FedNetWorker.getTopicByList(lisTopic), Data, priority); }
        public bool sendMessage(string ClientID, string theTopic, string Data, MessagePriority priority = FedNetConstante.DEFAULT_PRIORITY) { return sendMessage(ClientID, theTopic, Encoding.ASCII.GetBytes(Data), priority); }
        public bool sendMessage(string ClientID, string theTopic, byte[] Data, MessagePriority priority = FedNetConstante.DEFAULT_PRIORITY)
        {
            if (!_theGameServer.IsStarted) { return false; }
            if (ClientID == "") { return false; }
            if (theTopic == "") { return false; }
            MqttApplicationMessageBuilder theMsgBuilder = new MqttApplicationMessageBuilder();
            theMsgBuilder.WithTopic(FedNetConstante.SERVER_TO_CLIENT + FedNetConstante.DEFAULT_TOPIC_SEPARATOR + ClientID + FedNetConstante.DEFAULT_TOPIC_SEPARATOR + theTopic);
            theMsgBuilder.WithQualityOfServiceLevel((MqttQualityOfServiceLevel)priority);
            theMsgBuilder.WithPayload(Data);
            _theGameServer.PublishAsync(theMsgBuilder.Build());
            return true;
        }
        
        // ---------- broadcast message ----------

        public bool sendBroadcastMessage(List<string> lisTopic, string Data, MessagePriority priority = FedNetConstante.DEFAULT_PRIORITY) { return sendBroadcastMessage(lisTopic, Encoding.ASCII.GetBytes(Data), priority); }
        public bool sendBroadcastMessage(List<string> lisTopic, byte[] Data, MessagePriority priority = FedNetConstante.DEFAULT_PRIORITY) { return sendBroadcastMessage(FedNetWorker.getTopicByList(lisTopic), Data, priority); }
        public bool sendBroadcastMessage(string theTopic, string Data, MessagePriority priority = FedNetConstante.DEFAULT_PRIORITY) { return sendBroadcastMessage(theTopic, Encoding.ASCII.GetBytes(Data), priority); }
        public bool sendBroadcastMessage(string theTopic, byte[] Data, MessagePriority priority = FedNetConstante.DEFAULT_PRIORITY)
        {
            if (!_theGameServer.IsStarted) { return false; }
            if (theTopic == "") { return false; }
            MqttApplicationMessageBuilder theMsgBuilder = new MqttApplicationMessageBuilder();
            theMsgBuilder.WithTopic(FedNetConstante.SERVER_BROADCAST + FedNetConstante.DEFAULT_TOPIC_SEPARATOR + theTopic);
            theMsgBuilder.WithQualityOfServiceLevel((MqttQualityOfServiceLevel)priority);
            theMsgBuilder.WithPayload(Data);
            _theGameServer.PublishAsync(theMsgBuilder.Build());
            return true;
        }

        // ---------- broadcast message ----------

        public string getClientIDByUsername(string theUsername)
        {
            int theInd = ClientData.IndexOfUsername(_theClientList, theUsername);
            if(theInd == -1) { return ""; }
            return _theClientList[theInd].ClientID;
        }
        public string getUsernameByClientID(string theClientID)
        {
            int theInd = ClientData.IndexOfClientID(_theClientList, theClientID);
            if (theInd == -1) { return ""; }
            return _theClientList[theInd].Username;
        }
        public List<string> getListClient() { return ClientData.getListUsername(_theClientList); }
    }
}
