// ----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using SoloX.BlazorJsBlob.Services.Impl;
using System;

namespace SoloX.BlazorJsBlob
{
    /// <summary>
    /// Extension methods to setup the JS Blob services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add JS Blob service.
        /// </summary>
        /// <param name="services">The service collection to setup.</param>
        /// <param name="serviceLifetime">Service Lifetime to use to register the IStringLocalizerFactory. (Default is Scoped)</param>
        /// <returns>The given service collection updated with the JS Blob services.</returns>
        public static IServiceCollection AddJsBlob(this IServiceCollection services, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            => services.AddJsBlob(_ => { }, serviceLifetime);

        /// <summary>
        /// Add JS Blob service.
        /// </summary>
        /// <param name="services">The service collection to setup.</param>
        /// <param name="optionsBuilder">Options builder action delegate.</param>
        /// <param name="serviceLifetime">Service Lifetime to use to register the IStringLocalizerFactory. (Default is Scoped)</param>
        /// <returns>The given service collection updated with the JS Blob services.</returns>
        public static IServiceCollection AddJsBlob(this IServiceCollection services, Action<BlobServiceOptions> optionsBuilder, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        {
            services.AddSingleton<IBufferService, BufferService>();

            switch (serviceLifetime)
            {
                case ServiceLifetime.Singleton:
                    services.AddSingleton<IBlobService, BlobService>();
                    break;
                case ServiceLifetime.Scoped:
                    services.AddScoped<IBlobService, BlobService>();
                    break;
                case ServiceLifetime.Transient:
                default:
                    services.AddTransient<IBlobService, BlobService>();
                    break;
            }

            services.Configure(optionsBuilder);

            return services;
        }
    }
}
