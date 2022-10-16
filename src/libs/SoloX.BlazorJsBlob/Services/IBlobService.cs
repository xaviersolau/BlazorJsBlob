// ----------------------------------------------------------------------
// <copyright file="IBlobService.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;

namespace SoloX.BlazorJsBlob
{
    /// <summary>
    /// Blob service interface.
    /// </summary>
    public interface IBlobService : IAsyncDisposable
    {
        /// <summary>
        /// Create a JS Blog from a given data stream.
        /// </summary>
        /// <param name="dataStream">The data stream to setup the blob with.</param>
        /// <param name="type">MIME type.</param>
        /// <returns>The created Blob.</returns>
        ValueTask<IBlob> CreateBlobAsync(Stream dataStream, string type = "application/octet-stream");

        /// <summary>
        /// Save the given blob as file.
        /// </summary>
        /// <param name="blob">The blob to save.</param>
        /// <param name="fileName">The file name to save.</param>
        /// <returns>The saving task.</returns>
        ValueTask SaveAsFileAsync(IBlob blob, string fileName);

        /// <summary>
        /// Save the given URL as file.
        /// </summary>
        /// <param name="href">The URL to save.</param>
        /// <param name="fileName">The file name to save.</param>
        /// <returns>The saving task.</returns>
        ValueTask SaveAsFileAsync(string href, string? fileName = null);
    }
}
