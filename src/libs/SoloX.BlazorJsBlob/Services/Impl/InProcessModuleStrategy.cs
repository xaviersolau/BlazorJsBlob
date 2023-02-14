// ----------------------------------------------------------------------
// <copyright file="InProcessModuleStrategy.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SoloX.BlazorJsBlob.Services.Impl
{
    /// <summary>
    /// ModuleStrategy to handle WASM.
    /// </summary>
    public class InProcessModuleStrategy : AModuleStrategy<IJSInProcessObjectReference>
    {
        /// <summary>
        /// Setup instance.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="options"></param>
        /// <param name="bufferService"></param>
        /// <param name="logger"></param>
        public InProcessModuleStrategy(IJSInProcessObjectReference module, BlobServiceOptions options, IBufferService bufferService, ILogger<BlobService> logger)
            : base(module, options, bufferService, logger)
        {
        }

        /// <inheritdoc/>
        protected override Stream CreateStream(IBufferService bufferService, int sliceBufferSize, string bufferGuid)
        {
            return new InProcessBlobStream(bufferService, sliceBufferSize, Module, bufferGuid);
        }

        internal class InProcessBlobStream : Stream
        {
            private readonly IJSInProcessObjectReference module;
            private readonly IBufferService bufferService;
            private readonly string bufferGuid;

            private byte[] bufferArray;
            private Memory<byte> bufferMemory;

            private int bufferSize;
            private int bufferOffset;
            private long length;

            public InProcessBlobStream(IBufferService bufferService, int bufferSize, IJSInProcessObjectReference module, string bufferGuid)
            {
                this.module = module;
                this.bufferService = bufferService;

                this.bufferGuid = bufferGuid;

                this.bufferArray = this.bufferService.Rent(bufferSize);
                this.bufferSize = this.bufferArray.Length;

                this.bufferMemory = new Memory<byte>(this.bufferArray);
            }

            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => this.length;

            public override long Position { get => this.length; set => throw new NotSupportedException(); }

            public override void Flush()
            {
                if (this.bufferOffset > 0)
                {
                    this.module.InvokeVoid(AddToBuffer, this.bufferGuid, this.bufferArray, this.bufferOffset);

                    this.bufferOffset = 0;
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                this.length += count;

                while (count > 0)
                {
                    var chunkSize = this.bufferSize - this.bufferOffset;

                    if (count >= chunkSize)
                    {
                        buffer.AsMemory().Slice(offset, chunkSize).CopyTo(this.bufferMemory.Slice(this.bufferOffset));

                        this.module.InvokeVoid(AddToBuffer, this.bufferGuid, this.bufferArray, this.bufferSize);

                        offset += chunkSize;
                        count -= chunkSize;
                        this.bufferOffset = 0;
                    }
                    else
                    {
                        buffer.AsMemory().Slice(offset, count).CopyTo(this.bufferMemory.Slice(this.bufferOffset));

                        this.bufferOffset += count;
                        count = 0;
                    }
                }
            }

            public override async ValueTask DisposeAsync()
            {
                await FlushAsync().ConfigureAwait(false);

                this.bufferMemory = null;

                this.bufferService.Return(this.bufferArray);

                this.bufferArray = null;
                this.bufferSize = 0;
                this.bufferOffset = 0;

                await base.DisposeAsync().ConfigureAwait(false);

                GC.SuppressFinalize(this);
            }
        }
    }
}