using Grpc.Core;
using gRPCDefinition;
using Server.Helpers;
using Server.Interfaces;

namespace Server
{
    internal class ServerService : gRPCService.gRPCServiceBase
    {
        private readonly ICommandProcessor _commandProcessor;

        public ServerService(ICommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        public override Task CommandStream(
            IAsyncStreamReader<CommandRequest> requestStream,
            IServerStreamWriter<CommandResponce> responseStream,
            ServerCallContext context
            )
        {
            return Task.Factory.StartNew(async () =>
            {
                var guid = GrpcHeaderHelper.GetGuidFromHeaderOrThrowCancellCallException(context);
                Console.WriteLine($"Client with guid '{guid}': Connected");
                if(!_commandProcessor.AddClientTerminal(guid, out var channel))
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
                            Console.WriteLine($"Got a command '{current.Guid}': {current.Command}");
                            if(current.ProcessType == ProcessType.Immideatly)
                            {
                                _commandProcessor.AddImmidiateCommand(guid, requestStream.Current);
                            }
                            else
                            {
                                _commandProcessor.EnqueueCommand(guid, requestStream.Current);
                            }
                        }
                    }, context.CancellationToken);

                    while(!context.CancellationToken.IsCancellationRequested)
                    {
                        var current = await channel.ReadAsync(context.CancellationToken);
                        await responseStream.WriteAsync(current);
                    }
                }
                finally
                {
                    _commandProcessor.DestroyClientTerminal(guid);
                }
                Console.WriteLine($"Client with guid '{guid}': Disconnected");
            });
        }

        public override Task<NullMessage> CheckConection(NullMessage request, ServerCallContext context)
        {
            GrpcHeaderHelper.GetGuidFromHeaderOrThrowCancellCallException(context);
            return Task.FromResult(new NullMessage());
        }

        public override async Task<NullMessage> CheckStreamRemoved(IAsyncStreamReader<NullMessage> requestStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext(context.CancellationToken).ConfigureAwait(false))
            {
            }

            return new NullMessage();
        }
    }
}