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
            _logger.LogInformation("Starting crawling on Caesb to find {district}", request.DistrictName);

            await using var browser = await _puppeteerBrowserFactory.CreateAsync();

            using var page = await browser.NewPageAsync();
            await page.GoToAsync(URL, TimeSpan.FromSeconds(60).Milliseconds, [WaitUntilNavigation.DOMContentLoaded]);
            await page.WaitForSelectorAsync("form[id='tabView:formFaltaDeAgua']", new() { Timeout = 30_000, Visible = true });

            var districtInfo = await TryGetDistrictInfo(page, request.DistrictName);
            if (districtInfo.Count > 0)
            {
                return CreateNotification(districtInfo);
            }

            return null;
        }

        async Task<List<string>> TryGetDistrictInfo(IPage page, string districtName)
        {
            try
            {
                await page.WaitForSelectorAsync("form[id='tabView:formFaltaDeAgua'] table tbody tr");

                var districtElement = (await page.EvaluateExpressionAsync<IEnumerable<string>>(
                    $"""
                    [...(
                        Array.from(document.querySelectorAll('td'))
                            .find(el => el.textContent.toLowerCase().includes('{districtName}'.toLowerCase()))?.parentElement?.getElementsByTagName("td") || []
                        )
                    ].map(el => el.innerText) 
                    """
                ))
                .ToList();

                if (districtElement.Count == 0)
                {
                    _logger.LogError("Nothing about {district} was found.", districtName);
                }

                return districtElement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while tried to find '{districtName}'.", districtName);
                return [];
            }
        }

        NotificationMessage? CreateNotification(List<string> districtInfo)
        {
            if (districtInfo.Count == 6)
            {
                var message = $"Lack of water in: '{districtInfo[0]}'. \n\n" +
                    $"Area: '{districtInfo[1]}'. \n\n" +
                    $"Between: '{districtInfo[2]}' and '{districtInfo[3]}'. \n\n" +
                    $"Reason: '{districtInfo[4]} - {districtInfo[5]}'";

                return new NotificationMessage(message);
            }

            return new NotificationMessage(string.Join("\n", districtInfo));
        }
    }
}
