using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Xieyi.DistributedLock.Helper;

internal static class RetryHelper
{
    internal static void Call(ILogger logger, Action action, TimeSpan retryInterval, int maxAttemptCount)
    {
        Call<object>(logger, () =>
        {
            action();
            return null;
        }, retryInterval, maxAttemptCount);
    }

    internal static T Call<T>(ILogger logger, Func<T> action, TimeSpan retryInterval, int maxAttemptCount)
    {
        var exceptions = new List<Exception>();

        if (maxAttemptCount == 0) maxAttemptCount = 1;
        
        for (var attempted = 1; attempted <= maxAttemptCount; attempted++)
        {
            try
            {
                if (attempted > 1)
                    Thread.Sleep(retryInterval);

                return action();
            }
            catch (SocketException e)
            {
                exceptions.Add(e);
                logger.LogWarning($"Try {attempted} times to handle method: {action.Method.Name}.", e);
            }
            catch (IOException e)
            {
                exceptions.Add(e);
                logger.LogWarning($"Try {attempted} times to handle method: {action.Method.Name}.", e);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
                logger.LogWarning($"Try {attempted} times to handle request: {action.Method.Name}.", e);
                break;
            }
        }

        throw new AggregateException(exceptions);
    }
}