using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fedNet
{
    public static class FedNetConstante
    {
        public const int DEFAULT_MQTT_PORT = 4620;
        public const string DEFAULT_HOST = "irongrad.ddns.net";
        public const string DEFAULT_TOPIC_SEPARATOR = "/";
        public const string DEFAULT_TOPIC_JOKER = "#";
        public const string DEFAULT_DICO_STRING = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const MessagePriority DEFAULT_PRIORITY = MessagePriority.Medium;

        public const int MAX_MESSAGE_PENDING = 10;
        public const string CLIENT_TO_SERVER = "Client-To-Server";
        public const string SERVER_TO_CLIENT = "Server-To-Client";
        public const string SERVER_BROADCAST = "Server-Broadcast";
        public const MessagePriority PRIORITY_CLIENT_TO_SERVER = MessagePriority.Medium;
        public const MessagePriority PRIORITY_SERVER_TO_CLIENT = MessagePriority.Medium;
        public const MessagePriority PRIORITY_SERVER_BROADCAST = MessagePriority.LowAndFast;
    }
}
