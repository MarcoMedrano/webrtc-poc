namespace signaling
{
    using System.Linq;
    class LoadBalancer
    {
        internal static KurentoMediaServer NextAvailable(string role = "mirror")
        {
            var next = (from kms in Cache.MirrorMediaServers
                       where !kms.MaintenanceMode
                       where kms.Role == role
                       orderby kms.Available descending
                       select kms).First();

            return next;
        }
    }
}