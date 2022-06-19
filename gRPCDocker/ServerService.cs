using Grpc.Core;
using gRPCDefinition;
using gRPCServer.Helpers;
using gRPCServer.Interfaces;

namespace gRPCServer
{
    public class ServerService : gRPCService.gRPCServiceBase
    {
        ICommandProcessor _commandProcessor;

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
                if(!_commandProcessor.AddClientTerminal(guid, out var channel))
                {
                    throw new RpcException(new Grpc.Core.Status(StatusCode.InvalidArgument, "Invalid guid, duplicate client terminals"));
                }
                try
                {
                    var taskRequest = Task.Factory.StartNew(async () =>
                    {
                        while (!context.CancellationToken.IsCancellationRequested && await requestStream.MoveNext())
                        {
                            _commandProcessor.EnqueueCommand(requestStream.Current);
                        }
                    }, context.CancellationToken);


                    while (!context.CancellationToken.IsCancellationRequested && await channel.WaitToReadAsync(context.CancellationToken))
                    {
                        var current = await channel.ReadAsync(context.CancellationToken);
                    }
                }
                finally
                {
                    _commandProcessor.DestroyClientTerminal(guid);
                }
            });
        }
    }
}