using gRPCDefinition;
using Server.Interfaces;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Server.Impl
{
    public class CommandProcessor : ICommandProcessor, IDisposable
    {
        private class CommandWrapper
        {
            public CommandWrapper(
                string guidTerminal,
                CommandRequest command)
            {
                GuidTerminal = guidTerminal;
                Command = command;
            }

            public string GuidTerminal;
            public CommandRequest Command;
        }

        public CommandProcessor()
        {
            Initialize();
        }

        private void Initialize()
        {
            _cts = new CancellationTokenSource();
            _mres = new ManualResetEvent(false);
            _routine =
                Task.Factory.StartNew(async () =>
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        CommandWrapper immediateCommand = null;
                        while (_commandRequests.TryDequeue(out var request))
                        {
                            if (_cts.IsCancellationRequested)
                                return;

                            if (_clientTerminals.TryGetValue(request.GuidTerminal, out var terminal))
                            {
                                var startProcessing = new CommandResponce();
                                startProcessing.Guid = request.Command.Guid;
                                startProcessing.Status = Status.StartProcess;
                                await terminal.Writer.WriteAsync(startProcessing, _cts.Token);

                                //TODO processing

                                var endProcessing = new CommandResponce();
                                endProcessing.Guid = request.Command.Guid;
                                endProcessing.Status = Status.Completed;
                                await terminal.Writer.WriteAsync(endProcessing, _cts.Token);
                            }

                            immediateCommand = Volatile.Read(ref _immediateCommand);
                            if (immediateCommand != null)
                            {
                                break;
                            }
                        }

                        if (immediateCommand != null)
                        {
                            //TODO processing
                            Interlocked.Exchange(ref immediateCommand, null);
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

        public void AddImmidiateCommand(string guidTerminal, CommandRequest command)
        {
            lock(_addLock)
            {
                var currentCommand = Volatile.Read(ref _immediateCommand);
                if(currentCommand != null)
                {
                    if (_clientTerminals.TryGetValue(guidTerminal, out var terminal))
                    {
                        var rejected = new CommandResponce
                        {
                            Guid = command.Guid,
                            Status = Status.Rejected
                        };
                        terminal.Writer.WriteAsync(rejected, _cts.Token).AsTask().Wait();
                    }
                }
                else
                {
                    _immediateCommand = new(guidTerminal, command);
                }
            }
        }

        public void EnqueueCommand(string guidTerminal, CommandRequest command)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            _commandRequests.Enqueue(new (guidTerminal, command));
            _mres.Set();
        }

        public bool AddClientTerminal(string guid, out ChannelReader<CommandResponce> reader)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);

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

        public bool DestroyClientTerminal(string guid)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);

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
                    _routine = null;
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

        private readonly ConcurrentQueue<CommandWrapper> _commandRequests = new();
        private readonly ConcurrentDictionary<string, Channel<CommandResponce>> _clientTerminals = new();

        private Task _routine;
        private CancellationTokenSource _cts;
        private ManualResetEvent _mres;

        private readonly object _addLock = new ();
        private CommandWrapper _immediateCommand;
    }
}