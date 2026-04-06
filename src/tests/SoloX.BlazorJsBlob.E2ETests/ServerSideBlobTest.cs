// ----------------------------------------------------------------------
// <copyright file="ServerSideBlobTest.cs" company="Xavier Solau">
// Copyright © 2022-2026 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Playwright;
using SoloX.BlazorJsBlob.Example.ServerSide;
using SoloX.CodeQuality.Playwright;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace SoloX.BlazorJsBlob.E2ETests
{
    public class ServerSideBlobTest
    {
        private readonly IPlaywrightTestBuilder builder;

        public ServerSideBlobTest()
        {
            this.builder = PlaywrightTestBuilder.Create()
                .WithLocalHost(localHostBuilder =>
                {
                    localHostBuilder
                        .UsePortRange(new PortRange(5000, 6000))
                        .UseApplication<App>();
                });
        }

        [Theory]
        [InlineData(Browser.Chromium)]
        [InlineData(Browser.Firefox)]
        [InlineData(Browser.Webkit)]
        public async Task ItShouldCreateABlobAndSaveIt(Browser browser)
        {
            var playwrightTest = await this.builder
                .BuildAsync(browser)
                .ConfigureAwait(true);

            await using var _ = playwrightTest.ConfigureAwait(false);

            await playwrightTest.GotoPageAsync(
                string.Empty,
                async (page) =>
                {
                    // Click text=Create Blob.
                    await page.Locator("text=Create Blob").ClickAsync().ConfigureAwait(false);

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

                    await strong.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible }).ConfigureAwait(false);
                });
        }

        [Theory]
        [InlineData(Browser.Chromium)]
        [InlineData(Browser.Firefox)]
        [InlineData(Browser.Webkit)]
        public async Task ItShouldSaveUrlAsFile(Browser browser)
        {
            var playwrightTest = await this.builder
                //.WithPlaywrightOptions(opt =>
                //{
                //    // Display the browser screen.
                //    opt.Headless = false;
                //})
                .BuildAsync(browser)
                .ConfigureAwait(true);

            await using var _ = playwrightTest.ConfigureAwait(false);

            await playwrightTest.GotoPageAsync(
                string.Empty,
                async (page) =>
                {
                    // Click text=Save Url.
                    var downloadedFile = await page.RunAndWaitForDownloadAsync(async () =>
                    {
                        await page.Locator("text=Download Url").ClickAsync().ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    downloadedFile.Should().NotBeNull();
                    downloadedFile.SuggestedFilename.Should().StartWith("tropical-waterfall").And.EndWith(".jpg");

                    var size = await GetDownloadedSize(downloadedFile).ConfigureAwait(false);
                    size.Should().Be(2959153);
                });
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
