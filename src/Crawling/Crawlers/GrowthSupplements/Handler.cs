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

            using var page = await browser.NewPageAsync();
            var url = $"{BASE_URL}/{request.ProductId}";
            await page.GoToAsync(url, TimeSpan.FromSeconds(60).Milliseconds, [WaitUntilNavigation.DOMContentLoaded]);

            string title = await TryGetProductTitle(page, request.ProductId);

            if (await ProductIsAvailableAsync(page))
            {
                _logger.LogInformation("Product {productId} is available.", request.ProductId);

                if (request.Sku != null)
                {
                    _logger.LogInformation("Checking {sku} if is available....", request.Sku);
                    if (await SkuIsAvailableAsync(page, request.Sku))
                    {
                        _logger.LogInformation("SKU {sku} is available.", request.Sku);
                        return new NotificationMessage($"'{title}' - '{request.Sku}' is available. {url}");
                    }
                    else return null;
                }
                
                return new NotificationMessage($"'{title}' is available. {url}"); 
            }

            if (!await EnsuresProductIdUnavailableAsync(page))
            {
                _logger.LogWarning("Impossible to know whether the product '{productId}' is available. Maybe DOM changed?.", request.ProductId);
                return new NotificationMessage($"Impossible to know whether the product '{title}' is available. Maybe DOM changed?. {url}");
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

        async Task<bool> ProductIsAvailableAsync(IPage page)
        {
            return await page.QuerySelectorAsync(".botaoComprar") != null;
        }

        async Task<bool> SkuIsAvailableAsync(IPage page, string sku)
        {
            var optionsElements = await page.QuerySelectorAllAsync("option:not([disabled])");
            foreach (var optionsElement in optionsElements)
            {
                var text = await page.EvaluateFunctionAsync<string>("(elem) => elem.innerText", optionsElement);
                if (text.Equals(sku, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        async Task<bool> EnsuresProductIdUnavailableAsync(IPage page)
        {
            return await page.QuerySelectorAsync(".btIndisponivel") != null;
        }
    }
}
