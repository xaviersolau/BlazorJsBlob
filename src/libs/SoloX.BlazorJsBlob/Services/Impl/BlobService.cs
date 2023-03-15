// ----------------------------------------------------------------------
// <copyright file="BlobService.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SoloX.BlazorJsBlob.Services.Impl
{
    /// <summary>
    /// Blob service that provides JS Blob storage access.
    /// </summary>
    public class BlobService : IBlobService
    {
        internal const string Import = "import";
        internal const string BlobManagerJsInteropFile = "./_content/SoloX.BlazorJsBlob/JsBlobInterop.js";

        private readonly Lazy<Task<IModuleStrategy>> moduleStrategyTask;

        private readonly BlobServiceOptions options;
        private readonly IBufferService bufferService;
        private readonly ILogger<BlobService> logger;

        /// <summary>
        /// Setup instance.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="jsRuntime">The JS runtime interoperability instance.</param>
        /// <param name="bufferService"></param>
        /// <param name="logger">Logger.</param>
        public BlobService(IOptions<BlobServiceOptions> options, IJSRuntime jsRuntime, IBufferService bufferService, ILogger<BlobService> logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this.options = options.Value;
            this.bufferService = bufferService;
            this.logger = logger;

            if (jsRuntime is IJSInProcessRuntime jsInProcessRuntime)
            {
                this.moduleStrategyTask = new Lazy<Task<IModuleStrategy>>(async () =>
                {
                    var module = await jsRuntime.InvokeAsync<IJSInProcessObjectReference>(Import, BlobManagerJsInteropFile).ConfigureAwait(false);

                    var strategy = new InProcessModuleStrategy(module, this.options, this.bufferService, this.logger);

                    await strategy.SetupJsLogsAsync().ConfigureAwait(false);

                    return strategy;
                });
            }
            else
            {
                this.moduleStrategyTask = new Lazy<Task<IModuleStrategy>>(async () =>
                {
                    var module = await jsRuntime.InvokeAsync<IJSObjectReference>(Import, BlobManagerJsInteropFile).ConfigureAwait(false);

                    var strategy = new ModuleStrategy(module, this.options, this.bufferService, this.logger);

                    await strategy.SetupJsLogsAsync().ConfigureAwait(false);

                    return strategy;
                });
            }
        }

        /// <inheritdoc/>
        public async ValueTask<IBlob> CreateBlobAsync(Stream dataStream, string type)
        {
            if (dataStream == null)
            {
                throw new ArgumentNullException(nameof(dataStream));
            }

            return await CreateBlobAsync(
                async writeStream =>
                {
                    await dataStream.CopyToAsync(writeStream).ConfigureAwait(false);
                },
                type).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async ValueTask<IBlob> CreateBlobAsync(Func<Stream, ValueTask> writer, string type)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            this.logger.LogInformation($"Create Blob with type {type}");

            var bufferGuid = Guid.NewGuid();

            var moduleStrategy = await this.moduleStrategyTask.Value.ConfigureAwait(false);

            var blob = await moduleStrategy.CreateBlobAsync(bufferGuid, type, writer).ConfigureAwait(false);

            this.logger.LogInformation($"Blob with type {type} created");

            return blob;
        }

        /// <inheritdoc/>
        public async ValueTask SaveAsFileAsync(IBlob blob, string fileName)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            var moduleStrategy = await this.moduleStrategyTask.Value.ConfigureAwait(false);

            await moduleStrategy.SaveAsFileAsync(blob, fileName).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async ValueTask SaveAsFileAsync(string href, string? fileName = null)
        {
            if (string.IsNullOrEmpty(href))
            {
                throw new ArgumentNullException(nameof(href));
            }

            // Get a file name if not provided
            if (string.IsNullOrEmpty(fileName))
            {
                if (Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out var uri) && uri.IsAbsoluteUri)
                {
                    // looks like this is an Url
                    var localPath = uri.LocalPath;
                    fileName = localPath.Split('/').LastOrDefault();
                }
                else
                {
                    fileName = href.Split('/').LastOrDefault();
                }

                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = "Download_" + Guid.NewGuid().ToString();
                }

                // remove query parameters
                if (fileName.Contains('?', StringComparison.Ordinal))
                {
                    fileName = fileName.Split('?').First();
                }
            }

            var moduleStrategy = await this.moduleStrategyTask.Value.ConfigureAwait(false);

            await moduleStrategy.SaveAsFileAsync(href, fileName).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (this.moduleStrategyTask.IsValueCreated)
            {
                var moduleStrategy = await this.moduleStrategyTask.Value.ConfigureAwait(false);
                await moduleStrategy.DisposeAsync().ConfigureAwait(false);
            }

            GC.SuppressFinalize(this);
        }
    }
}