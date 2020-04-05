using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using XOutput.Core.DependencyInjection;
using XOutput.Server.Emulation;
using XOutput.Server.Websocket;

namespace XOutput.Server
{
    public class Startup
    {
        private readonly ApplicationContext applicationContext;
        private readonly WebSocketService webSocketService;

        public Startup()
        {
            applicationContext = ApplicationContext.Global;
            webSocketService = applicationContext.Resolve<WebSocketService>();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            RegisterSingleton<EmulatorsController>(services);

            services.AddMvc().AddControllersAsServices();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            RegisterWebsocket(app);
            app.UseFileServer(enableDirectoryBrowsing: false);

            var contentRoot = Directory.GetCurrentDirectory();
            var webRoot = Path.Join(contentRoot, "webapp");
            if(!Directory.Exists(webRoot))
            {
                Directory.CreateDirectory(webRoot);
            }

            env.ContentRootPath = contentRoot;
            env.ContentRootFileProvider = new PhysicalFileProvider(contentRoot);
            env.WebRootPath = webRoot;
            env.WebRootFileProvider = new PhysicalFileProvider(webRoot);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void RegisterWebsocket(IApplicationBuilder app)
        {
            app.UseWebSockets(new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(300),
                ReceiveBufferSize = 4 * 1024
            });
            app.Use(async (context, next) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    await webSocketService.HandleWebSocketAsync(context, new CancellationTokenSource().Token);
                }
                else
                {
                    await next();
                }

            });
        }

        private void RegisterSingleton<T>(IServiceCollection services) where T : class
        {
            T singleton = applicationContext.Resolve<T>();
            services.AddSingleton<T>(singleton);
        }
    }
}
