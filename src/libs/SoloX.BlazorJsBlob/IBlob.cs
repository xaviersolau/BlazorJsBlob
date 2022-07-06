// ----------------------------------------------------------------------
// <copyright file="IBlob.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using System;

namespace SoloX.BlazorJsBlob
{
    /// <summary>
    /// Blob interface
    /// </summary>
    public interface IBlob : IAsyncDisposable
    {
        /// <summary>
        /// Get the Blob Uri.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// Blob MIME type.
        /// </summary>
        string Type { get; }
    }
}
