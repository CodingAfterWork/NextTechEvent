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
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddRazorComponents()
 .AddInteractiveServerComponents()
 .AddInteractiveWebAssemblyComponents()
 .AddAuthenticationStateSerialization(options => options.SerializeAllClaims = true); ;


builder.Services.AddScoped<INextTechEventApi, NextTechEventClient>();
builder.Services.AddScoped<NextTechEventRepository>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<INextTechEventApi,NextTechEventClient>(httpClient =>
{
    httpClient.BaseAddress = new("https://localhost:7081/");
});
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


app.MapGet("calendar/{id}", async (string id, NextTechEventRepository api) =>
{
    var settings = await api.GetSettingsByIdAsync(id);
    var calendar = await api.GetUserCalendarAsync(settings!.UserId!);

    CalendarSerializer serializer = new();
    MemoryStream ms = new MemoryStream();
    serializer.Serialize(calendar, ms, System.Text.Encoding.UTF8);
    ms.Position = 0;
    return Results.Stream(ms, contentType: "text/calendar", "NextTechEvent.ics");

});

// Minimal APIs for INextTechEventApi
var apiGroup = app.MapGroup("/api");

// Conferences
apiGroup.MapGet("/conferences", async (NextTechEventRepository api) => await api.GetConferencesAsync());


//apiGroup.MapPost("/conferences", async (Conference conference, INextTechEventApi api) =>
//{
// var saved = await api.SaveConferenceAsync(conference);
// return Results.Ok(saved);
//});
apiGroup.MapGet("/conferences/{*id}", async (string id, NextTechEventRepository api) => await api.GetConferenceAsync(id));
apiGroup.MapGet("/conferences/count", async (NextTechEventRepository api) => await api.GetConferenceCountsAsync());
//apiGroup.MapGet("/conferences/range", async (DateOnly startdate, DateOnly enddate, NextTechEventRepository api) => await api.GetConferencesAsync(startdate, enddate));
apiGroup.MapGet("/conferences/range", async (DateOnly startdate, DateOnly enddate, INextTechEventApi api) => await api.GetConferencesAsync(startdate, enddate));
apiGroup.MapGet("/conferences/near", async (double latitude, double longitude, double radius, DateOnly startdate, DateOnly enddate, NextTechEventRepository api) => await api.GetConferencesAsync(latitude, longitude, radius, startdate, enddate));
apiGroup.MapGet("/conferences/search", async (string searchterm, NextTechEventRepository api) => await api.SearchConferencesAsync(searchterm));
apiGroup.MapGet("/conferences/search-active", async (bool hasOpenCallforPaper, string? searchterm, int pagesize, int page, NextTechEventRepository api) => await api.SearchActiveConferencesAsync(hasOpenCallforPaper, searchterm ?? string.Empty, pagesize, page));
apiGroup.MapGet("/conferences/count-by-date", async (DateOnly start, DateOnly end, string? searchterm, NextTechEventRepository api) => await api.GetConferenceCountByDate(start, end, searchterm ?? string.Empty));
apiGroup.MapGet("/conferences/open-cfp", async (int startIndex, int count, NextTechEventRepository api, CancellationToken ct) => await api.GetConferencesWithOpenCfpAsync(new ItemsProviderRequest(startIndex, count, ct)));
apiGroup.MapGet("/conferences/by-user", async (ClaimsPrincipal user,NextTechEventRepository api) => await api.GetConferencesByUserAsync(user));
apiGroup.MapGet("/conferences/by-weather", async (double averageTemp, NextTechEventRepository api) => await api.GetConferencesByWeatherAsync(averageTemp));
apiGroup.MapGet("/conferences/weather/{*conferenceId}", async (string conferenceId, NextTechEventRepository api) => await api.GetWeatherTimeSeriesAsync(conferenceId));

//// Statuses
apiGroup.MapPost("/statuses", async (Status status, ClaimsPrincipal user,INextTechEventApi api) => Results.Ok(await api.SaveStatusAsync(status)));
apiGroup.MapGet("/statuses/{*conferenceId}", async (string conferenceId, ClaimsPrincipal user, NextTechEventRepository api) => await api.GetStatusAsync(conferenceId, user));
apiGroup.MapGet("/statuses/by-user/", async (ClaimsPrincipal user, INextTechEventApi api) => await api.GetStatusesAsync());


//// Settings
apiGroup.MapPost("/settings", async (Settings settings, ClaimsPrincipal user, NextTechEventRepository api) => Results.Ok(await api.SaveSettingsAsync(settings,user)));
apiGroup.MapGet("/settings/", async (ClaimsPrincipal user, NextTechEventRepository api) => await api.GetSettingsAsync(user));

app.Run();

public partial class Program
{ }