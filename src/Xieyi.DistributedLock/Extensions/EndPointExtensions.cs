using System.Net;

namespace Xieyi.DistributedLock.Extensions
{
    internal static class EndPointExtensions
    {
        internal static string GetFriendlyName(this EndPoint endPoint)
        {
            return endPoint switch
            {
                DnsEndPoint dnsEndPoint => $"{dnsEndPoint.Host}:{dnsEndPoint.Port}",
                IPEndPoint ipEndPoint => $"{ipEndPoint.Address}:{ipEndPoint.Port}",
                _ => endPoint.ToString()
            };
        }
    }
}