// ----------------------------------------------------------------------
// <copyright file="BlobServiceOptions.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

namespace SoloX.BlazorJsBlob
{
    /// <summary>
    /// Blob service options.
    /// </summary>
    public class BlobServiceOptions
    {
        private const int DefaultBufferSize = 1024 * 32;

        /// <summary>
        /// Gets/Sets slice buffer size.
        /// </summary>
        public int SliceBufferSize { get; set; } = DefaultBufferSize;

        /// <summary>
        /// Enable Js module to log traces in browser console.
        /// </summary>
        public bool EnableJsModuleLogs { get; set; }
    }
}
