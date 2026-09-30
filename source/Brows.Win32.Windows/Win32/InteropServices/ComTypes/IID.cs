namespace Brows.Win32.InteropServices.ComTypes;

internal static class IID {
    public const string IShellItem = "43826D1E-E718-42EE-BC55-A1E261C37BFE";
    public const string IShellItemImageFactory = "bcc18b79-ba16-442f-80c4-8a59c30c463b";
    public const string IImageList = "46EB5926-582E-4017-9FDF-E8998DAA0950";

    public static class Managed {
        public static readonly Guid IShellItem = new(IID.IShellItem);
        public static readonly Guid IShellItemImageFactory = new(IID.IShellItemImageFactory);
        public static readonly Guid IImageList = new(IID.IImageList);
    }
}
