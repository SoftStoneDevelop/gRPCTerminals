using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace gRPCServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel();
                    webBuilder.ConfigureKestrel(serverOptions => 
                    {
                        serverOptions.ListenAnyIP(6024, listenOptions =>
                        {
                            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
                            //listenOptions.UseHttps();

                        });
                    });
                    webBuilder.UseStartup<Startup>();
                })
                .Build();

            host.Run();
        }
    }
}