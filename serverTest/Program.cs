using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fedNet;

namespace serverTest
{
    class Program
    {
        public static FedNetServer theServer;
        public static ConsoleLogger theLogger;

        static void Main(string[] args)
        {
            theLogger = new ConsoleLogger();
            theLogger.Info("test");
            theLogger.Warn("test");
            theLogger.Error("test");
            theServer = new FedNetServer(ServerConfigData.getDefault(new Athentificateur()), theLogger);
            theServer.MessageReceived += TheServer_MessageReceived;
            theServer.StartServer();
            Console.ReadLine();
            //theServer.sendBroadcastMessage("test", "test", MessagePriority.LowAndFast);
            //Console.ReadLine();
            theServer.StopServer();
            Console.ReadLine();
        }

        private static void TheServer_MessageReceived(object sender, Message e)
        {
            theLogger.Info(e.ClientID + " send message" + (e.ListTopic.Count > 0 ? " on '" + FedNetWorker.getTopicByList(e.ListTopic) + "' " : " ") + "(" + e.Payload.Length + " bytes)");
            if (e.ListTopic.Count > 2)
            {
                if(e.ListTopic[0] == "request" && e.ListTopic[1] == "list" && e.ListTopic[2] == "client" && e.getStringPayload() == "ALL") {
                    theServer.sendMessage(e.ClientID, "listClient", FedNetWorker.getTopicByList(theServer.getListClient(true), "|"));
                }
            }
        }
    }

    public class ConsoleLogger : IFedNetLogger
    {
        private ConsoleColor _DefaultColor;

        public ConsoleLogger() { _DefaultColor = Console.ForegroundColor; }

        public void Clear() { Console.Clear(); }

        public void Info(string message)
        {
            Console.ForegroundColor = _DefaultColor;
            Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy-HH:mm:ss") + " |INFO     | " + message);
        }
        public void Warn(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy-HH:mm:ss") + " |WARNNING | " + message);
            Console.ForegroundColor = _DefaultColor;
        }
        public void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy-HH:mm:ss") + " |ERROR    | " + message);
            Console.ForegroundColor = _DefaultColor;
        }
    }

    public class Athentificateur : IAuthenticator
    {
        public bool CantConnect() { return false; }
        public bool NeedAthentification() { return false; }

        public void WhoWillBeCheck(string theUsername) { }

        public bool DontExist(string theUsername)
        {
            return false;
        }
        public int MaxConnection(string theUsername)
        {
            return 2;
        }

        public bool Check(string theUsername, string thePassword)
        {
            Console.WriteLine(theUsername + " try succesfully to connect with " + thePassword);
            return true;
        }

        public bool IsBanned(string theUsername)
        {
            return false;
        }
    }
}
