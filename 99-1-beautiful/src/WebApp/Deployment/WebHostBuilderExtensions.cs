using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Deployment
{
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder WithWebHostBuilder(
            this IWebHostBuilder builder,
            Func<IHostingEnvironment, bool> isEnvironmentSupported,
            Action<IWebHostBuilder> configure)
        {
            if (builder == null) {throw new ArgumentNullException(nameof(builder));}

            if (isEnvironmentSupported == null)
            {
                throw new ArgumentNullException(nameof(isEnvironmentSupported));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            EnvironmentAwareWebHostBuilder environmentAwareBuilder =
                builder.BeginEnvironment(isEnvironmentSupported);
            configure(environmentAwareBuilder);
            return environmentAwareBuilder.EndEnvironment();
        }

        public static IWebHostBuilder UseEnvironmentAwareStartup(
            this IWebHostBuilder builder,
            params (Func<IHostingEnvironment, bool> isEnvironmentSupported, Func<IServiceProvider, IEnvironmentSpecificStartup> environmentStartupFactory)[] startupConfigs)
        {
            if (builder == null) { throw new ArgumentNullException(nameof(builder)); }
            ValidateStartupConfigs(startupConfigs);

            builder.ConfigureServices((c, s) =>
            {
                foreach (var startupConfig in startupConfigs)
                {
                    if (startupConfig.isEnvironmentSupported(c.HostingEnvironment))
                    {
                        s.AddSingleton(startupConfig.environmentStartupFactory);
                    }
                }
            }).UseStartup<EnvironmentAwareStartup>();
            
            return builder;
        }

        static void ValidateStartupConfigs(
            (
                Func<IHostingEnvironment, bool> isEnvironmentSupported,
                Func<IServiceProvider, IEnvironmentSpecificStartup> environmentStartupFactory
            )[] startupConfigs)
        {
            if (startupConfigs == null) { throw new ArgumentNullException(nameof(startupConfigs)); }

            if (startupConfigs.Any(startupConfig =>
                startupConfig.isEnvironmentSupported == null || startupConfig.environmentStartupFactory == null))
            {
                throw new ArgumentException(
                    $"The environment specific startup must provide environment prediction and factory method.");
            }
        }

        static EnvironmentAwareWebHostBuilder BeginEnvironment(
            this IWebHostBuilder builder,
            Func<IHostingEnvironment, bool> isEnvironmentSupported)
        {
            if (builder == null) {throw new ArgumentNullException(nameof(builder));}

            if (isEnvironmentSupported == null)
            {
                throw new ArgumentNullException(nameof(isEnvironmentSupported));
            }

            return new DelegatedWebHostBuilder(builder, isEnvironmentSupported);
        }

        static IWebHostBuilder EndEnvironment(
            this IWebHostBuilder webHostBuilder)
        {
            EnvironmentAwareWebHostBuilder environmentAwareWebHostBuilder =
                ToEnvironmentAwareWebHostBuilder(webHostBuilder);
            return environmentAwareWebHostBuilder.UnderlyingBuilder;
        }

        static EnvironmentAwareWebHostBuilder ToEnvironmentAwareWebHostBuilder(
            IWebHostBuilder webHostBuilder)
        {
            var environmentAwareWebHostBuilder = webHostBuilder as EnvironmentAwareWebHostBuilder;

            if (environmentAwareWebHostBuilder == null)
            {
                throw new ArgumentException(
                    $"The {nameof(IWebHostBuilder)} is not environment aware.");
            }

            return environmentAwareWebHostBuilder;
        }
    }
}