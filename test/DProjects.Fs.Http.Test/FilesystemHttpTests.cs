using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

using DProjects.Fs.Test;
using DProjects.Fs.Http;
using DProjects.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using DProjects.Factories;
using System.Runtime.CompilerServices;

namespace DProjects.Fs.Http.Test {

    [Trait("Category", "Integration")]
    public class FilesystemHttpTests : FilesystemTests {

        //vars
        private IHost mHost;

        //ctor
        public FilesystemHttpTests() : base("http://127.0.0.1:82/") {
            var host = new HostBuilder().ConfigureWebHost(webBuilder => {
                webBuilder.UseKestrel(options => {
                    options.ListenLocalhost(82);
                    options.AllowSynchronousIO = true;
                });
                webBuilder.ConfigureServices(services => {
                    services.AddSingleton<IFactoryByUrl<IFilesystem>>(services => {
                        return new DProjects.Fs.FilesystemMemFactory();
                    });
                });
                webBuilder.Configure(app => {
                    var options = new FilesystemHttpMiddleware.Options("/", "mem:") { 
                        AllowAnonymous = true,
                        Mode = FilesystemHttpMiddleware.Modes.ReadWrite
                    };
                    app.UseMiddleware<DProjects.Fs.Http.FilesystemHttpMiddleware>(options);
                });
            });
            mHost = host.Start();
        }
        public override void Dispose() {
            mHost.Dispose();
            base.Dispose();
        }

    }
} 
 