using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TwitchPure.Services.Dto.Twitch;

namespace TwitchPure.Services.Data.Twitch
{
  public interface ITwitchApi
  {
    Task<StreamsResponse> GetTopStreamsAsync(int limit = 25, int offset = 0);
  }

  internal sealed class TwitchApi : ITwitchApi
  {
    public async Task<StreamsResponse> GetTopStreamsAsync(int limit = 25, int offset = 0)
    {
      var c = new HttpClient();
      var response = await c.GetStringAsync($"https://api.twitch.tv/kraken/streams?limit={limit}&offset={offset}");
      return JsonConvert.DeserializeObject<StreamsResponse>(response);
    }
  }
}
