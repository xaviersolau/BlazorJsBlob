// ----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionTest.cs" company="Xavier Solau">
// Copyright © 2022-2026 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace SoloX.BlazorJsBlob.UTests
{
    public class ServiceCollectionExtensionTest
    {
        [Theory]
        [InlineData(ServiceLifetime.Singleton)]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Transient)]
        public void ItShouldSetupServiceCollectionWithBlogService(ServiceLifetime serviceLifetime)
        {
            var servicesMock = Substitute.For<IServiceCollection>();

            var services = servicesMock;

            services.AddJsBlob(serviceLifetime);

            servicesMock.Received().Add(Arg.Is<ServiceDescriptor>(d => d.Lifetime == serviceLifetime && d.ServiceType == typeof(IBlobService)));
        }
    }
}
