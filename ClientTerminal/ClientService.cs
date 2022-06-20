using Grpc.Core;
using gRPCDefinition;
using System.Collections.Concurrent;

namespace ClientTerminal
{
    internal class ClientProvider : IDisposable
    {
        public ClientProvider(
            string host,
            int port,
            string guid
            )
        {
            _headersMetadata = new Metadata() { { "guid", guid } };
            _channel = new Channel(host, port, ChannelCredentials.Insecure);
            _client = new gRPCService.gRPCServiceClient(_channel);

            var taskConnect = _channel.ConnectAsync();
            _cts = new();

            var taskCreateStream = taskConnect.ContinueWith(t =>
            {
                ReCreateCheckStatusStream();
                ReCreateCommandStream();
                CreateResponceStreamRoutine();
                CreateRequestStreamRoutine();
            }, _cts.Token);

            Task.Factory.StartNew(() =>
            {
                taskCreateStream.Wait(_cts.Token);

                while (!_cts.Token.IsCancellationRequested)
                {
                    lock (_recreateLock)
                    {
                        try
                        {
                            _checkStreamRemoved.RequestStream.WriteAsync(_nullMessage).Wait();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Disconnected to Server");

                            try
                            {
                                ReCreateCheckStatusStream();
                                ReCreateCommandStream();
                                CreateResponceStreamRoutine();
                            }
                            catch (Exception e)
                            {
                                //ignore
                            }
                        }
                    }

                    Thread.Sleep(1000);
                }

            }, _cts.Token);
        }

        private void ReCreateCheckStatusStream()
        {
            _checkStreamRemoved?.Dispose();
            _client.CheckConection(new (), new CallOptions(_headersMetadata, cancellationToken: _cts.Token).WithWaitForReady(true));
            Console.WriteLine("Connected to Server");
            _checkStreamRemoved = _client.CheckStreamRemoved(_headersMetadata, cancellationToken: _cts.Token);
        }

        private void ReCreateCommandStream()
        {
            _ctsStream?.Cancel();
            _ctsStream = new();
            _commandStream = _client.CommandStream(_headersMetadata, cancellationToken: _ctsStream.Token);
        }

        private void CreateResponceStreamRoutine()
        {
            _responceStreamRoutine = Task.Factory.StartNew(async () => 
            {
                while(!_cts.Token.IsCancellationRequested)
                {
                    IAsyncStreamReader<CommandResponce> reader;
                    lock (_recreateLock)
                    {
                        reader = _commandStream.ResponseStream;
                    }

                    try
                    {
                        while (await reader.MoveNext(_ctsStream.Token))
                        {
                            var current = reader.Current;
                            Console.WriteLine($"Command '{current.Guid}': {current.Status}");
                        }
                    }
                    catch (TaskCanceledException) { /*Ignore*/}
                    catch (AggregateException aggEx) 
                    {
                        if (aggEx.InnerExceptions.Any(ex => ex is not OperationCanceledException))
                            throw;
                    }
                }
            }, _ctsStream.Token);
        }

        private void CreateRequestStreamRoutine()
        {
            _requestStreamRoutine = Task.Factory.StartNew(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    if(_queue.TryDequeue(out var item))
                    {
                        IClientStreamWriter<CommandRequest> writer;
                        lock (_recreateLock)
                        {
                            writer = _commandStream.RequestStream;
                        }

                        var sended = false;
                        while (!sended)
                        {
                            try
                            {
                                await writer.WriteAsync(item);
                                sended = true;
                                Console.WriteLine($"Sended command '{item.Guid}': {item.Command}");
                            }
                            catch (TaskCanceledException) { /*Ignore*/}
                            catch (AggregateException aggEx)
                            {
                                if (aggEx.InnerExceptions.Any(ex => ex is not OperationCanceledException))
                                {
                                    lock (_recreateLock)
                                    {
                                        ReCreateCheckStatusStream();
                                        ReCreateCommandStream();
                                        writer = _commandStream.RequestStream;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        _mre.WaitOne(500);
                    }
                }

            },_cts.Token);
        }

        public void SendCommand(CommandRequest command)
        {
            _queue.Enqueue(command);
            _mre.Set();
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
                }
                catch { /*ignore*/ }

                try
                {
                    _ctsStream?.Cancel();
                }
                catch { /*ignore*/ }

                try
                {
                    _mre.Dispose();
                }
                catch { /*ignore*/ }

                try
                {
                    _channel.ShutdownAsync().Wait();
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

        ~ClientProvider()
        {
            Dispose(false);
        }

        #endregion

        private readonly Metadata _headersMetadata;
        private readonly Channel _channel;
        private readonly gRPCService.gRPCServiceClient _client;

        private readonly NullMessage _nullMessage = new();
        private AsyncClientStreamingCall<NullMessage, NullMessage>  _checkStreamRemoved;

        AsyncDuplexStreamingCall<CommandRequest, CommandResponce> _commandStream;
        private readonly object _recreateLock = new();
        private CancellationTokenSource _ctsStream;
        private Task _responceStreamRoutine;
        private Task _requestStreamRoutine;
        private ConcurrentQueue<CommandRequest> _queue = new();
        private ManualResetEvent _mre = new(false);

        private readonly CancellationTokenSource _cts;
    }
}