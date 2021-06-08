using System;
using System.Threading;
using System.Threading.Tasks;
using Kurento.NET;

namespace signaling
{
    class KurentoMediaServer
    {
        private Lazy<KurentoClient> kurentoClient;
        public string Ip { get; set; }
        public int Port { get; set; }

        public KurentoClient KurentoClient => this.kurentoClient.Value;

        public long Available { get; internal set; }

        public bool MaintenanceMode { get; internal set; }

        public KurentoMediaServer(string ip, int port)
        {
            // TODO instead of lazy might be better make it async with retry so it is ready for first time usage.
            this.kurentoClient = new Lazy<KurentoClient>(() => new KurentoClient($"ws://{this.Ip}:{this.Port}/kurento"));
        }

        public override bool Equals(object obj)
        {
            var kms = (KurentoMediaServer)obj;
            return this.Ip == kms.Ip && this.Port == kms.Port;
        }

        public override string ToString()
        {
            return $"{this.Ip}:{this.Port}";
        }
    }
}