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
            Func<string[], object?>[] commandFactories = [
                Crawlers.GrowthSupplements.Command.Create
            ];

            var commands = commandFactories
                .Select(factory => factory.Invoke(args))
                .Where(command => command != null)
                .ToList();

            if (commands.Count == 0)
            {
                _logger.LogError("No Crawlers was found");
                return;
            }

            foreach (var command in commands)
            {
                var result = await _mediator.Send(command!, cancellationToken);
                if (result is not null && result is NotificationMessage message)
                    await _notification.SendAsync(message, cancellationToken);
            }
        }
    }
}
