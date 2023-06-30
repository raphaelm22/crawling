using Crawling.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Crawling
{
    internal class Runner
    {
        readonly IMediator _mediator;
        readonly INotifier _notification;
        readonly ILogger _logger;

        public Runner(IMediator mediator, INotifier notification, ILogger<Runner> logger)
        {
            _mediator = mediator;
            _notification = notification;
            _logger = logger;
        }

        public async Task RunAsync(string[] args, CancellationToken cancellationToken)
        {
            var message = args switch
            {
                [var name, var productId] when name == nameof(Crawlers.GrowthSupplements) =>
                    await _mediator.Send(new Crawlers.GrowthSupplements.Command(productId), cancellationToken),

                _ => InvalidArgs()
            };

            if (message != null)
                await _notification.SendAsync(message, cancellationToken);
        }

        NotificationMessage? InvalidArgs()
        {
            _logger.LogError("No Crawlers was found");
            return null;
        }
    }
}
