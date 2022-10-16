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
        internal const string Ping = "blobManager.ping";
        internal const string Import = "import";
        internal const string BlobManagerJsInteropFile = "./_content/SoloX.BlazorJsBlob/JsBlobInterop.js";

#if NET6_0_OR_GREATER
        /// <remark>
        /// Since .Net 6.0 IJSRuntime can directly use byte array without base 64 conversion: On JS side we get a Uint8Array.
        /// </remark>
        internal const string AddToBuffer = "blobManager.addToBuffer";
#else
        /// <remark>
        /// Before .Net 6.0 IJSRuntime needed to convert byte array to base 64: On JS side we get a Base64-encoded string.
        /// </remark>
        internal const string AddToBuffer = "blobManager.addToBufferB64";
#endif

        internal const string CreateBuffer = "blobManager.createBuffer";
        internal const string DeleteBuffer = "blobManager.deleteBuffer";
        internal const string CreateBlob = "blobManager.createBlob";
        internal const string DeleteBlob = "blobManager.deleteBlob";
        internal const string SaveBlobAsFile = "blobManager.saveAsFile";

        private readonly Lazy<Task<IJSObjectReference>> moduleTask;
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

            this.moduleTask = new Lazy<Task<IJSObjectReference>>(
                () => jsRuntime.InvokeAsync<IJSObjectReference>(Import, BlobManagerJsInteropFile).AsTask());
        }

        /// <inheritdoc/>
        public async ValueTask<IBlob> CreateBlobAsync(Stream dataStream, string type)
        {
            if (dataStream == null)
            {
                throw new ArgumentNullException(nameof(dataStream));
            }

            this.logger.LogInformation($"Create Blob with type {type}");

            var bufferGuid = Guid.NewGuid().ToString();

            var bufferArray = this.bufferService.Rent(this.options.SliceBufferSize);

            var module = await this.moduleTask.Value.ConfigureAwait(false);

            try
            {
                var res = await module.InvokeAsync<bool>(Ping).ConfigureAwait(false);

                await module.InvokeVoidAsync(CreateBuffer, bufferGuid).ConfigureAwait(false);

                var buffer = new Memory<byte>(bufferArray);

                var size = await dataStream.ReadAsync(buffer).ConfigureAwait(false);
                var totalSize = size;

                while (size > 0)
                {
                    await module.InvokeVoidAsync(AddToBuffer, bufferGuid, bufferArray, size).ConfigureAwait(false);

                    size = await dataStream.ReadAsync(buffer).ConfigureAwait(false);
                    totalSize += size;
                }

                var blobUrl = await module.InvokeAsync<string>(CreateBlob, bufferGuid, type).ConfigureAwait(false);

                await module.InvokeVoidAsync(DeleteBuffer, bufferGuid).ConfigureAwait(false);

                this.logger.LogInformation($"Blob with type {type} created (size: {totalSize} bytes)");

                return new Blob(this, blobUrl, type);
            }
            finally
            {
                this.bufferService.Return(bufferArray);
            }
        }

        /// <inheritdoc/>
        public async ValueTask SaveAsFileAsync(IBlob blob, string fileName)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            var module = await this.moduleTask.Value.ConfigureAwait(false);

            await module.InvokeVoidAsync(SaveBlobAsFile, blob.Uri.ToString(), fileName).ConfigureAwait(false);
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

            var module = await this.moduleTask.Value.ConfigureAwait(false);

            await module.InvokeVoidAsync(SaveBlobAsFile, href, fileName).ConfigureAwait(false);
        }

        private async ValueTask DisposeBlobAsync(Blob blob)
        {
            var module = await this.moduleTask.Value.ConfigureAwait(false);

            await module.InvokeVoidAsync(DeleteBlob, blob.Uri.ToString()).ConfigureAwait(false);
        }

        private class Blob : IBlob
        {
            private readonly BlobService blobService;

            public Blob(BlobService blobService, string blobUrl, string type)
            {
                this.blobService = blobService;
                Uri = new Uri(blobUrl);
                Type = type;
            }

            public Uri Uri { get; }

            public string Type { get; }

            public ValueTask DisposeAsync()
            {
                return this.blobService.DisposeBlobAsync(this);
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (this.moduleTask.IsValueCreated)
            {
                var module = await this.moduleTask.Value.ConfigureAwait(false);

                try
                {
                    // make sure JS runtime is steel responding otherwise disposing the module may block forever.
                    await module.InvokeAsync<bool>(Ping,
                        TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                    await module.DisposeAsync().ConfigureAwait(false);
                }
                catch (TaskCanceledException e)
                {
                    this.logger.LogDebug(e.Message);
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}