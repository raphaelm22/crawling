using Crawling.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Crawling
{
    internal static class Setup
    {
        public static Runner Perform()
        {
            var services = new ServiceCollection();
            Configure(services);

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<Runner>();
        }

        static void Configure(ServiceCollection services)
        {
            var configurations = ConfigureOptions();

            services.AddOptions();
            AddLogging(services);
            services.AddMediatR(option => option.RegisterServicesFromAssembly(typeof(Setup).Assembly));
            services.TryAddSingleton<Runner>();

            AddPuppeteer(services, configurations);
            AddTelegramNotifier(services, configurations);
        }

        private static void AddLogging(ServiceCollection services)
        {
            services.AddLogging(options =>
            {
                options
                    .AddFilter("Default", LogLevel.Warning)
                    .AddFilter("Crawling", LogLevel.Information)
                    .AddSimpleConsole(consoleOptions => consoleOptions.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ");
            });
        }

        static IConfiguration ConfigureOptions()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.local.json", optional: false);

            return builder.Build();
        }

        static void AddTelegramNotifier(ServiceCollection services, IConfiguration configurations)
        {
            services.TryAddScoped<INotifier, Notifications.TelegramNotification.Notifier>();

            services.TryAddSingleton(configurations
                .GetSection("Notifier:Telegram")
                .Get<Notifications.TelegramNotification.Options>()
                ?? throw new Exception("Could not create a Telegran Options")
            );
        }

        static void AddPuppeteer(ServiceCollection services, IConfiguration configurations)
        {
            services.TryAddScoped<Puppeteer.IPuppeteerBrowserFactory, Puppeteer.PuppeteerBrowserFactory>();

            services.TryAddSingleton(configurations
                .GetSection("PuppeteerBrowser")
                .Get<Puppeteer.Options>() ?? new Puppeteer.Options()
            );
        }
    }
}
