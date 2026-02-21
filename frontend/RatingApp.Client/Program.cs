using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RatingApp.Client;
using RatingApp.Client.Auth;
using RatingApp.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// In development the frontend and backend run on separate ports.
// In production the API serves the Blazor app from the same origin.
var apiBaseUrl = builder.HostEnvironment.IsDevelopment()
    ? "http://localhost:5212"
    : builder.HostEnvironment.BaseAddress.TrimEnd('/');

// Auth
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<CustomAuthStateProvider>());

// HTTP Client with JWT handler
builder.Services.AddTransient<JwtAuthMessageHandler>();
builder.Services.AddHttpClient("RatingApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<JwtAuthMessageHandler>();

// Provide a default HttpClient that uses the named client (for convenience)
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("RatingApi"));

// Application services
builder.Services.AddScoped<PhotoApiService>();
builder.Services.AddScoped<AuthApiService>();
builder.Services.AddScoped<ProfileApiService>();
builder.Services.AddScoped<CandidateApiService>();
builder.Services.AddScoped<RatingApiService>();
builder.Services.AddScoped<ChatApiService>();

await builder.Build().RunAsync();