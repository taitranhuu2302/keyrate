using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32;

internal static class Program
{
    // Must match WinUser SPI_* values (see SystemParametersInfo docs).
    private const uint SpiGetfilterkeys = 0x0032;
    private const uint SpiSetfilterkeys = 0x0033;

    private const uint FkfFilterkeyson = 0x00000001;
    private const uint FkfAvailable = 0x00000002;

    private const string AppName = "KeyRateApp";
    private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private const uint DefaultDelayMs = 180;
    private const uint DefaultRepeatMs = 8;

    private static uint _lastDelayMs = DefaultDelayMs;
    private static uint _lastRepeatMs = DefaultRepeatMs;

    public static int Main(string[] args)
    {
        if (args.Length == 2
            && TryParseUInt(args[0], out var delayArg)
            && TryParseUInt(args[1], out var repeatArg))
        {
            return ApplyFilterKeys(delayArg, repeatArg, quiet: true) ? 0 : 1;
        }

        RunMenu();
        return 0;
    }

    private static void RunMenu()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("=== KeyRate — keyboard repeat (Filter Keys) ===");
            Console.WriteLine($"Last applied: delay {_lastDelayMs} ms, repeat {_lastRepeatMs} ms");
            Console.WriteLine();
            Console.WriteLine("  1  Apply custom delay & repeat");
            Console.WriteLine("  2  Preset: Fast (delay 180 ms, repeat 8 ms)");
            Console.WriteLine("  3  Preset: Moderate (delay 250 ms, repeat 25 ms)");
            Console.WriteLine("  4  Preset: Slower (delay 500 ms, repeat 33 ms)");
            Console.WriteLine("  5  Show current Windows filter-keys settings");
            Console.WriteLine("  6  Add to Windows startup (re-applies last preset above on sign-in)");
            Console.WriteLine("  7  Remove from Windows startup");
            Console.WriteLine("  0  Exit");
            Console.WriteLine();
            Console.Write("Choose an option: ");

            var choice = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    PromptCustomValues();
                    break;
                case "2":
                    ApplyPreset(DefaultDelayMs, DefaultRepeatMs, "Fast");
                    break;
                case "3":
                    ApplyPreset(250, 25, "Moderate");
                    break;
                case "4":
                    ApplyPreset(500, 33, "Slower");
                    break;
                case "5":
                    PrintCurrentFilterKeys();
                    break;
                case "6":
                    AddToStartup(_lastDelayMs, _lastRepeatMs);
                    break;
                case "7":
                    RemoveFromStartup();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Unknown option.");
                    break;
            }
        }
    }

    private static void PromptCustomValues()
    {
        Console.WriteLine("Suggestions:");
        Console.WriteLine("  Delay before repeat starts (ms): common panel values 250, 500, 750, 1000; faster ~180–220.");
        Console.WriteLine("  Repeat interval (ms): smaller = faster repeat while holding a key; often 8–33.");
        Console.WriteLine();

        Console.Write($"Delay ms [{_lastDelayMs}]: ");
        var delayLine = Console.ReadLine()?.Trim();
        if (!TryParseUInt(delayLine, out var delayMs))
            delayMs = _lastDelayMs;

        Console.Write($"Repeat ms [{_lastRepeatMs}]: ");
        var repeatLine = Console.ReadLine()?.Trim();
        if (!TryParseUInt(repeatLine, out var repeatMs))
            repeatMs = _lastRepeatMs;

        ApplyFilterKeys(delayMs, repeatMs, quiet: false);
    }

    private static void ApplyPreset(uint delayMs, uint repeatMs, string label)
    {
        Console.WriteLine($"Applying preset \"{label}\": delay {delayMs} ms, repeat {repeatMs} ms.");
        ApplyFilterKeys(delayMs, repeatMs, quiet: false);
    }

    private static bool ApplyFilterKeys(uint delayMs, uint repeatMs, bool quiet)
    {
        var fkSize = (uint)Marshal.SizeOf<FilterKeys>();
        var fk = new FilterKeys
        {
            cbSize = fkSize,
            dwFlags = FkfFilterkeyson | FkfAvailable,
            iWaitMSec = 0,
            iDelayMSec = delayMs,
            iRepeatMSec = repeatMs,
            iBounceMSec = 0,
        };

        if (!SystemParametersInfo(SpiSetfilterkeys, fkSize, ref fk, 0))
        {
            var err = Marshal.GetLastWin32Error();
            Console.Error.WriteLine($"SystemParametersInfo failed (Win32 error {err}). Unable to set keyrate.");
            return false;
        }

        _lastDelayMs = delayMs;
        _lastRepeatMs = repeatMs;

        if (!quiet)
            Console.WriteLine($"OK — delay {delayMs} ms, repeat {repeatMs} ms.");

        return true;
    }

    private static void PrintCurrentFilterKeys()
    {
        var fkSize = (uint)Marshal.SizeOf<FilterKeys>();
        var fk = new FilterKeys { cbSize = fkSize };

        if (!SystemParametersInfo(SpiGetfilterkeys, fkSize, ref fk, 0))
        {
            Console.Error.WriteLine($"Could not read filter keys (Win32 error {Marshal.GetLastWin32Error()}).");
            return;
        }

        Console.WriteLine($"cbSize={fk.cbSize}, flags=0x{fk.dwFlags:X}");
        Console.WriteLine($"iWaitMSec={fk.iWaitMSec}, iDelayMSec={fk.iDelayMSec}, iRepeatMSec={fk.iRepeatMSec}, iBounceMSec={fk.iBounceMSec}");
        Console.WriteLine($"FKF_FILTERKEYSON={(fk.dwFlags & FkfFilterkeyson) != 0}, FKF_AVAILABLE={(fk.dwFlags & FkfAvailable) != 0}");
    }

    private static void AddToStartup(uint delayMs, uint repeatMs)
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            Console.Error.WriteLine("Cannot resolve executable path.");
            return;
        }

        var startupCommand = exePath.Contains(' ', StringComparison.Ordinal)
            ? $"\"{exePath}\" {delayMs} {repeatMs}"
            : $"{exePath} {delayMs} {repeatMs}";

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: true)
                           ?? Registry.CurrentUser.CreateSubKey(RunRegistryPath);

            key?.SetValue(AppName, startupCommand, RegistryValueKind.String);
            Console.WriteLine($"Added to startup for current user (re-applies delay {delayMs} ms, repeat {repeatMs} ms).");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to write startup registry value: {ex.Message}");
        }
    }

    private static void RemoveFromStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: true);
            if (key?.GetValue(AppName) is null)
            {
                Console.WriteLine("No startup entry found.");
                return;
            }

            key.DeleteValue(AppName, throwOnMissingValue: false);
            Console.WriteLine("Removed from startup.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to remove startup registry value: {ex.Message}");
        }
    }

    private static bool TryParseUInt(string? s, out uint value)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            value = default;
            return false;
        }

        return uint.TryParse(s.Trim(), System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FilterKeys
    {
        public uint cbSize;
        public uint dwFlags;
        public uint iWaitMSec;
        public uint iDelayMSec;
        public uint iRepeatMSec;
        public uint iBounceMSec;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref FilterKeys pvParam, uint fWinIni);
}
