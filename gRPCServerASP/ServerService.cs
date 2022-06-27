using Grpc.Core;
using gRPCDefinition;
using gRPCServer.Helpers;
using gRPCServer.Interfaces;
using System;
using System.Threading.Tasks;

namespace gRPCServer
{
    internal class ServerService : gRPCService.gRPCServiceBase
    {
        private readonly ICommandProcessor _commandProcessor;
        private readonly NullMessage _message = new();

        public ServerService(ICommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        public override async Task CommandStream(
            IAsyncStreamReader<CommandRequest> requestStream,
            IServerStreamWriter<CommandResponce> responseStream,
            ServerCallContext context
            )
        {
            var guid = GrpcHeaderHelper.GetGuidFromHeaderOrThrowCancellCallException(context);
            Console.WriteLine($"Client with guid '{guid}': Connected");
            if (!_commandProcessor.AddClientTerminal(guid, out var channel))
            {
                throw new RpcException(new Grpc.Core.Status(StatusCode.InvalidArgument, "Invalid guid, duplicate client terminals"));
            }
            try
            {
                var taskRequest = Task.Factory.StartNew(async () =>
                {
                    while (await requestStream.MoveNext(context.CancellationToken))
                    {
                        var current = requestStream.Current;
                        Console.WriteLine($"Got a command '{current.Guid}': '{current.Command ?? "Empty command"}'.");
                        if (current.ProcessType == ProcessType.Immideatly)
                        {
                            _commandProcessor.AddImmidiateCommand(guid, requestStream.Current);
                        }
                        else
                        {
                            _commandProcessor.EnqueueCommand(guid, requestStream.Current);
                        }
                    }
                }, context.CancellationToken);

                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var current = await channel.ReadAsync(context.CancellationToken);
                    await responseStream.WriteAsync(current, context.CancellationToken);
                }
            }
            catch(OperationCanceledException)
            {
                //ignore
            }
            finally
            {
                _commandProcessor.DestroyClientTerminal(guid);
                Console.WriteLine($"Client with guid '{guid}': Disconnected");
            }
        }

        public override Task<NullMessage> CheckConection(NullMessage request, ServerCallContext context)
        {
            GrpcHeaderHelper.GetGuidFromHeaderOrThrowCancellCallException(context);
            return Task.FromResult(_message);
        }

        public override async Task<NullMessage> CheckStreamRemoved(IAsyncStreamReader<NullMessage> requestStream, ServerCallContext context)
        {
            try
            {
                while (await requestStream.MoveNext(context.CancellationToken))
                {
                }
            }
            catch
            {
                //ignore
            }

            return _message;
        }
    }
}