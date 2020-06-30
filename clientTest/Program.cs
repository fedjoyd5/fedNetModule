using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fedNet;

namespace clientTest
{
    class Program
    {
        static void Main(string[] args)
        {
            FedNetClient theClient = new FedNetClient(new ConnectorData("127.0.0.1", 4620, "test", true, "test"), new ConsoleLogger());
            theClient.MessageReceived += TheClient_MessageReceived;
            theClient.Connected += TheClient_Connected;
            theClient.Disconnected += TheClient_Disconnected;
            theClient.Connect();
            Console.ReadLine();
            theClient.sendMessage(new List<string>() { "request", "list", "client" }, "ALL");
            Console.ReadLine();
            theClient.Disconnect();
            Console.ReadLine();
        }

        private static void TheClient_Disconnected(object sender, EventArgs e)
        {
            Console.WriteLine("disconnected !!");
        }

        private static void TheClient_Connected(object sender, EventArgs e)
        {
            Console.WriteLine("connected !!");
        }

        private static void TheClient_MessageReceived(object sender, Message e)
        {
            Console.WriteLine("topic '" + (e.ListTopic.Count > 0 ? FedNetWorker.getTopicByList(e.ListTopic) : "none") + "' said : " + e.getStringPayload());
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

    /*public class testReceiver : IMessageReceiver
    {
        public void ReceiveMessage(Message theMSG)
        {
            if (theMSG.ListTopic.Count > 0) { Console.Write("topic '" + theMSG.ListTopic[0] + "' "); }
            Console.WriteLine("said " + theMSG.getStringPayload());
        }
    }*/
}
