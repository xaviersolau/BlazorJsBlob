// ----------------------------------------------------------------------
// <copyright file="BufferService.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using System.Buffers;

namespace SoloX.BlazorJsBlob.Services.Impl
{
    /// <summary>
    /// Buffer Service implementation using ArrayPool<byte>.Shared
    /// </summary>
    public class BufferService : IBufferService
    {
        /// <inheritdoc/>
        public byte[] Rent(int bufferSize)
        {
            return ArrayPool<byte>.Shared.Rent(bufferSize);
        }

        /// <inheritdoc/>
        public void Return(byte[] bufferArray)
        {
            ArrayPool<byte>.Shared.Return(bufferArray);
        }
    }
}
