// ----------------------------------------------------------------------
// <copyright file="PlaywrightFixture.cs" company="Xavier Solau">
// Copyright © 2022 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Playwright;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace SoloX.BlazorJsBlob.E2ETests
{
    /// <summary>
    /// Browser types we can use in the PlaywrightFixture.
    /// </summary>
    public enum Browser
    {
        Chromium,
        Firefox,
        Webkit,
    }

    /// <summary>
    /// Playwright fixture implementing an asynchronous life cycle.
    /// </summary>
    public class PlaywrightFixture : IAsyncLifetime
    {
        /// <summary>
        /// Playwright module.
        /// </summary>
        public IPlaywright Playwright { get; private set; }

        /// <summary>
        /// Chromium lazy initializer.
        /// </summary>
        public Lazy<Task<IBrowser>> ChromiumBrowser { get; private set; }
        /// <summary>
        /// Firefox lazy initializer.
        /// </summary>
        public Lazy<Task<IBrowser>> FirefoxBrowser { get; private set; }
        /// <summary>
        /// Webkit lazy initializer.
        /// </summary>
        public Lazy<Task<IBrowser>> WebkitBrowser { get; private set; }

        /// <summary>
        /// Initialize the Playwright fixture.
        /// </summary>
        /// <returns>The initialization task.</returns>
        public async Task InitializeAsync()
        {
            InstallPlaywright();

            var options = new BrowserTypeLaunchOptions
            {
                //Headless = false,
                //SlowMo = 5000,
                //Timeout = 60000,
            };

            // Create Playwright module.
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);

            // Setup Browser lazy initializers.
            ChromiumBrowser = new Lazy<Task<IBrowser>>(Playwright.Chromium.LaunchAsync(options));
            FirefoxBrowser = new Lazy<Task<IBrowser>>(Playwright.Firefox.LaunchAsync(options));
            WebkitBrowser = new Lazy<Task<IBrowser>>(Playwright.Webkit.LaunchAsync(options));
        }

        /// <summary>
        /// Install and deploy all binaries Playwright may need.
        /// </summary>
        private static void InstallPlaywright()
        {
            var exitCode = Microsoft.Playwright.Program.Main(new[] { "install-deps" });
            if (exitCode != 0)
            {
#pragma warning disable CA2201 // Do not raise reserved exception types
                throw new Exception($"Playwright exited with code {exitCode} on install-deps");
#pragma warning restore CA2201 // Do not raise reserved exception types
            }
            exitCode = Microsoft.Playwright.Program.Main(new[] { "install" });
            if (exitCode != 0)
            {
#pragma warning disable CA2201 // Do not raise reserved exception types
                throw new Exception($"Playwright exited with code {exitCode} on install");
#pragma warning restore CA2201 // Do not raise reserved exception types
            }
        }

        /// <summary>
        /// Open a Browser page and navigate to the given URL before applying the given test handler.
        /// </summary>
        /// <param name="url">URL to navigate to.</param>
        /// <param name="testHandler">Test handler to apply on the page.</param>
        /// <param name="browserType">The Browser to use to open the page.</param>
        /// <param name="retryCount">retry count in case of PlaywrightException.</param>
        /// <returns>The GotoPage task.</returns>
        public async Task GotoPageAsync(string url, Func<IPage, Task> testHandler, Browser browserType = Browser.Chromium, int retryCount = 5)
        {
            var stackTrace = new StackTrace();

            // since this is async/await frames, we must take the 7th
            var frame = stackTrace.GetFrame(7);

            var traceName = $"trace_{frame.GetMethod().DeclaringType.Name}_{frame.GetMethod().Name}_{Guid.NewGuid()}";

            var retry = retryCount;
            while (retry > 0)
            {
                try
                {
                    await GotoPageInternalAsync(url, testHandler, browserType, traceName).ConfigureAwait(false);
                    retry = 0;
                }
                catch (PlaywrightException)
                {
                    retry--;
                    if (retry == 0)
                    {
                        throw;
                    }
                }
            }
        }

        private async Task GotoPageInternalAsync(string url, Func<IPage, Task> testHandler, Browser browserType, string traceName)
        {
            var browser = await SelectBrowserAsync(browserType).ConfigureAwait(false);
            await using var context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true }).ConfigureAwait(false);

            // Start tracing before creating the page.
            await context.Tracing.StartAsync(new TracingStartOptions()
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            }).ConfigureAwait(false);

            var page = await context.NewPageAsync().ConfigureAwait(false);

            page.Should().NotBeNull();

            try
            {
                var gotoResult = await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 }).ConfigureAwait(false);
                gotoResult.Should().NotBeNull();

                await gotoResult.FinishedAsync().ConfigureAwait(false);

                gotoResult.Ok.Should().BeTrue();

                await testHandler(page).ConfigureAwait(false);
            }
            finally
            {
                await page.CloseAsync().ConfigureAwait(false);

                // Stop tracing and save data into a zip archive.
                await context.Tracing.StopAsync(new TracingStopOptions()
                {
                    Path = traceName + ".zip"
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Select the IBrowser instance depending on the given browser enumeration value.
        /// </summary>
        /// <param name="browser">The browser to select.</param>
        /// <returns>The selected IBrowser instance.</returns>
        /// <exception cref="NotImplementedException"></exception>
        private Task<IBrowser> SelectBrowserAsync(Browser browser)
        {
            return browser switch
            {
                Browser.Chromium => ChromiumBrowser.Value,
                Browser.Firefox => FirefoxBrowser.Value,
                Browser.Webkit => WebkitBrowser.Value,
                _ => throw new NotImplementedException(),
            };
        }

        public static string MakeUrl(string hostName, bool isHttps = true, int? port = null)
        {
#pragma warning disable CA5394 // Do not use insecure randomness
            if (!port.HasValue)
            {
                port = 5000;
            }

            var http = isHttps ? "https" : "http";
            return $"{http}://{hostName}:{port}";
#pragma warning restore CA5394 // Do not use insecure randomness
        }

        /// <summary>
        /// Dispose all Playwright module resources.
        /// </summary>
        /// <returns>The disposal task.</returns>
        public async Task DisposeAsync()
        {
            if (Playwright != null)
            {
                if (ChromiumBrowser != null && ChromiumBrowser.IsValueCreated)
                {
                    var browser = await ChromiumBrowser.Value.ConfigureAwait(false);
                    await browser.DisposeAsync().ConfigureAwait(false);
                }
                if (FirefoxBrowser != null && FirefoxBrowser.IsValueCreated)
                {
                    var browser = await FirefoxBrowser.Value.ConfigureAwait(false);
                    await browser.DisposeAsync().ConfigureAwait(false);
                }
                if (WebkitBrowser != null && WebkitBrowser.IsValueCreated)
                {
                    var browser = await WebkitBrowser.Value.ConfigureAwait(false);
                    await browser.DisposeAsync().ConfigureAwait(false);
                }

                Playwright.Dispose();
                Playwright = null;
            }
        }

        /// <summary>
        /// PlaywrightCollection name that is used in the Collection attribute on each test classes.
        /// Like "[Collection(PlaywrightFixture.PlaywrightCollection)]"
        /// </summary>
        public const string PlaywrightCollection = nameof(PlaywrightCollection);

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
#pragma warning disable CA1034 // Nested types should not be visible
        [CollectionDefinition(PlaywrightCollection)]
        public class PlaywrightCollectionDefinition : ICollectionFixture<PlaywrightFixture>
        {
            // This class is just xUnit plumbing code to apply [CollectionDefinition] and
            // the ICollectionFixture<> interfaces. Witch in our case is parametrized
            // with the PlaywrightFixture
        }
#pragma warning restore CA1034 // Nested types should not be visible
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
    }
}
