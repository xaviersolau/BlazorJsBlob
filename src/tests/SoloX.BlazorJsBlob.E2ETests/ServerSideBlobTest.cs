// ----------------------------------------------------------------------
// <copyright file="ServerSideBlobTest.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Playwright;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace SoloX.BlazorJsBlob.E2ETests
{
    [Collection(PlaywrightFixture.PlaywrightCollection)]
    public class ServerSideBlobTest
    {
        private readonly PlaywrightFixture playwrightFixture;

        public ServerSideBlobTest(PlaywrightFixture playwrightFixture)
        {
            this.playwrightFixture = playwrightFixture;
        }

        [Theory]
        [InlineData(BrowserType.Chromium)]
        [InlineData(BrowserType.Firefox)]
        public async Task ItShouldCreateABlobAndSaveIt(BrowserType browserType)
        {
            var url = PlaywrightFixture.MakeUrl("localhost");

            using var host = new WebHostTestFactory<HostProgram>(url);

            host.CreateDefaultClient();

            await this.playwrightFixture.GotoPageAsync(
                url,
                async (page) =>
                {
                    // Click text=CreateBlob.
                    await page.Locator("text=CreateBlob").ClickAsync().ConfigureAwait(false);

                    var embed = page.Locator("embed");

                    await embed.WaitForAsync().ConfigureAwait(false);

                    var embedSrc = await embed.GetAttributeAsync("src").ConfigureAwait(false);

                    embedSrc.Should().StartWith("blob:");

                    var embedType = await embed.GetAttributeAsync("type").ConfigureAwait(false);

                    embedType.Should().Be("image/jpeg");

                    // Click text=Save Blob
                    var downloadedFile = await page.RunAndWaitForDownloadAsync(async () =>
                    {
                        await page.Locator("text=Save Blob").ClickAsync().ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    downloadedFile.Should().NotBeNull();
                    downloadedFile.SuggestedFilename.Should().StartWith("tropical-waterfall").And.EndWith(".jpg");

                    downloadedFile.Url.Should().Be(embedSrc);

                    var size = await GetDownloadedSize(downloadedFile).ConfigureAwait(false);
                    size.Should().Be(2959153);

                    // Click text=Delete Blob
                    await page.Locator("text=Delete Blob").ClickAsync().ConfigureAwait(false);

                    var strong = page.Locator("strong", new PageLocatorOptions { HasTextString = "No blob to display!" });

                    var count = await strong.CountAsync().ConfigureAwait(false);
                    count.Should().Be(1);
                },
                browserType).ConfigureAwait(false);
        }

        internal static async Task<int> GetDownloadedSize(IDownload downloadedFile)
        {
            var downloadedStream = await downloadedFile.CreateReadStreamAsync().ConfigureAwait(false);
            var size = await ComputeStreamSizeAsync(downloadedStream).ConfigureAwait(false);
            return size;
        }

        private static async Task<int> ComputeStreamSizeAsync(Stream stream)
        {
            var buffer = MemoryPool<byte>.Shared.Rent(1024);
            var total = 0;
            var size = 1;
            while (size > 0)
            {
                size = await stream.ReadAsync(buffer.Memory).ConfigureAwait(false);
                total += size;
            }

            return total;
        }
    }
}
