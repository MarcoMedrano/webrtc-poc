using System;
using Kurento.NET;

namespace signaling
{
    class KurentoMediaServer
    {
        private Lazy<KurentoClient> kurentoClient;
        private string networkIpAddress;

        public string ConnectionId { get; set; }

        public int Port { get; set; }

        public KurentoClient KurentoClient => this.kurentoClient.Value;

        public long Available { get; internal set; }

        public bool MaintenanceMode { get; internal set; }
        public string Ip
        {
            get => networkIpAddress;
            internal set
            {
                networkIpAddress = value;
                // TODO instead of lazy might be better make it async with retry so it is ready for first time usage.
                this.kurentoClient = new Lazy<KurentoClient>(() => new KurentoClient($"ws://{value}:8888/kurento"));
            }
        }

        public string Role { get; internal set; }
        public string Name { get; }

        public KurentoMediaServer(string connectionId, string ip, int port, string role)
        {
            this.ConnectionId = connectionId;
            this.Ip = ip;
            this.Port = port;
            this.Role = role;
        }

        public KurentoMediaServer(string connectionId, string ip, int port, string role, string name) : this(connectionId, ip, port, role)
        {
            Name = name;
        }

        public override bool Equals(object obj)
        {
            if(obj == null) return false;

            var kms = (KurentoMediaServer)obj;
            return (this.ConnectionId != null && this.ConnectionId == kms.ConnectionId)
            || (this.Ip != null && this.Ip == kms.Ip);
        }

        public override string ToString()
        {
            return $"{this.Ip}:{this.Port} - ({this.Name})";
        }
    }
}