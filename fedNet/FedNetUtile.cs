using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using MQTTnet;
using MQTTnet.Protocol;

namespace fedNet
{
    // ---------------------- interface ----------------------

    public interface IFedNetLogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        void Clear();
    }
    public interface IAuthenticator
    {
        bool CantConnect();
        bool NeedAthentification();

        void WhoWillBeCheck(string theUsername);
        int MaxConnection(string theUsername);
        bool DontExist(string theUsername);
        bool IsBanned(string theUsername);
        bool Check(string theUsername, string thePassword);
    }

    // ---------------------- class ----------------------

    public class DefaultLogger : IFedNetLogger
    {
        public DefaultLogger() { }
        public void Clear() { }
        public void Error(string message) { }
        public void Info(string message) { }
        public void Warn(string message) { }
    }

    public class Message
    {
        public static Message getMessage(MqttApplicationMessage mess)
        {
            List<string> lstTop = FedNetWorker.getListByTopic(mess.Topic);
            if (lstTop[0] == FedNetConstante.CLIENT_TO_SERVER) {
                string theNewClientID = lstTop[1];
                lstTop.RemoveRange(0, 2);
                return new Message(theNewClientID, lstTop, mess.Payload);
            } // server side
            if (lstTop[0] == FedNetConstante.SERVER_TO_CLIENT) { lstTop.RemoveAt(0); }
            lstTop.RemoveAt(0);
            return new Message(lstTop, mess.Payload); // client side
        }

        public Message(List<string> newListTopic, byte[] newPayload)
        {
            ClientID = "";
            ListTopic = newListTopic;
            Payload = newPayload;
        }
        public Message(string newClientID, List<string> newListTopic, byte[] newPayload)
        {
            ClientID = newClientID;
            ListTopic = newListTopic;
            Payload = newPayload;
        }

        public string ClientID { get; private set; } // server only

        public List<string> ListTopic { get; private set; }
        
        public byte[] Payload { get; private set; }
        public string getStringPayload() { return Encoding.ASCII.GetString(Payload); }
    }

    public class FedNetWorker
    {
        public static List<string> getListByTopic(string topic, string separator = FedNetConstante.DEFAULT_TOPIC_SEPARATOR)
        {
            List<string> toRet = new List<string>();
            int indSlash = topic.IndexOf(separator);
            while(indSlash != -1) {
                toRet.Add(topic.Substring(0, indSlash));
                topic = topic.Substring(indSlash + separator.Length);
                indSlash = topic.IndexOf(separator);
            }
            toRet.Add(topic);
            return toRet;
        }

        public static string getTopicByList(List<string> lisTopic, string separator = FedNetConstante.DEFAULT_TOPIC_SEPARATOR)
        {
            if(lisTopic.Count < 1) { return ""; }
            string toRet = lisTopic[0];
            for(int ind = 1; ind < lisTopic.Count; ind++) {
                toRet = toRet + separator + lisTopic[ind];
            }
            return toRet;
        }
        
        public static string getRandomString(int length, string dicoString = FedNetConstante.DEFAULT_DICO_STRING)
        {
            if(length < 1) { return ""; }
            string toRet = "";
            Random theRand = new Random();
            while (toRet.Length < length) { toRet = toRet + dicoString[theRand.Next(0, dicoString.Length)]; }
            return toRet;
        }
    }

    // ---------------------- struct ----------------------

    public struct ConnectorData
    {
        public ConnectorData(string newHost, int newPort, string newUsername, bool newUseCreditential = false, string newPassword = "")
        {
            Host = newHost;
            Port = newPort;
            useCreditential = newUseCreditential;
            Username = newUsername;
            Password = newPassword;
        }

        public string Host;
        public int Port;
        public bool useCreditential;
        public string Username;
        public string Password;

        public static ConnectorData getDefault(string newUsername, string newPassword, string newHost = FedNetConstante.DEFAULT_HOST)
        {
            return new ConnectorData(newHost, FedNetConstante.DEFAULT_MQTT_PORT, newUsername, true, newPassword);
        }
        public static ConnectorData getDefaultNoCred(string newUsername, string newHost = FedNetConstante.DEFAULT_HOST)
        {
            return new ConnectorData(newHost, FedNetConstante.DEFAULT_MQTT_PORT, newUsername, false, "");
        }

        public shortEndpoint getHasSE()
        {
            return new shortEndpoint(Host, Port);
        }
    }
    public struct shortEndpoint
    {
        public shortEndpoint(string newHost, int newPort)
        {
            Host = newHost;
            Port = newPort;
        }

        public string Host;
        public int Port;
    }
    public struct ServerConfigData
    {
        public ServerConfigData(IPAddress newListenAdress, int newPort, IAuthenticator newAuthenticator)
        {
            listenAdress = newListenAdress;
            Port = newPort;
            Authenticator = newAuthenticator;
        }

        public IPAddress listenAdress;
        public int Port;
        public IAuthenticator Authenticator;

        public static ServerConfigData getDefault(IAuthenticator newAuthenticator)
        {
            return new ServerConfigData(IPAddress.Any, FedNetConstante.DEFAULT_MQTT_PORT, newAuthenticator);
        }
    }

    public struct ClientData
    {
        public ClientData(string newClientID, string newUsername)
        {
            ClientID = newClientID;
            Username = newUsername;
        }

        public string ClientID;
        public string Username;

        public static int IndexOfClientID(List<ClientData> theList, string theClientID)
        {
            int ret = -1;
            for(int i = 0; i < theList.Count; i++)
            {
                if(theList[i].ClientID == theClientID) { ret = i; }
            }
            return ret;
        }
        public static List<int> IndexOfUsername(List<ClientData> theList, string theUsername)
        {
            List<int> ret = new List<int>();
            for (int i = 0; i < theList.Count; i++)
            {
                if (theList[i].Username == theUsername) { ret.Add(i); }
            }
            return ret;
        }
        public static List<string> getListUsername(List<ClientData> theList, bool withIndexCount = false, string indexSeparatorChar = "-")
        {
            List <string> toRet = new List<string>();
            Dictionary<string, int> IndexCount = new Dictionary<string, int>();
            for (int inder = 0; inder < theList.Count; inder++)
            {
                if (theList[inder].Username != null) {
                    if (withIndexCount) {
                        int curInd = 0;
                        if (IndexCount.TryGetValue(theList[inder].Username, out curInd)) {
                            toRet.Add(theList[inder].Username + indexSeparatorChar + curInd);
                            IndexCount[theList[inder].Username] = (curInd + 1);
                        } else {
                            toRet.Add(theList[inder].Username + indexSeparatorChar + 0);
                            IndexCount.Add(theList[inder].Username, 1);
                        }
                    } else {
                        toRet.Add(theList[inder].Username);
                    }
                }
                if (theList[inder].Username == null) {
                    toRet.Add("unknow-client(ERRORatIndex:" + inder + ")");
                }
            }
            return toRet;
        }
    }

    // ---------------------- enum ----------------------

    public enum MessagePriority
    {
        LowAndFast = MqttQualityOfServiceLevel.AtMostOnce,
        Medium = MqttQualityOfServiceLevel.AtLeastOnce,
        HighAndSlow = MqttQualityOfServiceLevel.ExactlyOnce
    }
}
