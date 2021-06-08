namespace signaling
{
    using System.Linq;
    class LoadBalancer
    {
        internal static KurentoMediaServer NextAvailable()
        {
            var next = (from kms in Cache.MirrorMediaServers
                       where !kms.MaintenanceMode
                       orderby kms.Available descending
                       select kms).First();

            return next;
        }
    }
}