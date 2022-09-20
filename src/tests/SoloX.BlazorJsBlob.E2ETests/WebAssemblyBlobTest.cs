// ----------------------------------------------------------------------
// <copyright file="WebAssemblyBlobTest.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace SoloX.BlazorJsBlob.E2ETests
{
    [Collection(PlaywrightFixture.PlaywrightCollection)]
    public class WebAssemblyBlobTest
    {
        private readonly PlaywrightFixture playwrightFixture;

        public WebAssemblyBlobTest(PlaywrightFixture playwrightFixture)
        {
            this.playwrightFixture = playwrightFixture;
        }

        [Theory]
        [InlineData(Browser.Chromium)]
        [InlineData(Browser.Firefox)]
#if !DEBUG
        [InlineData(Browser.Webkit)]
#endif
        public async Task ItShouldCreateABlobAndSaveIt(Browser browser)
        {
            var url = PlaywrightFixture.MakeUrl("localhost");

            // Create the host factory with the App class as parameter and the url we are going to use.
            using var hostFactory = new WebTestingHostFactory<WebHostTestProgram>();

            hostFactory
                // Override host configuration to mock stuff if required.
                .WithWebHostBuilder(builder =>
                {
                    builder.UseUrls(url);
                    //builder.ConfigureServices(services =>
                    //{
                    //    services.AddTransient<IService, ServiceMock>();
                    //})
                    //.ConfigureAppConfiguration((app, conf) =>
                    //{
                    //    conf.AddJsonFile("appsettings.Test.json");
                    //});
                })
                // Create the host using the CreateDefaultClient method.
                .CreateDefaultClient();

            await this.playwrightFixture.GotoPageAsync(
                url,
                async (page) =>
                {

                    var waitForRequestFinishedTask = page.WaitForRequestFinishedAsync(new PageWaitForRequestFinishedOptions()
                    {
                        Predicate = request =>
                        {
                            return request.Url == $"{url}/tropical-waterfall.jpg";
                        },
                    });

                    // Click text=CreateBlob.
                    await page.Locator("text=CreateBlob").ClickAsync().ConfigureAwait(false);

                    // Wait for image loaded from host.
                    await waitForRequestFinishedTask.ConfigureAwait(false);

                    var embed = page.Locator("embed");

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

                    var size = await ServerSideBlobTest.GetDownloadedSize(downloadedFile).ConfigureAwait(false);
                    size.Should().Be(2959153);

                    // Click text=Delete Blob
                    await page.Locator("text=Delete Blob").ClickAsync().ConfigureAwait(false);

                    var strong = page.Locator("strong", new PageLocatorOptions { HasTextString = "No blob to display!" });

                    var count = await strong.CountAsync().ConfigureAwait(false);
                    count.Should().Be(1);
                },
                browser).ConfigureAwait(false);
        }
    }
}