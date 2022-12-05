using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextTechEvent.Data;
using System.Security.Cryptography.X509Certificates;
using NextTechEvent.Function;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(s => {
        s.AddHttpClient<ConferenceFunctions>();
        s.AddSingleton<IDocumentStore>(ctx =>
        {
            var store = new DocumentStore
            {
                Urls = new string[] { "https://a.free.caw.ravendb.cloud" },
                Database = "NextTechEvent",
                Certificate = new X509Certificate2(Convert.FromBase64String(Environment.GetEnvironmentVariable("RavenCert")), Environment.GetEnvironmentVariable("RavenPassword"))
            };

            store.Initialize();
            store.TimeSeries.Register<Conference, WeatherData>();
            return store;
        });
    })
    
    .Build();

host.Run();
