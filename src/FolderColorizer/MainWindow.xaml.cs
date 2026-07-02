using System.IO;
using System.Windows;
using FolderColorizer.Services;
using Microsoft.Win32;

namespace FolderColorizer;

public partial class MainWindow : Window
{
    public MainWindow(string? initialFolder = null)
    {
        InitializeComponent();
        PaletteItems.ItemsSource = FolderPalette.All;
        FolderPathTextBox.Text = initialFolder ?? string.Empty;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose a folder to color",
            Multiselect = false
        };

        if (Directory.Exists(FolderPathTextBox.Text))
        {
            dialog.InitialDirectory = FolderPathTextBox.Text;
        }

        if (dialog.ShowDialog(this) == true)
        {
            FolderPathTextBox.Text = dialog.FolderName;
            StatusText.Text = string.Empty;
        }
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string colorId })
        {
            return;
        }

        FolderColor color = FolderPalette.Find(colorId)
            ?? throw new InvalidOperationException($"Unknown color: {colorId}");

        RunAction(
            () => FolderCustomizationService.Apply(FolderPathTextBox.Text, color),
            $"{color.DisplayName} applied");
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e) =>
        RunAction(
            () =>
            {
                bool changed = FolderCustomizationService.Reset(FolderPathTextBox.Text);
                if (!changed)
                {
                    throw new InvalidOperationException("This folder is not colored by Folder Colorizer.");
                }
            },
            "Default icon restored");

    private void RunAction(Action action, string successMessage)
    {
        try
        {
            action();
            StatusText.Foreground = System.Windows.Media.Brushes.SeaGreen;
            StatusText.Text = successMessage;
        }
        catch (Exception exception)
        {
            StatusText.Foreground = System.Windows.Media.Brushes.Crimson;
            StatusText.Text = exception.Message;
        }
    }
}
