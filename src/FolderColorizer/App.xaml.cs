using System.Windows;
using FolderColorizer.Core;
using FolderColorizer.Services;

namespace FolderColorizer;

public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            if (TryRunCommand(e.Args))
            {
                Shutdown();
                return;
            }

            string? initialFolder = e.Args.Length == 1 ? e.Args[0] : null;
            var window = new MainWindow(initialFolder);
            MainWindow = window;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            window.Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "Folder Colorizer",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static bool TryRunCommand(string[] args)
    {
        if (args.Length == 1 && args[0].Equals("--register", StringComparison.OrdinalIgnoreCase))
        {
            ContextMenuRegistrar.Register(Environment.ProcessPath
                ?? throw new InvalidOperationException("Cannot locate the application executable."));
            return true;
        }

        if (args.Length == 1 && args[0].Equals("--unregister", StringComparison.OrdinalIgnoreCase))
        {
            ContextMenuRegistrar.Unregister();
            return true;
        }

        if (args.Length == 3 &&
            args[0].Equals("--color", StringComparison.OrdinalIgnoreCase))
        {
            FolderColor color = FolderPalette.Find(args[1])
                ?? throw new ArgumentException($"Unknown color: {args[1]}");
            FolderCustomizationService.Apply(args[2], color);
            ExplorerWindowRefresher.RefreshAfterShellCommand(
                args[2],
                FolderColorState.GetIconFileName(color.Id));
            return true;
        }

        if (args.Length == 2 &&
            args[0].Equals("--reset", StringComparison.OrdinalIgnoreCase))
        {
            FolderCustomizationService.Reset(args[1]);
            ExplorerWindowRefresher.RefreshAfterShellCommand(
                args[1],
                iconFileName: null);
            return true;
        }

        return false;
    }
}
