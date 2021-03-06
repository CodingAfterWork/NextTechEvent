using Auth0.AspNetCore.Authentication;
using Blazm.Components;
using NextTechEvent.Data;
using NextTechEvent.Data.Index;
using Raven.Client.Documents;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddScoped<INextTechEventApi, NextTechEventApi>();
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
    store.ExecuteIndexAsync(new ConferencesByWeather());
    return store;
});

builder.Services
    .AddAuth0WebAppAuthentication(options =>
    {
        options.Domain = builder.Configuration["Auth0:Authority"];
        options.ClientId = builder.Configuration["Auth0:ClientId"];
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages(); //Do I need this?
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

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

app.Run();
