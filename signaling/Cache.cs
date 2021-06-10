namespace signaling
{
    using System.Collections.Generic;

    static class Cache
    {
        static Cache(){
            Cache.MediaServers = new List<KurentoMediaServer>();
        }

        public static List<KurentoMediaServer> MediaServers { get; set; }
    }
}