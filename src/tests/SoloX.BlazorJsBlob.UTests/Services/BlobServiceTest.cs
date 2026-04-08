// ----------------------------------------------------------------------
// <copyright file="BlobServiceTest.cs" company="Xavier Solau">
// Copyright © 2022-2026 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using Shouldly;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using NSubstitute;
using SoloX.BlazorJsBlob.Services;
using SoloX.BlazorJsBlob.Services.Impl;
using SoloX.CodeQuality.Test.Helpers.XUnit.V3.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SoloX.BlazorJsBlob.UTests
{
    public class BlobServiceTest
    {
        private ILogger<BlobService> Logger { get; }

        public BlobServiceTest(ITestOutputHelper testOutputHelper)
        {
            Logger = new TestLogger<BlobService>(testOutputHelper);
        }

        [Theory]
        [InlineData(1, 1024, 123)]
        [InlineData(2, 1024, 123)]
        [InlineData(3, 1024, 123)]
        [InlineData(2, 514, 123)]
        [InlineData(6, 514, 0)]
        [InlineData(1, 514, 0)]
        [InlineData(1, 514, 1)]
        [InlineData(0, 1024, 0)]
        [InlineData(0, 1024, 123)]
        public async Task ItShouldCreateABlobFromAGivenStream(int sliceCount, int sliceBufferSize, int restSize)
        {
            var blobUrl = "blob://my.blob.url/";
            var type = "application/json";

            // Define bufferSize with the number of slice and a rest.
            var bufferSize = sliceBufferSize * sliceCount + restSize;

            // Setup the stream to write to the JS Blob.
            var bufferArray = SetupStreamcontent(bufferSize);
            var buffer = new MemoryStream(bufferArray.ToArray());
            await using var _ = buffer.ConfigureAwait(false);

            var jsObjectReferenceMock = Substitute.For<IJSObjectReference>();

            // Get the GUID used to identify the Blob buffer.
            var blobId = Guid.Empty;

            jsObjectReferenceMock
                .When(x => x.InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.CreateBuffer,
                    Arg.Is<object[]>(objs => objs.Length == 1)))
                .Do(ci =>
                {
                    var args = ci.Arg<object[]>();
                    blobId = Guid.Parse(args[0].ToString());
                });

            // Make a copy of what is actually sent to the Blob.
            var copiedContent = new List<byte>();

            jsObjectReferenceMock.When(x => x.InvokeAsync<IJSVoidResult>(
                AModuleStrategy<IJSObjectReference>.AddToBuffer, Arg.Is<object[]>(
                        objs => objs.Length == 3
                        && Guid.Parse(objs[0].ToString()) == blobId
                        && objs[1].GetType() == typeof(byte[])
                        && objs[2].GetType() == typeof(int))))
                .Do(ci =>
                {
                    var args = ci.Arg<object[]>();

                    var slice = (byte[])args[1];
                    var size = (int)args[2];

                    copiedContent.AddRange(slice.Take(size));
                });

            // Mock the CreateBlob returned Url.
            jsObjectReferenceMock.InvokeAsync<string>(
                    AModuleStrategy<IJSObjectReference>.CreateBlob,
                    Arg.Is<object[]>(objs => objs.Length == 2 && Guid.Parse(objs[0].ToString()) == blobId && type.Equals(objs[1])))
                .Returns(blobUrl);

            var service = SetupBlobService(sliceBufferSize, jsObjectReferenceMock);
            await using var _1 = service.ConfigureAwait(false);

            var blob = await service.CreateBlobAsync(buffer, type);
            await using var _2 = blob.ConfigureAwait(false);

            // Make sure the buffer is created and a Guid has been defined
            blobId.ShouldNotBe(Guid.Empty);

            // Make sure the resulting blob is registered with the right parameters.
            blob.Uri.ToString().ShouldBe(blobUrl);
            blob.Type.ShouldBe(type);

            // Make sure the slices are properly sent to the JS layer.
            await jsObjectReferenceMock
                .Received(sliceCount)
                .InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.AddToBuffer,
                    Arg.Is<object[]>(
                        objs => objs.Length == 3
                        && Guid.Parse(objs[0].ToString()) == blobId
                        && objs[1].GetType() == typeof(byte[])
                        && sliceBufferSize == (int)objs[2]));

            if (restSize > 0)
            {
                await jsObjectReferenceMock
                    .Received()
                    .InvokeAsync<IJSVoidResult>(
                        AModuleStrategy<IJSObjectReference>.AddToBuffer,
                        Arg.Is<object[]>(
                            objs => objs.Length == 3
                            && Guid.Parse(objs[0].ToString()) == blobId
                            && objs[1].GetType() == typeof(byte[])
                            && restSize == (int)objs[2]));
            }

            await jsObjectReferenceMock
                .Received((restSize > 0) ? sliceCount + 1 : sliceCount)
                .InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.AddToBuffer,
                    Arg.Any<object[]>());

            // Make sure the stream is correctly sent to the JS buffer.
            copiedContent.Count.ShouldBe(bufferSize);
            copiedContent.ShouldBeEquivalentTo(bufferArray);

            // Make sure the buffer has been delete
            await jsObjectReferenceMock
                .Received()
                .InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.DeleteBuffer,
                    Arg.Is<object[]>(
                        objs => objs.Length == 1
                        && Guid.Parse(objs[0].ToString()) == blobId));
        }

        [Fact]
        public async Task ItShouldSaveAsJsBlobAsync()
        {
            var blobUrl = "blob://my.blob.url/";
            var type = "application/json";
            var fileName = "fileName.Json";

            // Setup the stream to write to the JS Blob.
            var bufferArray = SetupStreamcontent(1024);
            var buffer = new MemoryStream(bufferArray.ToArray());
            await using var _1 = buffer.ConfigureAwait(false);

            var jsObjectReferenceMock = Substitute.For<IJSObjectReference>();

            var blobMock = Substitute.For<IBlob>();

            blobMock.Uri.Returns(new Uri(blobUrl));
            blobMock.Type.Returns(type);

            var service = SetupBlobService(1024, jsObjectReferenceMock);
            await using var _2 = service.ConfigureAwait(false);

            await service.SaveAsFileAsync(blobMock, fileName);

            await jsObjectReferenceMock
                .Received()
                .InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.SaveBlobAsFile,
                    Arg.Is<object[]>(
                        objs => objs.Length == 2
                        && objs[0].ToString() == blobUrl
                        && objs[1].ToString() == fileName));
        }

        [Theory]
        [InlineData("http://my.url/fileName.Json", "fileName.Json", "fileName.Json")]
        [InlineData("http://my.url/fileName.Json", null, "fileName.Json")]
        [InlineData("http://my.url/fileName.Json?var1=1&var2=2", null, "fileName.Json")]
        [InlineData("fileName.Json", null, "fileName.Json")]
        public async Task ItShouldSaveAsUrlAsync(string url, string? fileName, string expectedFileName)
        {
            var type = "application/json";

            var jsObjectReferenceMock = Substitute.For<IJSObjectReference>();

            var service = SetupBlobService(1024, jsObjectReferenceMock);
            await using var _ = service.ConfigureAwait(false);

            await service.SaveAsFileAsync(url, fileName);

            await jsObjectReferenceMock
                .Received()
                .InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.SaveBlobAsFile,
                    Arg.Is<object[]>(
                        objs => objs.Length == 2
                        && objs[0].ToString() == url
                        && objs[1].ToString() == expectedFileName));
        }

        [Fact]
        public async Task ItShouldDeleteJsBlobOnDisposeAsync()
        {
            var blobUrl = "blob://my.blob.url/";
            var type = "application/json";

            // Setup the stream to write to the JS Blob.
            var bufferArray = SetupStreamcontent(1024);
            var buffer = new MemoryStream(bufferArray.ToArray());
            await using var _1 = buffer.ConfigureAwait(false);

            var jsObjectReferenceMock = Substitute.For<IJSObjectReference>();

            // Mock the CreateBlob returned Url.
            jsObjectReferenceMock
                .InvokeAsync<string>(
                    AModuleStrategy<IJSObjectReference>.CreateBlob,
                    Arg.Is<object[]>(objs => objs.Length == 2 && Guid.Parse(objs[0].ToString()) != Guid.Empty && type.Equals(objs[1])))
                .Returns(blobUrl);

            var service = SetupBlobService(1024, jsObjectReferenceMock);
            await using var _2 = service.ConfigureAwait(false);

            var blob = await service.CreateBlobAsync(buffer, type);

            // Make sure the JS Blob has not been delete
            await jsObjectReferenceMock
                .Received(0)
                .InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.DeleteBlob,
                    Arg.Is<object[]>(
                        objs => objs.Length == 1
                        && objs[0].ToString() == blobUrl));

            await blob.DisposeAsync();

            // Make sure the JS Blob has been delete
            await jsObjectReferenceMock
                .Received()
                .InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.DeleteBlob,
                    Arg.Is<object[]>(
                        objs => objs.Length == 1
                        && objs[0].ToString() == blobUrl));
        }

        private BlobService SetupBlobService(int sliceBufferSize, IJSObjectReference jsObjectReferenceMock)
        {
            var bufferService = Substitute.For<IBufferService>();
            bufferService.Rent(Arg.Any<int>())
                .Returns(ci => new byte[ci.Arg<int>()]);

            var jsRuntimeMock = Substitute.For<IJSRuntime>();
            jsRuntimeMock
                .InvokeAsync<IJSObjectReference>(
                    BlobService.Import,
                    Arg.Is<object?[]?>(args => args != null && BlobService.BlobManagerJsInteropFile.Equals(args.SingleOrDefault())))
                .Returns(jsObjectReferenceMock);

            var options = new BlobServiceOptions
            {
                SliceBufferSize = sliceBufferSize,
            };

            var optionsMock = Substitute.For<IOptions<BlobServiceOptions>>();

            optionsMock.Value.Returns(options);
            var service = new BlobService(optionsMock, jsRuntimeMock, bufferService, Logger);
            return service;
        }

        private static List<byte> SetupStreamcontent(int bufferSize)
        {
            var bufferArray = new byte[bufferSize];

            for (var i = 0; i < bufferSize; i++)
            {
#pragma warning disable CA5394 // Do not use insecure randomness
                bufferArray[i] = (byte)Random.Shared.Next(255);
#pragma warning restore CA5394 // Do not use insecure randomness
            }

            return bufferArray.ToList();
        }
    }
}