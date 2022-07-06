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
using System.Threading.Tasks;
using Xunit;

namespace SoloX.BlazorJsBlob.E2ETests
{
    public enum BrowserType
    {
        Chromium,
        Firefox,
        Webkit,
    }


    public class PlaywrightFixture : IAsyncLifetime
    {
        public const string PlaywrightCollection = nameof(PlaywrightCollection);

        public IPlaywright Playwright { get; private set; }

        public Lazy<Task<IBrowser>> BrowserChromium { get; private set; }
        public Lazy<Task<IBrowser>> BrowserFirefox { get; private set; }

        public Lazy<Task<IBrowser>> BrowserWebkit { get; private set; }

        public async Task InitializeAsync()
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

            Playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);

            BrowserChromium = new Lazy<Task<IBrowser>>(Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                //Headless = false,
                //SlowMo = 5000,
            }));
            BrowserFirefox = new Lazy<Task<IBrowser>>(Playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
            {
                //Headless = false,
                //SlowMo = 5000,
            }));
            BrowserWebkit = new Lazy<Task<IBrowser>>(Playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions
            {
                //Headless = false,
                //SlowMo = 5000,
            }));
        }

        public async Task GotoPageAsync(string url, Func<IPage, Task> testHandler, BrowserType browserType = BrowserType.Chromium)
        {
            var browser = await SelectBrowserAsync(browserType).ConfigureAwait(false);

            var page = await browser.NewPageAsync(new BrowserNewPageOptions
            {
                IgnoreHTTPSErrors = true,
            }).ConfigureAwait(false);

            page.Should().NotBeNull();

            try
            {
                var gotoResult = await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle }).ConfigureAwait(false);
                gotoResult.Should().NotBeNull();

                await gotoResult.FinishedAsync().ConfigureAwait(false);

                gotoResult.Ok.Should().BeTrue();

                await testHandler(page).ConfigureAwait(false);
            }
            finally
            {
                await page.CloseAsync().ConfigureAwait(false);
            }
        }

        private Task<IBrowser> SelectBrowserAsync(BrowserType browserType)
        {
            return browserType switch
            {
                BrowserType.Chromium => BrowserChromium.Value,
                BrowserType.Firefox => BrowserFirefox.Value,
                BrowserType.Webkit => BrowserWebkit.Value,
                _ => throw new NotImplementedException(),
            };
        }

        public static string MakeUrl(string hostName, bool isHttps = true, int? port = null)
        {
#pragma warning disable CA5394 // Do not use insecure randomness
            if (!port.HasValue)
            {
                port = Random.Shared.Next(5000) + 5000;
            }

            var http = isHttps ? "https" : "http";
            return $"{http}://{hostName}:{port}";
#pragma warning restore CA5394 // Do not use insecure randomness
        }

        public async Task DisposeAsync()
        {
            if (Playwright != null)
            {
                if (BrowserChromium != null && BrowserChromium.IsValueCreated)
                {
                    var browser = await BrowserChromium.Value.ConfigureAwait(false);
                    await browser.DisposeAsync().ConfigureAwait(false);
                }
                if (BrowserChromium != null && BrowserChromium.IsValueCreated)
                {
                    var browser = await BrowserChromium.Value.ConfigureAwait(false);
                    await browser.DisposeAsync().ConfigureAwait(false);
                }
                if (BrowserWebkit != null && BrowserWebkit.IsValueCreated)
                {
                    var browser = await BrowserWebkit.Value.ConfigureAwait(false);
                    await browser.DisposeAsync().ConfigureAwait(false);
                }

                Playwright.Dispose();
                Playwright = null;
            }
        }
    }

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    [CollectionDefinition(PlaywrightFixture.PlaywrightCollection)]
    public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
}
