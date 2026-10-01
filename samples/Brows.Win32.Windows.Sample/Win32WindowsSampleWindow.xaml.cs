using Brows.Composition;
using Brows.Win32;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Brows;

sealed partial class Win32WindowsSampleWindow : Window, IExport {
    [ImportRequired]
    internal IWin32WindowsServices Services { get; set; }

    private readonly HashSet<Task> PreviewLoads = [];

    private CancellationTokenSource PreviewCancellation;
    private int PreviewRequest;
    private bool ClosingInProgress;
    private bool ShutdownComplete;

    public Win32WindowsSampleWindow() {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) {
        PathTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        await LoadPreviewsAsync();
    }

    private async void Window_Closing(object sender, CancelEventArgs e) {
        if (ShutdownComplete) {
            return;
        }
        e.Cancel = true;
        if (ClosingInProgress) {
            return;
        }
        ClosingInProgress = true;
        IsEnabled = false;
        PreviewRequest++;
        PreviewCancellation?.Cancel();

        try {
            await Task.WhenAll(PreviewLoads);
        }
        catch (Exception ex) {
            Debug.WriteLine(ex);
        }
        finally {
            _ = ShutdownComplete = true;
            _ = Dispatcher.BeginInvoke(Close);
        }
    }

    private async void BrowseButton_Click(object sender, RoutedEventArgs e) {
        var currentPath = PathTextBox.Text.Trim();
        var dialog = new OpenFileDialog {
            Title = "Choose a file to preview",
            CheckFileExists = true,
            Multiselect = false,
            Filter = "All files (*.*)|*.*",
            InitialDirectory = Directory.Exists(currentPath)
                ? currentPath
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        if (dialog.ShowDialog(this) == true) {
            PathTextBox.Text = dialog.FileName;
            await LoadPreviewsAsync();
        }
    }

    private async void LoadButton_Click(object sender, RoutedEventArgs e) {
        await LoadPreviewsAsync();
    }

    private async void PathTextBox_KeyDown(object sender, KeyEventArgs e) {
        if (e.Key == Key.Enter) {
            e.Handled = true;
            await LoadPreviewsAsync();
        }
    }

    private Task LoadPreviewsAsync() {
        if (ClosingInProgress) {
            return Task.CompletedTask;
        }
        var load = LoadPreviewsCoreAsync();
        PreviewLoads.Add(load);
        return trackPreviewLoadAsync(load);
        async Task trackPreviewLoadAsync(Task load) {
            try {
                await load;
            }
            finally {
                PreviewLoads.Remove(load);
            }
        }
    }

    private async Task LoadPreviewsCoreAsync() {
        var path = PathTextBox.Text.Trim();
        var request = ++PreviewRequest;
        var cancellation = new CancellationTokenSource();
        var previousCancellation = PreviewCancellation;
        PreviewCancellation = cancellation;
        previousCancellation?.Cancel();

        OverlayImage.Source = null;
        ThumbnailImage.Source = null;
        IconImage.Source = null;

        if (string.IsNullOrWhiteSpace(path)) {
            OverlayPlaceholder.Visibility = Visibility.Visible;
            ThumbnailPlaceholder.Visibility = Visibility.Visible;
            IconPlaceholder.Visibility = Visibility.Visible;
            IconPlaceholder.Text = "No icon";
            OverlayStatusText.Text = "Enter a file or folder path to inspect its overlay.";
            ThumbnailStatusText.Text = "Enter a file or folder path to request a thumbnail.";
            IconStatusText.Text = "Enter a file or folder path to request its icon.";
            PageStatusText.Text = "Enter a path, or browse to a file.";
            LoadButton.IsEnabled = true;
            cancellation.Dispose();
            if (ReferenceEquals(PreviewCancellation, cancellation)) {
                PreviewCancellation = null;
            }
            return;
        }

        OverlayPlaceholder.Visibility = Visibility.Visible;
        ThumbnailPlaceholder.Visibility = Visibility.Visible;
        IconPlaceholder.Visibility = Visibility.Visible;
        OverlayPlaceholder.Text = "Loading overlay…";
        ThumbnailPlaceholder.Text = "Loading thumbnail…";
        IconPlaceholder.Text = "Loading icon…";
        OverlayStatusText.Text = "Querying the Shell's effective overlay…";
        ThumbnailStatusText.Text = "Requesting a shell thumbnail…";
        IconStatusText.Text = "Requesting a shell icon…";
        PageStatusText.Text = $"Loading previews for {path}";

        var overlayTask = LoadOverlayAsync(path, request, cancellation.Token);
        var thumbnailTask = LoadThumbnailAsync(path, request, cancellation.Token);
        var iconTask = LoadIconAsync(path, request, cancellation.Token);
        try {
            await Task.WhenAll(overlayTask, thumbnailTask, iconTask);
            if (IsCurrent(request)) {
                PageStatusText.Text = $"Finished loading previews for {path}";
            }
        }
        finally {
            if (IsCurrent(request)) {
                LoadButton.IsEnabled = true;
            }
            if (ReferenceEquals(PreviewCancellation, cancellation)) {
                PreviewCancellation = null;
            }
            cancellation.Dispose();
        }
    }

    private async Task LoadOverlayAsync(string path, int request, CancellationToken token) {
        try {
            var source = await Services.GetOverlayIconSource(path, cancellationToken: token);
            if (!IsCurrent(request)) {
                return;
            }
            OverlayImage.Source = source;
            OverlayPlaceholder.Visibility = source is null ? Visibility.Visible : Visibility.Collapsed;
            OverlayPlaceholder.Text = "No overlay icon";
            OverlayStatusText.Text = source is null
                ? "The Shell has no overlay icon for this path."
                : "The shell returned an overlay icon for this path.";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) {
        }
        catch (Exception ex) {
            if (IsCurrent(request)) {
                OverlayPlaceholder.Text = "Overlay unavailable";
                OverlayStatusText.Text = ex.Message;
            }
        }
    }

    private async Task LoadThumbnailAsync(string path, int request, CancellationToken token) {
        try {
            var source = await Services.GetThumbnailSource(path, 320, 240, token);
            if (!IsCurrent(request)) {
                return;
            }
            ThumbnailImage.Source = source;
            ThumbnailPlaceholder.Visibility = source is null ? Visibility.Visible : Visibility.Collapsed;
            ThumbnailPlaceholder.Text = "No thumbnail";
            ThumbnailStatusText.Text = source is null
                ? "The shell did not return a thumbnail for this path."
                : "The shell returned a thumbnail for this path.";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) {
        }
        catch (Exception ex) {
            if (IsCurrent(request)) {
                ThumbnailPlaceholder.Text = "Thumbnail unavailable";
                ThumbnailStatusText.Text = ex.Message;
            }
        }
    }

    private async Task LoadIconAsync(string path, int request, CancellationToken token) {
        try {
            var source = await Services.GetIconSource(path, cancellationToken: token);
            if (!IsCurrent(request)) {
                return;
            }
            IconImage.Source = source;
            IconPlaceholder.Visibility = source is null ? Visibility.Visible : Visibility.Collapsed;
            IconPlaceholder.Text = "No icon";
            IconStatusText.Text = source is null
                ? "The Shell did not return an icon for this path."
                : "The Shell returned an icon for this path.";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) {
        }
        catch (Exception ex) {
            if (IsCurrent(request)) {
                IconPlaceholder.Text = "Icon unavailable";
                IconStatusText.Text = ex.Message;
            }
        }
    }

    private bool IsCurrent(int request) => request == PreviewRequest;
}
