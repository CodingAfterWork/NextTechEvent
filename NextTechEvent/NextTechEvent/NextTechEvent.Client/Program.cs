using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NextTechEvent.Client.Data;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddScoped<INextTechEventApi, NextTechEventClient>();
builder.Services.AddHttpClient<INextTechEventApi, NextTechEventClient>(httpClient =>
{
    httpClient.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

await builder.Build().RunAsync();
