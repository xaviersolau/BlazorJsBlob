// ----------------------------------------------------------------------
// <copyright file="WebAssemblyBlobTest.cs" company="Xavier Solau">
// Copyright © 2022-2026 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using Shouldly;
using Microsoft.Playwright;
using SoloX.CodeQuality.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace SoloX.BlazorJsBlob.E2ETests
{
    public class WebAssemblyBlobTest
    {
        private readonly IPlaywrightTestBuilder builder;

        public WebAssemblyBlobTest()
        {
            this.builder = PlaywrightTestBuilder.Create()
                .WithLocalHost(localHostBuilder =>
                {
                    localHostBuilder
                        .UsePortRange(new PortRange(5000, 6000))
                        .UseApplication<WebHostTestProgram>();
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
                    var waitForRequestFinishedTask = page.WaitForRequestFinishedAsync(new PageWaitForRequestFinishedOptions()
                    {
                        Predicate = request =>
                        {
                            return request.Url == $"{playwrightTest.Url}/tropical-waterfall.jpg";
                        },
                    });

                    // Click text=Create Blob.
                    await page.Locator("text=Create Blob").ClickAsync().ConfigureAwait(false);

                    // Wait for image loaded from host.
                    await waitForRequestFinishedTask.ConfigureAwait(false);

                    var embed = page.Locator("embed");

                    var embedSrc = await embed.GetAttributeAsync("src").ConfigureAwait(false);

                    embedSrc.ShouldStartWith("blob:");

                    var embedType = await embed.GetAttributeAsync("type").ConfigureAwait(false);

                    embedType.ShouldBe("image/jpeg");

                    // Click text=Save Blob
                    var downloadedFile = await page.RunAndWaitForDownloadAsync(async () =>
                    {
                        await page.Locator("text=Save Blob").ClickAsync().ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    downloadedFile.ShouldNotBeNull();
                    downloadedFile.SuggestedFilename
                        .ShouldSatisfyAllConditions(
                            fn => fn.ShouldStartWith("tropical-waterfall"),
                            fn => fn.ShouldEndWith(".jpg"));

                    downloadedFile.Url.ShouldBe(embedSrc);

                    var size = await ServerSideBlobTest.GetDownloadedSize(downloadedFile).ConfigureAwait(false);
                    size.ShouldBe(2959153);

                    // Click text=Delete Blob
                    await page.Locator("text=Delete Blob").ClickAsync().ConfigureAwait(false);

                    var strong = page.Locator("strong", new PageLocatorOptions { HasTextString = "No blob to display!" });

                    var count = await strong.CountAsync().ConfigureAwait(false);
                    count.ShouldBe(1);
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

                    downloadedFile.ShouldNotBeNull();
                    downloadedFile.SuggestedFilename
                        .ShouldSatisfyAllConditions(
                            fn => fn.ShouldStartWith("tropical-waterfall"),
                            fn => fn.ShouldEndWith(".jpg"));

                    var size = await ServerSideBlobTest.GetDownloadedSize(downloadedFile).ConfigureAwait(false);
                    size.ShouldBe(2959153);
                });
        }
    }
}