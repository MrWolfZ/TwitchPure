using System;
using System.Collections.Generic;
using Windows.Storage;

namespace TwitchPure.Services
{
  public interface IFavoritesService
  {
    void AddChannelToFavorites(string channel);
    void RemoveChannelFromFavorites(string channel);
    bool IsChannelFavorite(string channel);
    HashSet<string> GetFavorites();
  }

  internal sealed class FavoritesService : IFavoritesService
  {
    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public void AddChannelToFavorites(string channel)
    {
      var favorites = this.GetFavoritesFromSettings();
      favorites.Add(channel);
      this.SaveFavoritesToSettings(favorites);
    }

    public void RemoveChannelFromFavorites(string channel)
    {
      var favorites = this.GetFavoritesFromSettings();
      favorites.Remove(channel);
      this.SaveFavoritesToSettings(favorites);
    }

    public bool IsChannelFavorite(string channel) => this.GetFavorites().Contains(channel);

    public HashSet<string> GetFavorites() => this.GetFavoritesFromSettings();

    private HashSet<string> GetFavoritesFromSettings()
    {
      var favorites = new HashSet<string>();
      object data;
      if (this.localSettings.Values.TryGetValue("favorite_channels", out data))
      {
        foreach (var channel in ((string)data).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
          favorites.Add(channel);
        }
      }
      return favorites;
    }

    private void SaveFavoritesToSettings(HashSet<string> value)
    {
      var data = string.Join(",", value);
      this.localSettings.Values["favorite_channels"] = data;
    }
  }
}
