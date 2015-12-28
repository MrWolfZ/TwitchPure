using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TwitchPure.Services.Dto.Twitch;

namespace TwitchPure.Services.Data.Twitch
{
  public interface ITwitchApi
  {
    Task<ICollection<Stream>> GetTopStreamsAsync();
  }

  internal sealed class TwitchApi : ITwitchApi
  {
    public async Task<ICollection<Stream>> GetTopStreamsAsync()
    {
      var c = new HttpClient();
      var response = await c.GetStringAsync("https://api.twitch.tv/kraken/streams");
      return JsonConvert.DeserializeObject<StreamsResponse>(response).Streams;
    }
  }
}
