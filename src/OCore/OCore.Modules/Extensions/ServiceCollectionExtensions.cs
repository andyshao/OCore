using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using OCore.Environment.Extensions;
using OCore.Environment.Shell;
using OCore.Environment.Shell.Descriptor.Models;
using OCore.Modules;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds OrchardCore services to the host service collection.
        /// </summary>
        public static OCoreBuilder AddOrchardCore(this IServiceCollection services) {
            // If an instance of OrchardCoreBuilder exists reuse it,
            // so we can call AddOrchardCore several times.
            var builder = services
                .LastOrDefault(d => d.ServiceType == typeof(OCoreBuilder))?
                .ImplementationInstance as OCoreBuilder;

            if (builder == null) {
                builder = new OCoreBuilder(services);
                services.AddSingleton(builder);

                AddDefaultServices(services);
                AddShellServices(services);
                AddExtensionServices(builder);

                AddStaticFiles(builder);

                AddAntiForgery(builder);
                AddAuthentication(builder);
                AddDataProtection(builder);

                // Register the list of services to be resolved later on
                services.AddSingleton(services);
            }

            return builder;
        }

        private static void AddDefaultServices(IServiceCollection services) {
            services.AddLogging();
            services.AddOptions();

            // These services might be moved at a higher level if no components from OrchardCore needs them.
            services.AddLocalization();
            services.AddWebEncoders();

            // ModularTenantRouterMiddleware which is configured with UseModules() calls UseRouter() which requires the routing services to be
            // registered. This is also called by AddMvcCore() but some applications that do not enlist into MVC will need it too.
            services.AddRouting();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IClock, Clock>();
            services.AddScoped<ILocalClock, LocalClock>();

            services.AddSingleton<IPoweredByMiddlewareOptions, PoweredByMiddlewareOptions>();
            services.AddTransient<IModularTenantRouteBuilder, ModularTenantRouteBuilder>();
        }

        private static void AddShellServices(IServiceCollection services) {
            // Use a single tenant and all features by default
            services.AddHostingShellServices();
            services.AddAllFeaturesDescriptor();

            // Registers the application main feature
            services.AddTransient(sp => new ShellFeature
            (
                sp.GetRequiredService<IHostingEnvironment>().ApplicationName, alwaysEnabled: true)
            );
        }

        private static void AddExtensionServices(OCoreBuilder builder) {
            builder.ApplicationServices.AddExtensionManagerHost();

            builder.ConfigureServices(services => {
                services.AddExtensionManager();
            });
        }

        /// <summary>
        /// Adds tenant level configuration to serve static files from modules
        /// </summary>
        private static void AddStaticFiles(OCoreBuilder builder) {
            builder.Configure((app, routes, serviceProvider) => {
                var env = serviceProvider.GetRequiredService<IHostingEnvironment>();

                IFileProvider fileProvider;
                if (env.IsDevelopment()) {
                    var fileProviders = new List<IFileProvider>();
                    fileProviders.Add(new ModuleProjectStaticFileProvider(env));
                    fileProviders.Add(new ModuleEmbeddedStaticFileProvider(env));
                    fileProvider = new CompositeFileProvider(fileProviders);
                } else {
                    fileProvider = new ModuleEmbeddedStaticFileProvider(env);
                }

                app.UseStaticFiles(new StaticFileOptions {
                    RequestPath = "",
                    FileProvider = fileProvider
                });
            });
        }

        /// <summary>
        /// Adds host and tenant level antiforgery services.
        /// </summary>
        private static void AddAntiForgery(OCoreBuilder builder) {
            builder.ApplicationServices.AddAntiforgery();

            builder.ConfigureServices((services, serviceProvider) => {
                var settings = serviceProvider.GetRequiredService<ShellSettings>();

                var tenantName = settings.Name;
                var tenantPrefix = "/" + settings.RequestUrlPrefix;

                services.AddAntiforgery(options => {
                    options.Cookie.Name = "orchantiforgery_" + tenantName;
                    options.Cookie.Path = tenantPrefix;
                });
            });
        }

        /// <summary>
        /// Adds host and tenant level authentication services and configuration.
        /// </summary>
        private static void AddAuthentication(OCoreBuilder builder) {
            builder.ApplicationServices.AddAuthentication();

            builder.ConfigureServices(services => {
                services.AddAuthentication();

                // IAuthenticationSchemeProvider is already registered at the host level.
                // We need to register it again so it is taken into account at the tenant level
                // because it holds a reference to an underlying dictionary, responsible of storing 
                // the registered schemes which need to be distinct for each tenant.
                services.AddSingleton<IAuthenticationSchemeProvider, AuthenticationSchemeProvider>();

            })
            .Configure(app => {
                app.UseAuthentication();
            });
        }

        /// <summary>
        /// Adds tenant level data protection services.
        /// </summary>
        private static void AddDataProtection(OCoreBuilder builder) {
            builder.ConfigureServices((services, serviceProvider) => {
                var settings = serviceProvider.GetRequiredService<ShellSettings>();
                var options = serviceProvider.GetRequiredService<IOptions<ShellOptions>>();

                var directory = Directory.CreateDirectory(Path.Combine(
                options.Value.ShellsApplicationDataPath,
                options.Value.ShellsContainerName,
                settings.Name, "DataProtection-Keys"));

                // Re-register the data protection services to be tenant-aware so that modules that internally
                // rely on IDataProtector/IDataProtectionProvider automatically get an isolated instance that
                // manages its own key ring and doesn't allow decrypting payloads encrypted by another tenant.
                // By default, the key ring is stored in the tenant directory of the configured App_Data path.
                services.Add(new ServiceCollection()
                    .AddDataProtection()
                    .PersistKeysToFileSystem(directory)
                    .SetApplicationName(settings.Name)
                    .Services);
            });
        }

        ///// <summary>
        ///// ���ģ�����<see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>.
        ///// </summary>
        //public static IServiceCollection AddModules(this IServiceCollection services, Action<IServiceCollection> configure = null)
        //{
        //    services.AddWebHost();
        //    services.AddManifestDefinition("module");

        //    // ModularTenantRouterMiddleware which is configured with UseModules() calls UserRouter() which requires the routing services to be
        //    // registered. This is also called by AddMvcCore() but some applications that do not enlist into MVC will need it too.
        //    services.AddRouting();

        //    // Use a single tenant and all features by defaultĬ�������ʹ�õ����⻧������ӵ��ȫ������
        //    services.AddAllFeaturesDescriptor();

        //    // Let the app change the default tenant behavior and set of features ��Ӧ�������ø���Ĭ���⻧�͹���
        //    configure?.Invoke(services);

        //    // Registers the application feature
        //    services.AddTransient(sp =>
        //    {
        //        return new ShellFeature(sp.GetRequiredService<IHostingEnvironment>().ApplicationName);
        //    });

        //    // Register the list of services to be resolved later on ע���Ժ�Ҫ����ķ����б�
        //    services.AddSingleton(_ => services);

        //    return services;
        //}

        /////// <summary>
        /////// ע��ģ����Ҫʹ�õ������ļ�ѡ��
        /////// </summary>
        ////public static ModularServiceCollection WithConfiguration(this ModularServiceCollection modules, IConfiguration configuration)
        ////{
        ////    // Register the configuration object for modules to register options with it
        ////    if (configuration != null)
        ////    {
        ////        modules.Configure(services => services.AddSingleton<IConfiguration>(configuration));
        ////    }

        ////    return modules;
        ////}

        ///// <summary>
        ///// Registers a default tenant with a set of features that are used to setup and configure the actual tenants.
        ///// For instance you can use this to add a custom Setup module.
        ///// ��Ĭ���⻧ע��һ���������ú�����ʵ���⻧�Ĺ��ܡ�
        ///// ���磬������ʹ����������Զ��尲װģ�顣
        ///// </summary>
        //public static IServiceCollection WithDefaultFeatures(this IServiceCollection services, params string[] featureIds)
        //{
        //    foreach (var featureId in featureIds)
        //    {
        //        services.AddTransient(sp => new ShellFeature(featureId));
        //    }

        //    return services;
        //}

        ///// <summary>
        ///// Registers tenants defined in configuration.
        ///// ע�������ж�����⻧����tenants.json�����ò������⻧����ӵ�еĹ��ܡ� 
        ///// </summary>
        //public static IServiceCollection WithTenants(this IServiceCollection services)
        //{
        //    services.AddSingleton<IShellSettingsConfigurationProvider, FileShellSettingsConfigurationProvider>();
        //    services.AddScoped<IShellDescriptorManager, FileShellDescriptorManager>();
        //    services.AddSingleton<IShellSettingsManager, ShellSettingsManager>();
        //    services.AddScoped<ShellSettingsWithTenants>();

        //    return services;
        //}

        ///// <summary>
        ///// Registers a single tenant with the specified set of features.
        ///// ע�����ָ�����ܵĵ����⻧��
        ///// </summary>
        //public static IServiceCollection WithFeatures(this IServiceCollection services,params string[] featureIds)
        //{
        //    services.WithDefaultFeatures(featureIds);
        //    services.AddSetFeaturesDescriptor();

        //    return services;
        //}

        //public static IServiceCollection AddWebHost(
        //    this IServiceCollection services)
        //{
        //    services.AddLogging();
        //    services.AddOptions();
        //    services.AddLocalization();
        //    services.AddHostingShellServices();
        //    services.AddExtensionManagerHost();
        //    services.AddWebEncoders();

        //    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        //    services.AddSingleton<IClock, Clock>();

        //    services.AddSingleton<IPoweredByMiddlewareOptions, PoweredByMiddlewareOptions>();
        //    services.AddScoped<IModularTenantRouteBuilder, ModularTenantRouteBuilder>();

        //    return services;
        //}
    }
}
