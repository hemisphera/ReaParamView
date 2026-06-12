using System.Reflection;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Hemisphera.Hulp.WebApp.Components;
using Hemisphera.Hulp.WebApp.Services;

var options = new WebApplicationOptions
{
  Args = args,
  ContentRootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
};
var openBrowser = args.Contains("--open");
var builder = WebApplication.CreateBuilder(options);

// Add services to the container.
builder.Services.AddRazorComponents()
  .AddInteractiveServerComponents();

builder.Services.AddSingleton<OscService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OscService>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode();

await app.StartAsync();

// Output the addresses the app is listening on
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var server = app.Services.GetRequiredService<IServer>();
var addressesFeature = server.Features.GetRequiredFeature<IServerAddressesFeature>();

// Get local IP address for network access
var hostName = System.Net.Dns.GetHostName();
var ips = System.Net.Dns.GetHostAddresses(hostName);
var localIp = ips.FirstOrDefault(ip =>
  ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
  !ip.ToString().StartsWith("127."));

if (localIp != null && addressesFeature.Addresses.Count > 0)
{
  if (Uri.TryCreate(addressesFeature.Addresses.First(), UriKind.Absolute, out var uri))
  {
    var endpoint = $"http://{localIp}:{uri.Port}";
    logger.LogInformation("WebApp is running at: {Endpoint}", endpoint);
  }
}
else
{
  foreach (var address in addressesFeature.Addresses)
  {
    logger.LogInformation("WebApp is running at: {Address}", address);
  }
}

if (openBrowser && addressesFeature.Addresses.Count > 0)
{
  var urlToOpen = addressesFeature.Addresses.First();
  
  // Try to use the local IP if available for better accessibility
  if (localIp != null && Uri.TryCreate(urlToOpen, UriKind.Absolute, out var uri))
  {
    urlToOpen = $"http://{localIp}:{uri.Port}";
  }

  logger.LogInformation("Opening browser at: {URL}", urlToOpen);
  Process.Start(new ProcessStartInfo
  {
    FileName = urlToOpen,
    UseShellExecute = true
  });
}

await app.WaitForShutdownAsync();