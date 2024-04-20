using Crawling.Notifications;
using Crawling.Puppeteer;
using MediatR;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace Crawling.Crawlers.Caesb
{
    internal sealed class Handler(ILogger<Handler> logger, IPuppeteerBrowserFactory puppeteerBrowserFactory) : IRequestHandler<Command, NotificationMessage?>
    {
        const string URL = "https://www.caesb.df.gov.br/portal-servicos/app/publico/consultarfaltadagua";

        readonly ILogger _logger = logger;
        readonly IPuppeteerBrowserFactory _puppeteerBrowserFactory = puppeteerBrowserFactory;

        public async Task<NotificationMessage?> Handle(Command request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting crawling on Caesb to find {Neighborhood}", request.NeighborhoodName);

            await using var browser = await _puppeteerBrowserFactory.CreateAsync();

            using var page = await browser.NewPageAsync();
            await page.GoToAsync(URL, TimeSpan.FromSeconds(60).Milliseconds, [WaitUntilNavigation.DOMContentLoaded]);

            var neighborhoodInfo = await TryGetNeighborhoodInfo(page, request.NeighborhoodName);
            if (neighborhoodInfo.Count > 0)
            {
                return CreateNotification(neighborhoodInfo);
            }

            return null;
        }

        async Task<List<string>> TryGetNeighborhoodInfo(IPage page, string neighborhoodName)
        {
            try
            {
                await page.WaitForSelectorAsync("form#formFaltaDeAgua table tbody tr");

                var neighborhoodElement = (await page.EvaluateExpressionAsync<IEnumerable<string>>(
                    $"""
                    [...(
                        Array.from(document.querySelectorAll('td'))
                            .find(el => el.textContent === '{neighborhoodName}')?.parentElement?.getElementsByTagName("td") || []
                        )
                    ].map(el => el.innerText) 
                    """
                ))
                .ToList();

                if (neighborhoodElement.Count == 0)
                {
                    _logger.LogError("Nothing about {neighborhood} was found.", neighborhoodName);
                }

                return neighborhoodElement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while tried to find '{neighborhoodName}'.", neighborhoodName);
                return [];
            }
        }

        NotificationMessage? CreateNotification(List<string> neighborhoodInfo)
        {
            if (neighborhoodInfo.Count == 6)
            {
                var message = $"Lack of water in: '{neighborhoodInfo[0]}'. \n\n" +
                    $"Area: '{neighborhoodInfo[1]}'. \n\n" +
                    $"Between: '{neighborhoodInfo[2]}' and '{neighborhoodInfo[3]}'. \n\n" +
                    $"Reason: '{neighborhoodInfo[4]} - {neighborhoodInfo[5]}'";

                return new NotificationMessage(message);
            }

            return new NotificationMessage(string.Join("\n", neighborhoodInfo));
        }
    }
}
