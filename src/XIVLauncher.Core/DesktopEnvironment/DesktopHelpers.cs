using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

using Serilog;

namespace XIVLauncher.Core.DesktopEnvironment;

public partial class DesktopHelpers
{
    [GeneratedRegex(@"^uint32\s+([0-9\.]+)$")]
    private static partial Regex GSettingsRegex();

    [GeneratedRegex(@"^Xft\.dpi:\s+([0-9]+)$")]
    private static partial Regex XrdbRegex();


    public static float GetDesktopScaleFactor()
    {
        if (Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") == "wayland"
            && SdlHelpers.GetCurrentVideoDriver() == "x11")
        {
            return GetXWaylandScaleFactor();
        }

        return 1f;
    }

    private static float GetXWaylandScaleFactor()
    {
        float? scaleFactor;

        if (Environment.GetEnvironmentVariable("XDG_SESSION_DESKTOP") == "KDE")
        {
            scaleFactor = GetKScreenDoctorScaleFactor();

            if (scaleFactor != null)
            {
                return (float)scaleFactor;
            }
        }

        scaleFactor = GetGSettingsScaleFactor();

        if (scaleFactor != null)
        {
            return (float)scaleFactor;
        }

        scaleFactor = GetXrdbScaleFactor();

        if (scaleFactor != null)
        {
            return (float)scaleFactor;
        }

        // Fallback
        return 1f;
    }

    private static float? GetKScreenDoctorScaleFactor()
    {
        try
        {
            Log.Verbose("KDE detected - obtaining desktop scale from kscreen-doctor");

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "kscreen-doctor",
                Arguments = "-j",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });

            if (process == null)
            {
                return null;
            }

            var screenData = JsonSerializer.Deserialize<KScreenDoctorData>(process.StandardOutput.BaseStream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (screenData != null)
            {
                foreach (var output in screenData.Outputs)
                {
                    if (output.Pos.X == 0 && output.Pos.Y == 0)
                    {
                        // Primary monitor
                        Log.Verbose("Obtained scale from kscreen-doctor: {0}", output.Scale);
                        return output.Scale;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Cannot obtain desktop scale from kscreen-doctor - trying other methods");
        }

        return null;
    }

    private static float? GetGSettingsScaleFactor()
    {
        try
        {
            Log.Verbose("Obtaining desktop scale from gsettings");
            
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "gsettings",
                Arguments = "get org.gnome.desktop.interface scaling-factor",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });

            if (process == null)
            {
                return null;
            }

            string? line;

            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                var match = GSettingsRegex().Match(line);
                
                if (match != null && match.Success)
                {
                    return float.Parse(match.Groups[1].Value);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Cannot obtain desktop scale from gsettings - trying other methods");
        }

        return null;
    }

    private static float? GetXrdbScaleFactor()
    {
        try
        {
            Log.Verbose("Obtaining desktop scale from xrdb");
            
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "xrdb",
                Arguments = "-query",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });

            if (process == null)
            {
                return null;
            }

            string? line;

            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                var match = XrdbRegex().Match(line);
                
                if (match != null && match.Success)
                {
                    return int.Parse(match.Groups[1].Value) / 96f;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Cannot obtain desktop scale from gsettings - trying other methods");
        }

        return null;
    }

    // JSON data classes

    private class KScreenDoctorData
    {
        public List<KScreenDoctorOutput> Outputs { get; set; } = new();
    }

    private class KScreenDoctorOutput
    {
        public float Scale { get; set; }
        public Point Pos { get; set; }
    }

    private struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
