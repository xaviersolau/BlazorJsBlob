// ----------------------------------------------------------------------
// <copyright file="WebTestingHostFactory.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SoloX.BlazorJsBlob.E2ETests
{
    public class WebTestingHostFactory<TProgram> : WebApplicationFactory<TProgram>
        where TProgram : class
    {
        // Override the CreateHost to build our HTTP host server.
        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Create the host that is actually used by the TestServer (In Memory).
            var testHost = base.CreateHost(builder);

            // configure and start the actual host using Kestrel.
            builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel());
            var host = builder.Build();
            host.Start();

            // In order to cleanup and properly dispose HTTP server resources we return a composite host object
            // that is actually just a way to way to intercept the StopAsync and Dispose call and relay to our
            // HTTP host.
            return new CompositeHost(testHost, host);
        }

        // Relay the call to both test host and kestrel host.
        private class CompositeHost : IHost
        {
            private readonly IHost testHost;
            private readonly IHost kestrelHost;

            public CompositeHost(IHost testHost, IHost kestrelHost)
            {
                this.testHost = testHost;
                this.kestrelHost = kestrelHost;
            }

            public IServiceProvider Services => this.testHost.Services;

            public void Dispose()
            {
                this.testHost.Dispose();

                // Relay the call to kestrel host.
                this.kestrelHost.Dispose();
            }

            public async Task StartAsync(CancellationToken cancellationToken = default)
            {
                await this.testHost.StartAsync(cancellationToken);

                // Relay the call to kestrel host.
                await this.kestrelHost.StartAsync(cancellationToken);
            }

            public async Task StopAsync(CancellationToken cancellationToken = default)
            {
                await this.testHost.StopAsync(cancellationToken);

                // Relay the call to kestrel host.
                await this.kestrelHost.StopAsync(cancellationToken);
            }
        }
    }
}