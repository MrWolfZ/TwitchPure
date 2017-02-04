using System;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using ReactiveUI;
using TwitchPure.Services.Dto.Twitch;

namespace TwitchPure.UI.ViewModels.Controls
{
  public sealed class StreamThumbnailViewModel : ReactiveObject, IDisposable
  {
    private readonly ObservableAsPropertyHelper<ImageSource> imageSource;

    public StreamThumbnailViewModel(Stream source)
    {
      this.Source = source;

      this.imageSource = Observable.FromAsync(() => new HttpClient().GetByteArrayAsync(source.Preview.Large))
                                   .ObserveOn(RxApp.MainThreadScheduler)
                                   .SelectMany(CreateImageSource)
                                   .ToProperty(this, vm => vm.ImageSource, scheduler: RxApp.MainThreadScheduler);
    }

    public Stream Source { get; }

    public ImageSource ImageSource => this.imageSource.Value;

    private static async Task<ImageSource> CreateImageSource(byte[] bytes)
    {
      using (var ms = new InMemoryRandomAccessStream())
      {
        using (var writer = new DataWriter(ms.GetOutputStreamAt(0)))
        {
          writer.WriteBytes(bytes);
          await writer.StoreAsync();
        }

        var image = new BitmapImage();
        await image.SetSourceAsync(ms);
        return image;
      }
    }

    public void Dispose() => this.imageSource.Dispose();
  }
}
