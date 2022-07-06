// ----------------------------------------------------------------------
// <copyright file="WebHostTestFactory.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace SoloX.BlazorJsBlob.E2ETests
{
    public class WebHostTestFactory<TProgram> : WebApplicationFactory<TProgram>
        where TProgram : class
    {
        private readonly string urls;

        public WebHostTestFactory(string urls)
        {
            this.urls = urls;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseUrls(this.urls);
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // need to create a plain host that we can return.
            var dummyHost = builder.Build();

            // configure and start the actual host.
            builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel());

            var host = builder.Build();
            host.Start();

            return dummyHost;
        }
    }
}