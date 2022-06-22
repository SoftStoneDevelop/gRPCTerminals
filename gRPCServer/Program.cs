using Grpc.Core;
using gRPCDefinition;
using Server.Impl;
using System;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //if (args.Length < 1)
            //{
            //    throw new ArgumentException($"Expected minimum arguments count 1");
            //}

            //var host = args[0];
            var host = "127.0.0.1";
            Console.WriteLine($"Using Host={host}");

            int port;
            if(args.Length > 1 && int.TryParse(args[1], out var portParse))
            {
                port = portParse;
            }
            else
            {
                port = 6024;
                Console.WriteLine($"Using default Port={port}");
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
                new Grpc.Core.Server(options)
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