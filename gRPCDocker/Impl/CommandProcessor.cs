using gRPCDefinition;
using gRPCServer.Interfaces;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace gRPCServer.Impl
{
    public class CommandProcessor : ICommandProcessor, IDisposable
    {
        public CommandProcessor()
        {
            Initialize();
        }

        private void Initialize()
        {
            _cts = new CancellationTokenSource();
            _mres = new ManualResetEvent(false);
            _routine =
                Task.Factory.StartNew(() =>
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        while (_commandRequests.TryDequeue(out var request))
                        {
                            if (_cts.IsCancellationRequested)
                                return;

                            //TODO processing
                        }

                        if (!_commandRequests.IsEmpty)
                        {
                            continue;
                        }
                        else
                        {
                            _mres.WaitOne(250);
                        }
                    }
                }, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void EnqueueCommand(CommandRequest command)
        {
            _commandRequests.Enqueue(command);
            _mres.Set();
        }

        public bool AddClientTerminal(Guid guid, out ChannelReader<CommandResponce> reader)
        {
            var channel = Channel.CreateUnbounded<CommandResponce>(new UnboundedChannelOptions { SingleWriter = true, SingleReader = true });
            if (_clientTerminals.TryAdd(guid, channel))
            {
                reader = channel;
                return true;
            }
            else
            {
                reader = null;
                return false;
            }
        }

        public bool DestroyClientTerminal(Guid guid)
        {
            if (_clientTerminals.TryRemove(guid, out _))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #region IDisposable

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    _cts.Cancel();
                    _cts.Dispose();
                    _cts = null;
                }
                catch { /*ignore*/ }

                try
                {
                    _routine.Wait();
                }
                catch { /*ignore*/ }

                try
                {
                    _mres.Dispose();
                    _mres = null;
                }
                catch { /*ignore*/ }
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CommandProcessor()
        {
            Dispose(false);
        }

        #endregion

        private readonly ConcurrentQueue<CommandRequest> _commandRequests = new();
        private readonly ConcurrentDictionary<Guid, Channel<CommandResponce>> _clientTerminals = new();

        private Task _routine;
        private CancellationTokenSource _cts;
        private ManualResetEvent _mres;
    }
}
