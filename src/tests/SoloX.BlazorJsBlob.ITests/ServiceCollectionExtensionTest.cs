// ----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionTest.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using SoloX.CodeQuality.Test.Helpers.XUnit;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

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

            services.AddSingleton(Mock.Of<IJSRuntime>());
            services.AddTestLogging(this.testOutputHelper);

            services.AddJsBlob();

            await using var serviceProvider = services.BuildServiceProvider();

            var blobService = serviceProvider.GetRequiredService<IBlobService>();

            blobService.Should().NotBeNull();
        }
    }
}