// ----------------------------------------------------------------------
// <copyright file="BlobStreamTest.cs" company="Xavier Solau">
// Copyright © 2022-2026 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using Shouldly;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using NSubstitute;
using SoloX.BlazorJsBlob.Services.Impl;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace SoloX.BlazorJsBlob.UTests.Services
{
    public class BlobStreamTest
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

            var jsReferenceMock = Substitute.For<IJSObjectReference>();

            var id = "abc123";


            var memStream = new MemoryStream();

            jsReferenceMock.When(x => x.InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.AddToBuffer, Arg.Is<object?[]?>(x => x.Length == 3)))
                .Do(ci =>
                {
                    var funcName = ci.Arg<string>();
                    var args = ci.Arg<object?[]?>();
                    memStream.Write((byte[])args[1], 0, (int)args[2]);
                });

#pragma warning disable CA2000 // Dispose objects before losing scope
            var stream = new ModuleStrategy.BlobStream(bufferService, sliceBufferSize, jsReferenceMock, id);
#pragma warning restore CA2000 // Dispose objects before losing scope
            await using (stream.ConfigureAwait(false))
            {
                await stream.WriteAsync(source);
            }

            memStream.Position = 0;
            memStream.ToArray().ShouldBeEquivalentTo(source);
        }
    }
}
