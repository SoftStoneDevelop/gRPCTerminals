using Grpc.Core;
using gRPCDefinition;
using gRPCServer.Impl;

namespace gRPCServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                throw new ArgumentException($"Expected minimum arguments count 1");
            }

            var host = args[0];
            Console.WriteLine($"Using Host={host}");

            int port;
            if(args.Length > 1 && int.TryParse(args[1], out var portParse))
            {
                port = portParse;
            }
            else
            {
                Console.WriteLine("Using default Port=4823");
                port = 4823;
            }

            var options =
                new[]
                {
                    new ChannelOption(ChannelOptions.MaxReceiveMessageLength, Int32.MaxValue),
                    new ChannelOption(ChannelOptions.MaxSendMessageLength,    Int32.MaxValue),
                };

            var processor = new CommandProcessor();
            var service = new ServerService(processor);

            var grpcDBServer =
                new Server(options)
                {
                    Services = { gRPCService.BindService(service) },
                    Ports = { new ServerPort(host, port, ServerCredentials.Insecure) },
                };

            grpcDBServer.Start();
            Console.WriteLine("Press Enter to stop server");
            Console.ReadLine();
            grpcDBServer.ShutdownAsync().Wait();
        }
    }
}