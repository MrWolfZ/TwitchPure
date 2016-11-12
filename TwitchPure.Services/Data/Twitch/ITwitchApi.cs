using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TwitchPure.Services.Dto.Twitch;

namespace TwitchPure.Services.Data.Twitch
{
  public interface ITwitchApi
  {
    Task<StreamsResponse> GetTopStreamsAsync(int limit = 25, int offset = 0, ICollection<string> channels = null);
  }

  internal sealed class TwitchApi : ITwitchApi
  {
    public async Task<StreamsResponse> GetTopStreamsAsync(int limit = 25, int offset = 0, ICollection<string> channels = null)
    {
      var c = new HttpClient();
      c.DefaultRequestHeaders.Add("Client-ID", "lsx8xunzjbcx15nwhjrjwbw7ryn81uv");
      var url = $"https://api.twitch.tv/kraken/streams?limit={limit}&offset={offset}&language=en";
      if (channels != null)
      {
        url = $"{url}&channel={string.Join(",", channels)}";
      }
      var response = await c.GetStringAsync(url);
      return JsonConvert.DeserializeObject<StreamsResponse>(response);
    }
  }
}
