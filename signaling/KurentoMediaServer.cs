using System;
using System.Threading;
using System.Threading.Tasks;
using Kurento.NET;

namespace signaling
{
    class KurentoMediaServer
    {
        private Lazy<KurentoClient> kurentoClient;
        private string networkIpAddress;

        public string Ip { get; set; }
        public int Port { get; set; }

        public KurentoClient KurentoClient => this.kurentoClient.Value;

        public long Available { get; internal set; }

        public bool MaintenanceMode { get; internal set; }
        public string NetworkIpAddress
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

        public KurentoMediaServer(string ip, int port, string role = "mirror")
        {
            this.Ip = ip;
            this.Port = port;
            this.Role = role;
        }

        public override bool Equals(object obj)
        {
            var kms = (KurentoMediaServer)obj;
            return this.Ip == kms.Ip && this.Port == kms.Port;
        }

        public override string ToString()
        {
            return $"{this.NetworkIpAddress}-{this.Ip}:{this.Port}";
        }
    }
}