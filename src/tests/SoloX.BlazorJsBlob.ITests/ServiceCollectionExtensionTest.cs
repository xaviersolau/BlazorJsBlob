// ----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionTest.cs" company="Xavier Solau">
// Copyright © 2022-2026 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using SoloX.CodeQuality.Test.Helpers.XUnit.V3;
using System.Threading.Tasks;
using Xunit;

namespace SoloX.BlazorJsBlob.ITests
{
    public class ServiceCollectionExtensionTest
    {
        private readonly ITestOutputHelper testOutputHelper;
        public ServiceCollectionExtensionTest(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task ItShouldSetupServiceCollectionWithBlogService()
        {
            var services = new ServiceCollection();

            services.AddSingleton(Substitute.For<IJSRuntime>());
            services.AddTestLogging(this.testOutputHelper);

            services.AddJsBlob();

            var serviceProvider = services.BuildServiceProvider();
            await using var _ = serviceProvider.ConfigureAwait(false);

            var blobService = serviceProvider.GetRequiredService<IBlobService>();

            blobService.ShouldNotBeNull();
        }
    }
}