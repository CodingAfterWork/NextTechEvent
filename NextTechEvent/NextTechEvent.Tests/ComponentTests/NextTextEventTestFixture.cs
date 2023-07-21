using NextTechEvent.Components;
using NextTechEvent.Data;
using NextTechEvent.Data.Index;
using Raven.Client.Documents;
using Raven.TestDriver;
using System;
using System.Threading.Tasks;

namespace NextTechEvent.Tests.ComponentTests;

public class NextTextEventTestFixture : RavenTestDriver, IAsyncLifetime
{

    public TestContext Context { get; private set; }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddTransient((services) =>
        {
            TestContext testContext = new();
            testContext.Services.AddScoped<INextTechEventApi, NextTechEventApi>();
            testContext.Services.AddSingleton(ctx =>
            {
                var store = GetDocumentStore(); //Gets a test store
                store.Initialize();
                return store;
            });
            testContext.Services.AddHttpClient();

            return testContext;
        });


        var context = services.BuildServiceProvider().GetService<TestContext>();
        if (context == null)
        {
            throw new Exception("Could not create TestContext");
        }
        Context = context!;

        //Fill the database with data
        var store = context.Services.GetRequiredService<IDocumentStore>();
        store.TimeSeries.Register<Conference, WeatherData>();
        await store.ExecuteIndexAsync(new ConferencesByWeather());
        new ConferenceCountByDates().Execute(store);
        await store.ExecuteIndexAsync(new ConferenceBySearchTerm());
        //Fill with test data
        var session = store.OpenAsyncSession();
        for (int a = 0; a < 100; a++)
        {
            var conference = new Conference()
            {
                Name = $"Test {a}",
                EventStart = DateOnly.FromDateTime(DateTime.Now.AddDays(a)),
                EventEnd = DateOnly.FromDateTime(DateTime.Now.AddDays(a + 1)),
            };
            await session.StoreAsync(conference);
        }
        await session.SaveChangesAsync();

        WaitForIndexing(store);
        WaitForUserToContinueTheTest(store);

    }
}
