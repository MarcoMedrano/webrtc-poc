namespace signaling
{
    using System.Collections.Generic;

    static class Cache
    {
        static Cache(){
            Cache.MirrorMediaServers = new List<KurentoMediaServer>();
        }

        public static List<KurentoMediaServer> MirrorMediaServers { get; set; }
    }
}