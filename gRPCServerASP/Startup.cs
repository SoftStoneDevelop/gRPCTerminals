using gRPCServer.Impl;
using gRPCServer.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace gRPCServer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc((options) =>
            {
                options.MaxReceiveMessageSize = Int32.MaxValue;
                options.MaxSendMessageSize    = Int32.MaxValue;
            });

            services.AddSingleton<ICommandProcessor, CommandProcessor>();
        }

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            IHostApplicationLifetime lifetime
            )
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<ServerService>();
            });
        }
    }
}
