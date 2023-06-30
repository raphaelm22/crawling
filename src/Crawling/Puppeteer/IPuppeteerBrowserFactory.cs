using PuppeteerSharp;

namespace Crawling.Puppeteer
{
    internal interface IPuppeteerBrowserFactory
    {
        Task<IBrowser> CreateAsync();
    }
}