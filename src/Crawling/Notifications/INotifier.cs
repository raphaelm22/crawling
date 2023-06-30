namespace Crawling.Notifications
{
    internal interface INotifier
    {
        Task SendAsync(NotificationMessage message, CancellationToken cancellationToken);
    }
}
