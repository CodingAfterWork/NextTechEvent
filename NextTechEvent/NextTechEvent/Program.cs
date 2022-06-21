using Blazm.Components;
using NextTechEvent.Data;
using NextTechEvent.Data.Index;
using Raven.Client.Documents;
using System.Security.Cryptography.X509Certificates;

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

//services.AddAuthentication(options => {
//options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//})
//    .AddCookie()
//    .AddOpenIdConnect("Auth0", options => {
//    options.Authority = $"https://{Configuration["Auth0:Domain"]}";

//    options.ClientId = Configuration["Auth0:ClientId"];
//    options.ClientSecret = Configuration["Auth0:ClientSecret"];

//    options.ResponseType = OpenIdConnectResponseType.Code;

//    options.Scope.Clear();
//    options.Scope.Add("openid");
//    options.Scope.Add("profile"); // <- Optional extra
//    options.Scope.Add("email");   // <- Optional extra

//    options.CallbackPath = new PathString("/callback");
//    options.ClaimsIssuer = "Auth0";
//    options.SaveTokens = true;

//    // Add handling of lo
//    options.Events = new OpenIdConnectEvents
//    {
//        OnRedirectToIdentityProviderForSignOut = (context) =>
//        {
//            var logoutUri = $"https://{Configuration["Auth0:Domain"]}/v2/logout?client_id={Configuration["Auth0:ClientId"]}";

//            var postLogoutUri = context.Properties.RedirectUri;
//            if (!string.IsNullOrEmpty(postLogoutUri))
//            {
//                if (postLogoutUri.StartsWith("/"))
//                {
//                    var request = context.Request;
//                    postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
//                }
//                logoutUri += $"&returnTo={Uri.EscapeDataString(postLogoutUri)}";
//            }

//            context.Response.Redirect(logoutUri);
//            context.HandleResponse();

//            return Task.CompletedTask;
//        }
//    };

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

app.Run();
