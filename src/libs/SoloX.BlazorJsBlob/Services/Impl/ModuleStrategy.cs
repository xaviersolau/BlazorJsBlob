// ----------------------------------------------------------------------
// <copyright file="ModuleStrategy.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SoloX.BlazorJsBlob.Services.Impl
{
    /// <summary>
    /// ModuleStrategy to handle server side.
    /// </summary>
    public class ModuleStrategy : AModuleStrategy<IJSObjectReference>
    {
        /// <summary>
        /// Setup instance.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="options"></param>
        /// <param name="bufferService"></param>
        /// <param name="logger"></param>
        public ModuleStrategy(IJSObjectReference module, BlobServiceOptions options, IBufferService bufferService, ILogger<BlobService> logger)
            : base(module, options, bufferService, logger)
        {
        }

        /// <inheritdoc/>
        protected override Stream CreateStream(IBufferService bufferService, int sliceBufferSize, string bufferGuid)
        {
            return new BlobStream(bufferService, sliceBufferSize, Module, bufferGuid);
        }

        internal class BlobStream : Stream
        {
            private readonly IBufferService bufferService;
            private readonly int sliceBufferSize;
            private readonly IJSObjectReference module;
            private readonly string bufferGuid;

            private byte[] bufferArray;
            private Memory<byte> bufferMemory;

            private int bufferSize;
            private int bufferOffset;

            private long length;

            private Task asyncTask;
            private readonly Queue<(byte[] Buffer, int Size)> buffers = new Queue<(byte[], int)>();

            public BlobStream(IBufferService bufferService, int sliceBufferSize, IJSObjectReference module, string bufferGuid)
            {
                this.bufferService = bufferService;
                this.module = module;
                this.bufferGuid = bufferGuid;
                this.sliceBufferSize = sliceBufferSize;

                this.bufferArray = this.bufferService.Rent(sliceBufferSize);
                this.bufferSize = this.bufferArray.Length;

                this.bufferMemory = new Memory<byte>(this.bufferArray);

                this.asyncTask = Task.CompletedTask;
            }

            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => this.length;

            public override long Position { get => this.length; set => throw new NotSupportedException(); }

            public override async Task FlushAsync(CancellationToken cancellationToken)
            {
                if (this.bufferOffset > 0)
                {
                    this.buffers.Enqueue((this.bufferArray, this.bufferOffset));

                    this.bufferOffset = 0;

                    this.asyncTask = ProcessAsyncTaskAsync(this.asyncTask);
                }

                await this.asyncTask.ConfigureAwait(false);
            }

            public override void Flush()
            {
                throw new NotSupportedException();
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

                        this.buffers.Enqueue((this.bufferArray, this.bufferSize));

                        if (this.asyncTask.IsCompleted)
                        {
                            this.asyncTask = ProcessAsyncTaskAsync(this.asyncTask);
                        }

                        this.bufferArray = this.bufferService.Rent(this.sliceBufferSize);
                        this.bufferSize = this.bufferArray.Length;
                        this.bufferMemory = new Memory<byte>(this.bufferArray);

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

            private async Task ProcessAsyncTaskAsync(Task asyncTask)
            {
                await asyncTask.ConfigureAwait(false);

                while (this.buffers.TryDequeue(out var item))
                {
                    await this.module.InvokeVoidAsync(AddToBuffer, this.bufferGuid, item.Buffer, item.Size).ConfigureAwait(false);

                    this.bufferService.Return(item.Buffer);
                }
            }

            public override async ValueTask DisposeAsync()
            {
                await FlushAsync().ConfigureAwait(false);

                this.bufferMemory = null;

                this.bufferArray = null;
                this.bufferSize = 0;
                this.bufferOffset = 0;

                this.buffers.Clear();

                await base.DisposeAsync().ConfigureAwait(false);

                GC.SuppressFinalize(this);
            }
        }
    }
}