using Crawling.Notifications;
using MediatR;

namespace Crawling.Crawlers.Caesb
{
    internal record Command(string NeighborhoodName) : IRequest<NotificationMessage?>
    {
        public static object? Create(string[] args) => args switch
        {
            [var crawller, var neighborhoodName] when crawller.Equals(nameof(Caesb), StringComparison.OrdinalIgnoreCase) => new Command(neighborhoodName),
            _ => null
        };
    }
}
