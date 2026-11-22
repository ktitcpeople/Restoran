using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RestaurantBlazor;
using RestaurantBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5161";
builder.Services.AddScoped(sp => new HttpClient(new ValidationHandler())
{
    BaseAddress = new Uri(apiBaseUrl.TrimEnd('/'))
});

builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<KitchenHubService>();
builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();
