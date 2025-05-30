using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace Crawling.Notifications.TelegramNotification
{
    internal class Notifier : INotifier
    {
        readonly Options _options;
        readonly ILogger _logger;

        public Notifier(Options options, ILogger<Notifier> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sending message: {message}", message.Text);

            var bot = new TelegramBotClient(_options.Token);
            await bot.SendMessage(_options.ChatId, message.Text, cancellationToken: cancellationToken);
        }
    }
}
