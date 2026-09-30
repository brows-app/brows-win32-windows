namespace Brows.Win32;

/// <summary>Identifies a stock icon provided by the Windows Shell.</summary>
/// <remarks>Values match the corresponding native <c>SHSTOCKICONID</c> identifiers.</remarks>
public enum Win32StockIcon : long {
    /// <summary>Icon for a document without a registered file association.</summary>
    DocNoAssoc = 0,
    /// <summary>Icon for a document with a registered file association.</summary>
    DocAssoc = 1,
    /// <summary>Generic application icon.</summary>
    Application = 2,
    /// <summary>Closed folder icon.</summary>
    Folder = 3,
    /// <summary>Open folder icon.</summary>
    FolderOpen = 4,
    /// <summary>Icon for a 5.25-inch floppy disk drive.</summary>
    Drive525 = 5,
    /// <summary>Icon for a 3.5-inch floppy disk drive.</summary>
    Drive35 = 6,
    /// <summary>Icon for a removable drive.</summary>
    DriveRemove = 7,
    /// <summary>Icon for a fixed disk drive.</summary>
    DriveFixed = 8,
    /// <summary>Icon for a network drive.</summary>
    DriveNet = 9,
    /// <summary>Icon for a disconnected network drive.</summary>
    DriveNetDisabled = 10,
    /// <summary>Icon for a CD drive.</summary>
    DriveCD = 11,
    /// <summary>Icon for a RAM disk drive.</summary>
    DriveRam = 12,
    /// <summary>Network world icon.</summary>
    World = 13,
    /// <summary>Server icon.</summary>
    Server = 15,
    /// <summary>Printer icon.</summary>
    Printer = 16,
    /// <summary>My Network Places icon.</summary>
    MyNetwork = 17,
    /// <summary>Search icon.</summary>
    Find = 22,
    /// <summary>Help icon.</summary>
    Help = 23,
    /// <summary>Share icon.</summary>
    Share = 28,
    /// <summary>Shortcut link icon.</summary>
    Link = 29,
    /// <summary>Icon for a slow file.</summary>
    SlowFile = 30,
    /// <summary>Empty Recycle Bin icon.</summary>
    Recycler = 31,
    /// <summary>Full Recycle Bin icon.</summary>
    RecyclerFull = 32,
    /// <summary>Audio CD media icon.</summary>
    MediaCDAudio = 40,
    /// <summary>Lock icon.</summary>
    Lock = 47,
    /// <summary>Automatic list icon.</summary>
    AutoList = 49,
    /// <summary>Network printer icon.</summary>
    PrinterNet = 50,
    /// <summary>Server share icon.</summary>
    ServerShare = 51,
    /// <summary>Fax printer icon.</summary>
    PrinterFax = 52,
    /// <summary>Network fax printer icon.</summary>
    PrinterFaxNet = 53,
    /// <summary>Printer file icon.</summary>
    PrinterFile = 54,
    /// <summary>Stack icon.</summary>
    Stack = 55,
    /// <summary>Super Video CD media icon.</summary>
    MediaSVCD = 56,
    /// <summary>Compressed folder icon.</summary>
    StuffedFolder = 57,
    /// <summary>Unknown drive icon.</summary>
    DriveUnknown = 58,
    /// <summary>Icon for a DVD drive.</summary>
    DriveDVD = 59,
    /// <summary>DVD media icon.</summary>
    MediaDVD = 60,
    /// <summary>DVD-RAM media icon.</summary>
    MediaDVDRam = 61,
    /// <summary>DVD-RW media icon.</summary>
    MediaDVDRW = 62,
    /// <summary>DVD-R media icon.</summary>
    MediaDVDR = 63,
    /// <summary>DVD-ROM media icon.</summary>
    MediaDVDRom = 64,
    /// <summary>Audio CD Plus media icon.</summary>
    MediaCDAudioPlus = 65,
    /// <summary>CD-RW media icon.</summary>
    MediaCDRW = 66,
    /// <summary>CD-R media icon.</summary>
    MediaCDR = 67,
    /// <summary>CD burning icon.</summary>
    MediaCDBurn = 68,
    /// <summary>Blank CD media icon.</summary>
    MediaBlankCD = 69,
    /// <summary>CD-ROM media icon.</summary>
    MediaCDRom = 70,
    /// <summary>Audio files icon.</summary>
    AudioFiles = 71,
    /// <summary>Image files icon.</summary>
    ImageFiles = 72,
    /// <summary>Video files icon.</summary>
    VideoFiles = 73,
    /// <summary>Mixed files icon.</summary>
    MixedFiles = 74,
    /// <summary>Back folder icon.</summary>
    FolderBack = 75,
    /// <summary>Front folder icon.</summary>
    FolderFront = 76,
    /// <summary>Shield icon.</summary>
    Shield = 77,
    /// <summary>Warning icon.</summary>
    Warning = 78,
    /// <summary>Information icon.</summary>
    Info = 79,
    /// <summary>Error icon.</summary>
    Error = 80,
    /// <summary>Key icon.</summary>
    Key = 81,
    /// <summary>Software icon.</summary>
    Software = 82,
    /// <summary>Rename icon.</summary>
    Rename = 83,
    /// <summary>Delete icon.</summary>
    Delete = 84,
    /// <summary>Audio DVD media icon.</summary>
    MediaAudioDVD = 85,
    /// <summary>Movie DVD media icon.</summary>
    MediaMovieDVD = 86,
    /// <summary>Enhanced CD media icon.</summary>
    MediaEnhancedCD = 87,
    /// <summary>Enhanced DVD media icon.</summary>
    MediaEnhancedDVD = 88,
    /// <summary>HD DVD media icon.</summary>
    MediaHDDVD = 89,
    /// <summary>Blu-ray media icon.</summary>
    MediaBluray = 90,
    /// <summary>Video CD media icon.</summary>
    MediaVCD = 91,
    /// <summary>DVD+R media icon.</summary>
    MediaDVDPlusR = 92,
    /// <summary>DVD+RW media icon.</summary>
    MediaDVDPlusRW = 93,
    /// <summary>Desktop PC icon.</summary>
    DesktopPC = 94,
    /// <summary>Mobile PC icon.</summary>
    MobilePC = 95,
    /// <summary>Users icon.</summary>
    Users = 96,
    /// <summary>Smart media icon.</summary>
    MediaSmartMedia = 97,
    /// <summary>CompactFlash media icon.</summary>
    MediaCompactFlash = 98,
    /// <summary>Cell phone icon.</summary>
    DeviceCellphone = 99,
    /// <summary>Camera icon.</summary>
    DeviceCamera = 100,
    /// <summary>Video camera icon.</summary>
    DeviceVideoCamera = 101,
    /// <summary>Audio player icon.</summary>
    DeviceAudioPlayer = 102,
    /// <summary>Network connection icon.</summary>
    NetworkConnect = 103,
    /// <summary>Internet icon.</summary>
    Internet = 104,
    /// <summary>ZIP file icon.</summary>
    ZipFile = 105,
    /// <summary>Settings icon.</summary>
    Settings = 106,
    /// <summary>Icon for an HD DVD drive.</summary>
    DriveHDDVD = 132,
    /// <summary>Icon for a Blu-ray drive.</summary>
    DriveBD = 133,
    /// <summary>HD DVD-ROM media icon.</summary>
    MediaHDDVDRom = 134,
    /// <summary>HD DVD-R media icon.</summary>
    MediaHDDVDR = 135,
    /// <summary>HD DVD-RAM media icon.</summary>
    MediaHDDVDRam = 136,
    /// <summary>Blu-ray ROM media icon.</summary>
    MediaBDRom = 137,
    /// <summary>Blu-ray recordable media icon.</summary>
    MediaBDR = 138,
    /// <summary>Blu-ray rewritable media icon.</summary>
    MediaBDRE = 139,
    /// <summary>Clustered drive icon.</summary>
    ClusteredDrive = 140,
    /// <summary>Marker for the upper bound of the stock icon identifiers.</summary>
    MaxIcons = 181,
}
