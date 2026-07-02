using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FolderColorizer.Services;

internal static class ExplorerWindowRefresher
{
    public static void RefreshAll()
        => RefreshSafely();

    private static void RefreshSafely()
    {
        try
        {
            RefreshCore();
        }
        catch (Exception)
        {
            // Explorer refresh is optional and must never fail the coloring command.
        }
    }

    private static void RefreshCore()
    {
        Type? shellType = Type.GetTypeFromProgID("Shell.Application");
        if (shellType is null)
        {
            return;
        }

        object? shell = null;
        object? windows = null;

        try
        {
            shell = Activator.CreateInstance(shellType);
            if (shell is null)
            {
                return;
            }

            windows = Invoke(shell, "Windows");
            if (windows is null)
            {
                return;
            }

            int count = Convert.ToInt32(Get(windows, "Count"), System.Globalization.CultureInfo.InvariantCulture);

            for (int index = 0; index < count; index++)
            {
                object? window = null;
                try
                {
                    window = Invoke(windows, "Item", index);
                    if (window is null)
                    {
                        continue;
                    }

                    string? executable = Get(window, "FullName") as string;
                    if (string.Equals(
                            Path.GetFileName(executable),
                            "explorer.exe",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        Invoke(window, "Refresh");
                    }
                }
                catch (COMException)
                {
                    // An Explorer window can disappear while the collection is enumerated.
                }
                catch (TargetInvocationException)
                {
                    // A transient Shell window must not make folder coloring fail.
                }
                finally
                {
                    Release(window);
                }
            }
        }
        catch (COMException)
        {
            // Shell refresh is best-effort; SHChangeNotify remains the primary signal.
        }
        catch (TargetInvocationException)
        {
            // Explorer may be restarting or unavailable during sign-in/sign-out.
        }
        catch (InvalidOperationException)
        {
            // An incomplete Shell object must not make folder coloring fail.
        }
        finally
        {
            Release(windows);
            Release(shell);
        }
    }

    private static object? Invoke(object target, string member, params object[] arguments) =>
        target.GetType().InvokeMember(
            member,
            BindingFlags.InvokeMethod,
            binder: null,
            target,
            arguments,
            System.Globalization.CultureInfo.InvariantCulture);

    private static object? Get(object target, string member) =>
        target.GetType().InvokeMember(
            member,
            BindingFlags.GetProperty,
            binder: null,
            target,
            args: null,
            System.Globalization.CultureInfo.InvariantCulture);

    private static void Release(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }
}
