using Brows.Composition;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Brows.Win32;

public interface IWin32WindowsServices : IExport {
    Task<BitmapSource> GetIconSource(
        string path,
        FileAttributes? attributes = null,
        CancellationToken cancellationToken = default);

    Task<BitmapSource> GetIconSource(
        Win32StockIcon stockIcon,
        CancellationToken cancellationToken = default);

    Task<BitmapSource> GetOverlayIconSource(
        string path,
        FileAttributes? attributes = null,
        CancellationToken cancellationToken = default);

    Task<BitmapSource> GetThumbnailSource(
        string path,
        int width,
        int height,
        CancellationToken cancellationToken = default);
}
