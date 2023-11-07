using System.Net;
using System.Security.Authentication;

namespace Xieyi.DistributedLock.Connection
{
    public class DistributedLockEndPoint
    {
        /// <summary>
        /// The endpoints for the redis connection. Can be used for connecting to replicated master/slaves.
        /// These servers will all be considered a single entity for distributedLock.
        /// </summary>
        public EndPoint EndPoint { get; set; }
        
        /// <summary>
        /// Whether to use SSL for the redis connection.
        /// </summary>
        public bool Ssl { get; set; }

        /// <summary>
        /// The allowed SSL/TLS protocols for the redis connection.
        /// Defaults to a value chosen by .NET if not specified.
        /// </summary>
        public SslProtocols? SslProtocols { get; set; }

        /// <summary>
        /// The password for the redis connection.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The connection timeout for the redis connection.
        /// Defaults to 100ms if not specified.
        /// </summary>
        public int? ConnectionTimeout { get; set; }

        /// <summary>
        /// The sync timeout for the redis connection.
        /// Defaults to 1000ms if not specified.
        /// </summary>
        public int? SyncTimeout { get; set; }

        /// <summary>
        /// The database to use with this redis connection.
        /// Defaults to 0 if not specified.
        /// </summary>
        public int? RedisDatabase { get; set; }

        /// <summary>
        /// The string format for keys created in redis, must include {0}.
        /// Defaults to "distributedLock:{0}" if not specified.
        /// </summary>
        public string RedisKeyFormat { get; set; }

        /// <summary>
        /// The number of seconds between config change checks
        /// Defaults to 10 seconds if not specified.
        /// </summary>
        public int? ConfigCheckSeconds { get; set; }
    }
}