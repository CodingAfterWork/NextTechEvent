using Auth0.AspNetCore.Authentication;
using Blazm.Components;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using NextTechEvent.Client.Data;
using NextTechEvent.Client.Data.Index;
using NextTechEvent.Client.Pages;
using NextTechEvent.Components;
using NextTechEvent.Data;
using NextTechEvent.Data.Index;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddRazorComponents()
 .AddInteractiveServerComponents()
 .AddInteractiveWebAssemblyComponents();


builder.Services.AddScoped<INextTechEventApi, NextTechEventServerApi>();
builder.Services.AddHttpClient();
builder.Services.AddBlazm();
builder.Services.AddSingleton<IDocumentStore>(ctx =>
{
    var store = new DocumentStore
    {
        Urls = new[] { "https://a.free.caw.ravendb.cloud" },
        Database = "NextTechEvent",
        Certificate = new X509Certificate2(Convert.FromBase64String(builder.Configuration["RavenCert"]), builder.Configuration["RavenPassword"])
    };
    store.Initialize();
    store.TimeSeries.Register<Conference, WeatherData>();
    // Index creation must be synchronous here, so use .Execute instead of .ExecuteIndexAsync
    store.ExecuteIndex(new ConferencesByWeather());
    new ConferenceCountByDates().Execute(store);
    store.ExecuteIndex(new ConferenceBySearchTerm());
    store.ExecuteIndex(new ConferencesByDatesAndCounts());

    return store;
});

builder.Services
 .AddAuth0WebAppAuthentication(options =>
 {
     options.Domain = builder.Configuration["Auth0:Authority"];
     options.ClientId = builder.Configuration["Auth0:ClientId"];
 });

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorComponents<App>()
 .AddInteractiveServerRenderMode()
 .AddInteractiveWebAssemblyRenderMode()
 .AddAdditionalAssemblies(typeof(NextTechEvent.Client._Imports).Assembly);

app.MapGet("login", async (string redirectUri, HttpContext context) =>
{
    var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
    .WithRedirectUri(redirectUri)
    .Build();

    await context.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
});
app.MapGet("logout", async (HttpContext context) =>
{
    var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
    .WithRedirectUri("/")
    .Build();

    await context.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
});


app.MapGet("calendar/{id}", async (string id, INextTechEventApi api) =>
{
    var settings = await api.GetSettingsAsync(id);
    var calendar = await api.GetUserCalendarAsync(settings!.UserId);

    CalendarSerializer serializer = new();
    MemoryStream ms = new MemoryStream();
    serializer.Serialize(calendar, ms, System.Text.Encoding.UTF8);
    ms.Position = 0;
    return Results.Stream(ms, contentType: "text/calendar", "NextTechEvent.ics");

});

// Minimal APIs for INextTechEventApi
var apiGroup = app.MapGroup("/api");

// Conferences
apiGroup.MapGet("/conferences", async (INextTechEventApi api) => await api.GetConferencesAsync());


//apiGroup.MapPost("/conferences", async (Conference conference, INextTechEventApi api) =>
//{
// var saved = await api.SaveConferenceAsync(conference);
// return Results.Ok(saved);
//});
apiGroup.MapGet("/conferences/{*id}", async (string id, INextTechEventApi api) => await api.GetConferenceAsync(id));
apiGroup.MapGet("/conferences/count", async (INextTechEventApi api) => await api.GetConferenceCountsAsync());
apiGroup.MapGet("/conferences/range", async (DateOnly startdate, DateOnly enddate, INextTechEventApi api) => await api.GetConferencesAsync(startdate, enddate));
apiGroup.MapGet("/conferences/near", async (double latitude, double longitude, double radius, DateOnly startdate, DateOnly enddate, INextTechEventApi api) =>
 await api.GetConferencesAsync(latitude, longitude, radius, startdate, enddate));
apiGroup.MapGet("/conferences/search", async (string searchterm, INextTechEventApi api) => await api.SearchConferencesAsync(searchterm));
apiGroup.MapGet("/conferences/search-active", async (bool hasOpenCallforPaper, string? searchterm, int pagesize, int page, INextTechEventApi api) =>
 await api.SearchActiveConferencesAsync(hasOpenCallforPaper, searchterm ?? string.Empty, pagesize, page));
apiGroup.MapGet("/conferences/count-by-date", async (DateOnly start, DateOnly end, string? searchterm, INextTechEventApi api) =>
 await api.GetConferenceCountByDate(start, end, searchterm ?? string.Empty));
apiGroup.MapGet("/conferences/open-cfp", async (int startIndex, int count, INextTechEventApi api, CancellationToken ct) =>
 await api.GetConferencesWithOpenCfpAsync(new ItemsProviderRequest(startIndex, count, ct)));
//apiGroup.MapGet("/conferences/virtualized", async (int startIndex, int count, INextTechEventApi api, CancellationToken ct) =>
// await api.GetConferencesAsync(new ItemsProviderRequest(startIndex, count, ct)));
//apiGroup.MapGet("/conferences/by-user/{userId}", async (string userId, INextTechEventApi api) => await api.GetConferencesByUserIdAsync(userId));
apiGroup.MapGet("/conferences/by-weather", async (double averageTemp, INextTechEventApi api) => await api.GetConferencesByWeatherAsync(averageTemp));
apiGroup.MapGet("/conferences/{conferenceId}/weather-timeseries", async (string conferenceId, INextTechEventApi api) => await api.GetWeatherTimeSeriesAsync(conferenceId));

//// Statuses
//apiGroup.MapPost("/statuses", async (Status status, INextTechEventApi api) => Results.Ok(await api.SaveStatusAsync(status)));
//apiGroup.MapGet("/statuses/{conferenceId}/{userId}", async (string conferenceId, string userId, INextTechEventApi api) => await api.GetStatusAsync(conferenceId, userId));
//apiGroup.MapGet("/statuses/by-user/{userId}", async (string userId, INextTechEventApi api) => await api.GetStatusesAsync(userId));
//apiGroup.MapPost("/statuses/update-from-calendar", async (Settings settings, INextTechEventApi api) =>
//{
// await api.UpdateStatusBasedOnSessionizeCalendarAsync(settings);
// return Results.NoContent();
//});
//apiGroup.MapPost("/statuses/update-from-calendar/{settingsId}", async (string settingsId, INextTechEventApi api) =>
//{
// var settings = await api.GetSettingsAsync(settingsId);
// if (settings is null)
// return Results.NotFound();
// await api.UpdateStatusBasedOnSessionizeCalendarAsync(settings);
// return Results.NoContent();
//});

//// Settings
//apiGroup.MapPost("/settings", async (Settings settings, INextTechEventApi api) => Results.Ok(await api.SaveSettingsAsync(settings)));
//apiGroup.MapGet("/settings/{id}", async (string id, INextTechEventApi api) => await api.GetSettingsAsync(id));
//apiGroup.MapGet("/settings/by-user/{userId}", async (string userId, INextTechEventApi api) => await api.GetSettingsByUserIdAsync(userId));

//// User calendar (JSON calendar object)
//apiGroup.MapGet("/users/{userId}/calendar", async (string userId, INextTechEventApi api) => await api.GetUserCalendarAsync(userId));

app.Run();

public partial class Program
{ }