// ----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionTest.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Moq;
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
            var servicesMock = new Mock<IServiceCollection>();

            var services = servicesMock.Object;

            services.AddJsBlob(serviceLifetime);

            servicesMock.Verify(x => x.Add(It.Is<ServiceDescriptor>(d => d.Lifetime == serviceLifetime && d.ServiceType == typeof(IBlobService))), Times.Once());
        }

    }
}
