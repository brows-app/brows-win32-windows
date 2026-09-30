using Brows.Win32.PlatformInvoke;
using System;
using System.Runtime.InteropServices;

namespace Brows.Win32.InteropServices.ComTypes;

[Guid(IID.IImageList)]
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IImageList {
    [PreserveSig] HRESULT Add(IntPtr hbmImage, IntPtr hbmMask, out int pi);
    [PreserveSig] HRESULT ReplaceIcon(int i, IntPtr hicon, out int pi);
    [PreserveSig] HRESULT SetOverlayImage(int iImage, int iOverlay);
    [PreserveSig] HRESULT Replace(int i, IntPtr hbmImage, IntPtr hbmMask);
    [PreserveSig] HRESULT AddMasked(IntPtr hbmImage, uint crMask, out int pi);
    [PreserveSig] HRESULT Draw(IntPtr pimldp);
    [PreserveSig] HRESULT Remove(int i);
    [PreserveSig] HRESULT GetIcon(int i, uint flags, out IntPtr picon);
    [PreserveSig] HRESULT GetImageInfo(int i, IntPtr pImageInfo);
    [PreserveSig] HRESULT Copy(int iDst, IntPtr punkSrc, int iSrc, uint uFlags);
    [PreserveSig] HRESULT Merge(int i1, IntPtr punk2, int i2, int dx, int dy, ref Guid riid, out IntPtr ppv);
    [PreserveSig] HRESULT Clone(ref Guid riid, out IntPtr ppv);
    [PreserveSig] HRESULT GetImageRect(int i, IntPtr prc);
    [PreserveSig] HRESULT GetIconSize(out int cx, out int cy);
    [PreserveSig] HRESULT SetIconSize(int cx, int cy);
    [PreserveSig] HRESULT GetImageCount(out int pi);
    [PreserveSig] HRESULT SetImageCount(uint uNewCount);
    [PreserveSig] HRESULT SetBkColor(uint clrBk, out uint pclr);
    [PreserveSig] HRESULT GetBkColor(out uint pclr);
    [PreserveSig] HRESULT BeginDrag(int iTrack, int dxHotspot, int dyHotspot);
    [PreserveSig] HRESULT EndDrag();
    [PreserveSig] HRESULT DragEnter(IntPtr hwndLock, int x, int y);
    [PreserveSig] HRESULT DragLeave(IntPtr hwndLock);
    [PreserveSig] HRESULT DragMove(int x, int y);
    [PreserveSig] HRESULT SetDragCursorImage(IntPtr punk, int iDrag, int dxHotspot, int dyHotspot);
    [PreserveSig] HRESULT DragShowNolock(int fShow);
    [PreserveSig] HRESULT GetDragImage(IntPtr ppt, IntPtr pptHotspot, ref Guid riid, out IntPtr ppv);
    [PreserveSig] HRESULT GetItemFlags(int i, out uint dwFlags);
    [PreserveSig] HRESULT GetOverlayImage(int iOverlay, out int piIndex);
}
