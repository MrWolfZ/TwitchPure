using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TwitchPure.Services.Dto.Twitch
{
  [DataContract]
  public sealed class StreamsResponse
  {
    [DataMember(Name = "_total")]
    public int Total { get; set; }

    [DataMember(Name = "streams")]
    public ICollection<Stream> Streams { get; set; }
  }

  [DataContract]
  public sealed class Stream
  {
    [DataMember(Name = "game")]
    public string Game { get; set; }

    [DataMember(Name = "viewers")]
    public int Viewers { get; set; }

    [DataMember(Name = "channel")]
    public Channel Channel { get; set; }

    [DataMember(Name = "preview")]
    public Preview Preview { get; set; }
  }

  [DataContract]
  public sealed class Channel
  {
    [DataMember(Name = "display_name")]
    public string DisplayName { get; set; }
  }

  [DataContract]
  public sealed class Preview
  {
    [DataMember(Name = "small")]
    public string Small { get; set; }

    [DataMember(Name = "medium")]
    public string Medium { get; set; }

    [DataMember(Name = "large")]
    public string Large { get; set; }
  }
}
