using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Serilog;
using Veldrid.Sdl2;

namespace XIVLauncher.Core;

public static unsafe partial class SdlHelpers
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate byte* SDL_GetCurrentVideoDriver_t();
    private static SDL_GetCurrentVideoDriver_t s_sdl_getCurrentVideoDriver =
        Sdl2Native.LoadFunction<SDL_GetCurrentVideoDriver_t>("SDL_GetCurrentVideoDriver");
    private static byte* SDL_GetCurrentVideoDriver() => s_sdl_getCurrentVideoDriver();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SDL_GetDisplayDPI_t(int displayIndex, float* ddpi, float* hdpi, float* vdpi);
    private static SDL_GetDisplayDPI_t s_sdl_getDisplayDPI =
        Sdl2Native.LoadFunction<SDL_GetDisplayDPI_t>("SDL_GetDisplayDPI");
    private static int SDL_GetDisplayDPI(int displayIndex, float* ddpi, float* hdpi, float* vdpi)
        => s_sdl_getDisplayDPI(displayIndex, ddpi, hdpi, vdpi);

    private static unsafe string GetString(byte* stringStart)
    {
        int characters = 0;
        while (stringStart[characters] != 0)
        {
            characters++;
        }

        return Encoding.UTF8.GetString(stringStart, characters);
    }

    public static string GetCurrentVideoDriver()
    {
        return GetString(SDL_GetCurrentVideoDriver());
    }

    public static Vector2 GetDisplayDpiScale()
    {
        float ddpi, hdpi, vdpi;

        if (SDL_GetDisplayDPI(0, &ddpi, &hdpi, &vdpi) < 0)
        {
            Log.Warning("Cannot determine display DPI scale, defaulting to 1.0: {0}",
                GetString(Sdl2Native.SDL_GetError()));
            return new Vector2(1.0f, 1.0f);
        }

        return new Vector2(hdpi / 96, vdpi / 96);
    }
}
