using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextTechEvent.Data;
using Raven.Client.Documents;
using System;
using System.Security.Cryptography.X509Certificates;

[assembly: FunctionsStartup(typeof(NextTechEvent.Function.Startup))]
namespace NextTechEvent.Function
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            //.AddJsonFile("appsettings.local.json", optional: false, reloadOnChange: true)
            .Build();

            builder.Services.AddSingleton<IDocumentStore>(ctx =>
            {
                var store = new DocumentStore
                {
                    Urls = new string[] { "https://a.free.caw.ravendb.cloud" },
                    Database = "NextTechEvent",
                    Certificate = new X509Certificate2(Convert.FromBase64String(config["RavenCert"]), config["RavenPassword"])
                };

                store.Initialize();
                store.TimeSeries.Register<Conference, WeatherData>("Weather");
                return store;
            });

        }
    }
}
