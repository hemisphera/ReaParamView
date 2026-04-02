using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using ReaParamView.WebApp.Components;
using ReaParamView.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
  .AddInteractiveServerComponents();

builder.Services.AddSingleton<ParameterDisplayService>();
builder.Services.AddHostedService<UdpReceiverService>();

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

await app.WaitForShutdownAsync();