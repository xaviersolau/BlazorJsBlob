// ----------------------------------------------------------------------
// <copyright file="AModuleStrategy.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SoloX.BlazorJsBlob.Services.Impl
{
    /// <summary>
    /// ModuleStrategy base abstract class.
    /// </summary>
    /// <typeparam name="TJSObjectReference">Type of JS Object reference.</typeparam>
    public abstract class AModuleStrategy<TJSObjectReference> : IModuleStrategy where TJSObjectReference : IJSObjectReference
    {
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
        internal const string Ping = "blobManager.ping";

        private readonly BlobServiceOptions options;
        private readonly IBufferService bufferService;
        private readonly ILogger<BlobService> logger;

        /// <summary>
        /// Module JS Object Reference.
        /// </summary>
        protected TJSObjectReference Module { get; }

        /// <summary>
        /// Setup the AModuleStrategy with its JS Object reference and all required dependencies.
        /// </summary>
        /// <param name="module">JS Module Object Reference.</param>
        /// <param name="options">Blob service options.</param>
        /// <param name="bufferService">Buffer service.</param>
        /// <param name="logger">Blob Service Logger.</param>
        protected AModuleStrategy(TJSObjectReference module, BlobServiceOptions options, IBufferService bufferService, ILogger<BlobService> logger)
        {
            Module = module;
            this.options = options;
            this.bufferService = bufferService;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async ValueTask<IBlob> CreateBlobAsync(Guid bufferGuid, string type, Func<Stream, ValueTask> writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var bufferGuidStr = bufferGuid.ToString();

            var res = await Module.InvokeAsync<bool>(Ping).ConfigureAwait(false);

            await Module.InvokeVoidAsync(CreateBuffer, bufferGuidStr).ConfigureAwait(false);

#pragma warning disable CA2000 // Supprimer les objets avant la mise hors de portée
            var stream = CreateStream(this.bufferService, this.options.SliceBufferSize, bufferGuidStr);
            await using var _ = stream.ConfigureAwait(false);
#pragma warning restore CA2000 // Supprimer les objets avant la mise hors de portée

            await writer(stream).ConfigureAwait(false);

            await stream.FlushAsync().ConfigureAwait(false);

            var totalSize = stream.Length;

            var blobUrl = await Module.InvokeAsync<string>(CreateBlob, bufferGuidStr, type).ConfigureAwait(false);

            await Module.InvokeVoidAsync(DeleteBuffer, bufferGuidStr).ConfigureAwait(false);

            return new Blob(this, blobUrl, type);
        }

        /// <inheritdoc/>
        public ValueTask SaveAsFileAsync(IBlob blob, string fileName)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            return Module.InvokeVoidAsync(SaveBlobAsFile, blob.Uri.ToString(), fileName);
        }

        /// <inheritdoc/>
        public ValueTask SaveAsFileAsync(string uri, string fileName)
        {
            return Module.InvokeVoidAsync(SaveBlobAsFile, uri, fileName);
        }

        /// <inheritdoc/>
        public ValueTask DisposeBlobAsync(IBlob blob)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            return Module.InvokeVoidAsync(DeleteBlob, blob.Uri.ToString());
        }

        /// <summary>
        /// Create the right stream implementation depending on the strategy.
        /// </summary>
        /// <param name="bufferService"></param>
        /// <param name="sliceBufferSize"></param>
        /// <param name="bufferGuid"></param>
        /// <returns></returns>
        protected abstract Stream CreateStream(IBufferService bufferService, int sliceBufferSize, string bufferGuid);

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            try
            {
                // make sure JS runtime is steel responding otherwise disposing the module may block forever.
                await Module.InvokeAsync<bool>(Ping,
                    TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                await Module.DisposeAsync().ConfigureAwait(false);
            }
            catch (TaskCanceledException e)
            {
                this.logger.LogDebug(e.Message);
            }

            GC.SuppressFinalize(this);
        }

        private class Blob : IBlob
        {
            private readonly IModuleStrategy blobStrategy;

            public Blob(IModuleStrategy blobStrategy, string blobUrl, string type)
            {
                this.blobStrategy = blobStrategy;
                Uri = new Uri(blobUrl);
                Type = type;
            }

            public Uri Uri { get; }

            public string Type { get; }

            public ValueTask DisposeAsync()
            {
                return this.blobStrategy.DisposeBlobAsync(this);
            }
        }
    }
}