using Crawling.Notifications;
using Crawling.Puppeteer;
using MediatR;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace Crawling.Crawlers.GrowthSupplements
{
    internal sealed class Handler : IRequestHandler<Command, NotificationMessage?>
    {
        const string BASE_URL = "https://www.gsuplementos.com.br";

        readonly ILogger _logger;
        readonly IPuppeteerBrowserFactory _puppeteerBrowserFactory;

        public Handler(ILogger<Handler> logger, IPuppeteerBrowserFactory puppeteerBrowserFactory)
        {
            _logger = logger;
            _puppeteerBrowserFactory = puppeteerBrowserFactory;
        }


        public async Task<NotificationMessage?> Handle(Command request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting crawling the product {productid}", request.ProductId);

            await using var browser = await _puppeteerBrowserFactory.CreateAsync();

            var page = await browser.NewPageAsync();
            var url = $"{BASE_URL}/{request.ProductId}";
            await page.GoToAsync(url, TimeSpan.FromSeconds(60).Milliseconds, new[] { WaitUntilNavigation.DOMContentLoaded });

            if (await page.QuerySelectorAsync(".botaoComprar") != null)
            {
                _logger.LogInformation("Product {productId} is available.", request.ProductId);

                string title = await TryGetProductTitle(page, request.ProductId);
                return new NotificationMessage($"'{title}' is available. {url}");
            }

            _logger.LogInformation("Product {productId} is unavailable.", request.ProductId);
            return null;
        }

        async Task<string> TryGetProductTitle(IPage page, string defaultValue)
        {
            try
            {
                var titleElement = await page.QuerySelectorAsync(".topoDetalhe-boxRight-nome");
                if (titleElement == null)
                {
                    _logger.LogError("Product title not found.");
                    return defaultValue;
                }

                return await page.EvaluateFunctionAsync<string>("(elem) => elem.innerText", titleElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error accourred while accessing the product title.");
                return defaultValue;
            }
        }
    }
}
