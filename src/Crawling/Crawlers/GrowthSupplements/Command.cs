using Crawling.Notifications;
using MediatR;

namespace Crawling.Crawlers.GrowthSupplements
{
    internal record Command(string ProductId, string? Sku) : IRequest<NotificationMessage?>
    {
        public static object? Create(string[] args)
        {
            if (args.Length > 1 && args[0].Equals(nameof(GrowthSupplements), StringComparison.OrdinalIgnoreCase))
                return new Command(args[1], args.Skip(2).FirstOrDefault());

            return null;
        }
    }
}
