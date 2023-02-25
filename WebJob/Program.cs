using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using TableauRestApiLib;
using TableauSyncWebJob.Models;

namespace TableauSyncWebJob
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                IServiceCollection services = new ServiceCollection();
                ConfigureServices(services);
                var serviceProvider = services.BuildServiceProvider();

                //Run the entry point.
                var entryPoint = serviceProvider.GetService<TableauSyncJob>();
                await entryPoint.RunAsync();
                await Task.Delay(3000);   //adding delay for logging to complete 

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred --{ex.StackTrace}");
            }
           
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            Console.WriteLine($"Current Environment : {(string.IsNullOrEmpty(environment) ? "Development" : environment)}");

            //Configuraion
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            
            //Logging
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(config.GetSection("Logging"));
                loggingBuilder.AddConsole();
            });

            //Dependency Injection
            services.Configure<TableauConfigSettings>(config.GetSection("TableauConfigSettings"));
            services.Configure<SyncSettings>(config.GetSection("SyncSettings"));
            services.AddTransient<IRestApiService,RestApiService>();
            services.AddTransient<TableauSyncJob>();
            services.AddTransient<IDisDataProvider, DisDataProvider>();
            services.AddSingleton(config);     // Add access to generic IConfigurationRoot
        }
    }
}
