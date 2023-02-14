// ----------------------------------------------------------------------
// <copyright file="BlobServiceTest.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;
using SoloX.BlazorJsBlob.Services.Impl;
using SoloX.CodeQuality.Test.Helpers.XUnit.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

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
            var buffer = new MemoryStream(bufferArray);
            await using var _ = buffer.ConfigureAwait(false);

            var jsObjectReferenceMock = new Mock<IJSObjectReference>();

            // Get the GUID used to identify the Blob buffer.
            var blobId = Guid.Empty;
            jsObjectReferenceMock
                .Setup(x => x.InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.CreateBuffer,
                    It.Is<object[]>(objs => objs.Length == 1)))
                .Callback<string, object[]>((_, args) =>
                {
                    blobId = Guid.Parse(args[0].ToString());
                });

            // Make a copy of what is actually sent to the Blob.
            var copiedContent = new List<byte>();

            jsObjectReferenceMock.Setup(x => x.InvokeAsync<IJSVoidResult>(
                AModuleStrategy<IJSObjectReference>.AddToBuffer, It.Is<object[]>(
                        objs => objs.Length == 3
                        && Guid.Parse(objs[0].ToString()) == blobId
                        && objs[1].GetType() == typeof(byte[])
                        && objs[2].GetType() == typeof(int))))
                .Callback<string, object[]>((_, args) =>
                {
                    var slice = (byte[])args[1];
                    var size = (int)args[2];

                    copiedContent.AddRange(slice.Take(size));
                });

            // Mock the CreateBlob returned Url.
            jsObjectReferenceMock
                .Setup(x => x.InvokeAsync<string>(
                    AModuleStrategy<IJSObjectReference>.CreateBlob,
                    It.Is<object[]>(objs => objs.Length == 2 && Guid.Parse(objs[0].ToString()) == blobId && type.Equals(objs[1]))))
                .ReturnsAsync(blobUrl);

            var service = SetupBlobService(sliceBufferSize, jsObjectReferenceMock);
            await using var _1 = service.ConfigureAwait(false);

            var blob = await service.CreateBlobAsync(buffer, type).ConfigureAwait(false);
            await using var _2 = blob.ConfigureAwait(false);

            // Make sure the buffer is created and a Guid has been defined
            blobId.Should().NotBe(Guid.Empty);

            // Make sure the resulting blob is registered with the right parameters.
            blob.Uri.ToString().Should().Be(blobUrl);
            blob.Type.Should().Be(type);

            // Make sure the slices are properly sent to the JS layer.
            jsObjectReferenceMock
                .Verify(x => x.InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.AddToBuffer,
                    It.Is<object[]>(
                        objs => objs.Length == 3
                        && Guid.Parse(objs[0].ToString()) == blobId
                        && objs[1].GetType() == typeof(byte[])
                        && sliceBufferSize == (int)objs[2])),
                    Times.Exactly(sliceCount));

            if (restSize > 0)
            {
                jsObjectReferenceMock
                    .Verify(x => x.InvokeAsync<IJSVoidResult>(
                        AModuleStrategy<IJSObjectReference>.AddToBuffer,
                        It.Is<object[]>(
                            objs => objs.Length == 3
                            && Guid.Parse(objs[0].ToString()) == blobId
                            && objs[1].GetType() == typeof(byte[])
                            && restSize == (int)objs[2])),
                        Times.Once());
            }

            jsObjectReferenceMock
                .Verify(x => x.InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.AddToBuffer,
                    It.IsAny<object[]>()),
                    Times.Exactly((restSize > 0) ? sliceCount + 1 : sliceCount));

            // Make sure the stream is correctly sent to the JS buffer.
            copiedContent.Count.Should().Be(bufferSize);
            copiedContent.Should().BeEquivalentTo(bufferArray);

            // Make sure the buffer has been delete
            jsObjectReferenceMock
                .Verify(x => x.InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.DeleteBuffer,
                    It.Is<object[]>(
                        objs => objs.Length == 1
                        && Guid.Parse(objs[0].ToString()) == blobId)),
                    Times.Once());
        }

        [Fact]
        public async Task ItShouldSaveAsJsBlobAsync()
        {
            var blobUrl = "blob://my.blob.url/";
            var type = "application/json";
            var fileName = "fileName.Json";

            // Setup the stream to write to the JS Blob.
            var bufferArray = SetupStreamcontent(1024);
            var buffer = new MemoryStream(bufferArray);
            await using var _1 = buffer.ConfigureAwait(false);

            var jsObjectReferenceMock = new Mock<IJSObjectReference>();

            var blobMock = new Mock<IBlob>();
            blobMock.SetupGet(b => b.Uri).Returns(new Uri(blobUrl));
            blobMock.SetupGet(b => b.Type).Returns(type);

            var service = SetupBlobService(1024, jsObjectReferenceMock);
            await using var _2 = service.ConfigureAwait(false);

            await service.SaveAsFileAsync(blobMock.Object, fileName).ConfigureAwait(false);

            jsObjectReferenceMock.Verify(x => x.InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.SaveBlobAsFile,
                    It.Is<object[]>(
                        objs => objs.Length == 2
                        && objs[0].ToString() == blobUrl
                        && objs[1].ToString() == fileName)),
                    Times.Once());
        }

        [Theory]
        [InlineData("http://my.url/fileName.Json", "fileName.Json", "fileName.Json")]
        [InlineData("http://my.url/fileName.Json", null, "fileName.Json")]
        [InlineData("http://my.url/fileName.Json?var1=1&var2=2", null, "fileName.Json")]
        [InlineData("fileName.Json", null, "fileName.Json")]
        public async Task ItShouldSaveAsUrlAsync(string url, string fileName, string expectedFileName)
        {
            var type = "application/json";

            var jsObjectReferenceMock = new Mock<IJSObjectReference>();

            var service = SetupBlobService(1024, jsObjectReferenceMock);
            await using var _ = service.ConfigureAwait(false);

            await service.SaveAsFileAsync(url, fileName).ConfigureAwait(false);

            jsObjectReferenceMock.Verify(x => x.InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.SaveBlobAsFile,
                    It.Is<object[]>(
                        objs => objs.Length == 2
                        && objs[0].ToString() == url
                        && objs[1].ToString() == expectedFileName)),
                    Times.Once());
        }

        [Fact]
        public async Task ItShouldDeleteJsBlobOnDisposeAsync()
        {
            var blobUrl = "blob://my.blob.url/";
            var type = "application/json";

            // Setup the stream to write to the JS Blob.
            var bufferArray = SetupStreamcontent(1024);
            var buffer = new MemoryStream(bufferArray);
            await using var _1 = buffer.ConfigureAwait(false);

            var jsObjectReferenceMock = new Mock<IJSObjectReference>();

            // Mock the CreateBlob returned Url.
            jsObjectReferenceMock
                .Setup(x => x.InvokeAsync<string>(
                    AModuleStrategy<IJSObjectReference>.CreateBlob,
                    It.Is<object[]>(objs => objs.Length == 2 && Guid.Parse(objs[0].ToString()) != Guid.Empty && type.Equals(objs[1]))))
                .ReturnsAsync(blobUrl);

            var service = SetupBlobService(1024, jsObjectReferenceMock);
            await using var _2 = service.ConfigureAwait(false);

            var blob = await service.CreateBlobAsync(buffer, type).ConfigureAwait(false);

            // Make sure the JS Blob has not been delete
            jsObjectReferenceMock
                .Verify(x => x.InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.DeleteBlob,
                    It.Is<object[]>(
                        objs => objs.Length == 1
                        && objs[0].ToString() == blobUrl)),
                    Times.Never());

            await blob.DisposeAsync().ConfigureAwait(false);

            // Make sure the JS Blob has been delete
            jsObjectReferenceMock
                .Verify(x => x.InvokeAsync<IJSVoidResult>(
                    AModuleStrategy<IJSObjectReference>.DeleteBlob,
                    It.Is<object[]>(
                        objs => objs.Length == 1
                        && objs[0].ToString() == blobUrl)),
                    Times.Once());
        }

        private BlobService SetupBlobService(int sliceBufferSize, Mock<IJSObjectReference> jsObjectReferenceMock)
        {
            var bufferService = new Mock<IBufferService>();
            bufferService.Setup(s => s.Rent(It.IsAny<int>())).Returns<int>(s => new byte[s]);

            var jsRuntimeMock = new Mock<IJSRuntime>();
            jsRuntimeMock
                .Setup(x => x.InvokeAsync<IJSObjectReference>(BlobService.Import, new[] { BlobService.BlobManagerJsInteropFile }))
                .ReturnsAsync(jsObjectReferenceMock.Object);

            var options = new BlobServiceOptions
            {
                SliceBufferSize = sliceBufferSize,
            };

            var optionsMock = new Mock<IOptions<BlobServiceOptions>>();

            optionsMock.SetupGet(o => o.Value).Returns(options);
            var service = new BlobService(optionsMock.Object, jsRuntimeMock.Object, bufferService.Object, Logger);
            return service;
        }

        private static byte[] SetupStreamcontent(int bufferSize)
        {
            var bufferArray = new byte[bufferSize];

            for (var i = 0; i < bufferSize; i++)
            {
#pragma warning disable CA5394 // Do not use insecure randomness
                bufferArray[i] = (byte)Random.Shared.Next(255);
#pragma warning restore CA5394 // Do not use insecure randomness
            }

            return bufferArray;
        }
    }
}