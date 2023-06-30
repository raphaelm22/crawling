using Crawling.Notifications;
using MediatR;

namespace Crawling.Crawlers.GrowthSupplements
{
    internal record Command(string ProductId) : IRequest<NotificationMessage?>
    {
    }
}
