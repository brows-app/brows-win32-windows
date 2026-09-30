using Brows.Win32.PlatformInvoke;
using Domore.Logs;
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Brows.Win32.InteropServices;

internal static class ShellItemImageFactoryWrapperExtension {
    private static readonly ILog Log = Logging.For(typeof(ShellItemImageFactoryWrapperExtension));

    extension(ShellItemImageFactoryWrapper shellItemImageFactoryWrapper) {
        public BitmapSource GetBitmapSource(SIZE size, SIIGBF flags) {
            if (shellItemImageFactoryWrapper is null) {
                throw new ArgumentNullException(nameof(shellItemImageFactoryWrapper));
            }
            var bm = default(IntPtr);
            try {
                bm = shellItemImageFactoryWrapper.GetImage(size, flags);
                var source = Imaging.CreateBitmapSourceFromHBitmap(bm,
                                                                   IntPtr.Zero,
                                                                   Int32Rect.Empty,
                                                                   BitmapSizeOptions.FromEmptyOptions());
                if (source.CanFreeze) {
                    source.Freeze();
                }
                return source;
            }
            finally {
                if (bm != default) {
                    var success = gdi32.DeleteObject(bm);
                    if (success != true) {
                        if (Log.Warn()) {
                            Log.Warn("Could not delete object!");
                        }
                    }
                }
            }
        }
    }
}
