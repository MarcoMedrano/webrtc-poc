namespace signaling
{
    using System.Linq;
    class LoadBalancer
    {
        internal static KurentoMediaServer NextAvailable(string role, KurentoMediaServer except = null)
        {
            var next = (from kms in Cache.MediaServers
                       where !kms.MaintenanceMode
                       where kms.Role == role
                       where kms != except
                       orderby kms.Available descending
                       select kms).First();

            return next;
        }
    }
}