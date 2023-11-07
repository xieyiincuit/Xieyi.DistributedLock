namespace Xieyi.DistributedLock.Renew
{
    internal abstract class ShutdownableThread
    {
        protected CancellationTokenSource _cancellation;
        protected Thread _worker;

        internal ShutdownableThread()
        {
        }

        internal bool Started { get; private set; }
        internal bool Stopped { get; private set; }

        protected abstract void Run();

        internal virtual void Start()
        {
            _cancellation = new CancellationTokenSource();

            _worker = new Thread(Run) { IsBackground = true };
            _worker.Start();

            Started = true;
            Stopped = false;
        }

        protected virtual void Stop()
        {
            _cancellation?.Cancel();

            Started = false;
            Stopped = true;
        }

        protected virtual void Join()
        {
            _worker?.Join();
        }

        internal void StopAndJoin()
        {
            Stop();
            Join();
        }
    }
}