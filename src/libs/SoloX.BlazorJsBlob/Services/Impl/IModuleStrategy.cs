// ----------------------------------------------------------------------
// <copyright file="IModuleStrategy.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;

namespace SoloX.BlazorJsBlob.Services.Impl
{
    /// <summary>
    /// JS Module strategy to handle InProcess (WASM) or not (Server Side).
    /// </summary>
    public interface IModuleStrategy : IAsyncDisposable
    {
        /// <summary>
        /// Create the Blob from the given writer.
        /// </summary>
        /// <param name="bufferGuid">JS buffer ID.</param>
        /// <param name="type">Data type.</param>
        /// <param name="writer">Writer to write the stream.</param>
        /// <returns>The resulting JS Blob.</returns>
        ValueTask<IBlob> CreateBlobAsync(Guid bufferGuid, string type, Func<Stream, ValueTask> writer);

        /// <summary>
        /// Save as a file the given Blob.
        /// </summary>
        /// <param name="blob">The blob to save.</param>
        /// <param name="fileName">The target file name.</param>
        /// <returns>The asynchronous task.</returns>
        ValueTask SaveAsFileAsync(IBlob blob, string fileName);

        /// <summary>
        /// Save as a file the given Url.
        /// </summary>
        /// <param name="uri">The Uri to save.</param>
        /// <param name="fileName">The target file name.</param>
        /// <returns>The asynchronous task.</returns>
        ValueTask SaveAsFileAsync(string uri, string fileName);

        /// <summary>
        /// Dispose the given Blob.
        /// </summary>
        /// <param name="blob">The Blob to dispose.</param>
        /// <returns>The asynchronous task.</returns>
        ValueTask DisposeBlobAsync(IBlob blob);
    }
}