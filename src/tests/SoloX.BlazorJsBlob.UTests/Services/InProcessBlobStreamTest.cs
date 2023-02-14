// ----------------------------------------------------------------------
// <copyright file="InProcessBlobStreamTest.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;
using SoloX.BlazorJsBlob.Services.Impl;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace SoloX.BlazorJsBlob.UTests.Services
{
    public class InProcessBlobStreamTest
    {
        [Theory]
        [InlineData(50, 50)]
        [InlineData(100, 50)]
        [InlineData(50, 150)]
        [InlineData(50, 55)]
        [InlineData(50, 123)]
        [InlineData(50, 149)]
        public async Task ItShouldCopyASimpleStream(int sliceBufferSize, int bufferSize)
        {
            var source = new byte[bufferSize];

            for (var i = 0; i < bufferSize; i++)
            {
                source[i] = (byte)i;
            }

            var bufferService = new BufferService();

            var jsReferenceMock = new Mock<IJSInProcessObjectReference>();

            var id = "abc123";

            var memStream = new MemoryStream();

            jsReferenceMock.Setup(x => x.Invoke<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.AddToBuffer, It.Is<object?[]?>(x => x.Length == 3)))
                .Callback<string, object?[]?>((funcName, args) =>
                {
                    memStream.Write((byte[])args[1], 0, (int)args[2]);
                });

#pragma warning disable CA2000 // Dispose objects before losing scope
            var stream = new InProcessModuleStrategy.InProcessBlobStream(bufferService, sliceBufferSize, jsReferenceMock.Object, id);
#pragma warning restore CA2000 // Dispose objects before losing scope
            await using (stream.ConfigureAwait(false))
            {
                await stream.WriteAsync(source).ConfigureAwait(false);
            }

            memStream.Position = 0;
            memStream.ToArray().Should().BeEquivalentTo(source);
        }
    }
}
