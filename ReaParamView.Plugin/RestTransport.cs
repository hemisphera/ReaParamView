using ReaParamView.Types;

namespace ReaParamView.Plugin;

public class RestTransport : ITransport
{
  private readonly HttpClient _httpClient = new();
  private readonly Uri _baseUri = new("http://localhost:5079/api/parameters");


  public async Task SendMessage(MessageDto message, CancellationToken token)
  {
    await _httpClient.SendAsync(new HttpRequestMessage
    {
      RequestUri = _baseUri,
      Method = HttpMethod.Post,
      Content = System.Net.Http.Json.JsonContent.Create(message, FxJsonContext.Default.MessageDto)
    }, token);
  }
}