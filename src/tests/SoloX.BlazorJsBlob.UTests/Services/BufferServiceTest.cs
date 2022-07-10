// ----------------------------------------------------------------------
// <copyright file="BufferServiceTest.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using FluentAssertions;
using SoloX.BlazorJsBlob.Services.Impl;
using Xunit;

namespace SoloX.BlazorJsBlob.UTests.Services
{
    public class BufferServiceTest
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(512)]
        [InlineData(2048)]
        [InlineData(16000)]
        public void ItShouldCreateBufferWithAGivenSize(int size)
        {
            var service = new BufferService();

            var buffer = service.Rent(size);

            buffer.Should().NotBeNull();
            buffer.Length.Should().BeGreaterThanOrEqualTo(size);

            service.Return(buffer);
        }
    }
}
