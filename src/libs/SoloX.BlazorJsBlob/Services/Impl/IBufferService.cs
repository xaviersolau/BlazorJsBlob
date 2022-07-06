// ----------------------------------------------------------------------
// <copyright file="IBufferService.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

namespace SoloX.BlazorJsBlob.Services.Impl
{
    /// <summary>
    /// Buffer service.
    /// </summary>
    public interface IBufferService
    {
        /// <summary>
        /// Rent a buffer.
        /// </summary>
        /// <param name="bufferSize">Minimum buffer size to rent.</param>
        /// <returns>The rented buffer.</returns>
        byte[] Rent(int bufferSize);

#pragma warning disable CA1716 // Identifiers should not match keywords
        /// <summary>
        /// Return a previously rented buffer.
        /// </summary>
        /// <param name="bufferArray">The buffer to return.</param>
        void Return(byte[] bufferArray);
#pragma warning restore CA1716 // Identifiers should not match keywords
    }
}
