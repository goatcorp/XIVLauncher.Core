using System.Runtime.InteropServices;

using Hexa.NET.ImGui;

namespace XIVLauncher.Core;

public class FontManager
{
    private const float FONT_GAMMA = 1.4f;
    private const string TEXT_FONT_NAME = "NotoSansCJKjp-Regular.otf";
    private const string ICON_FONT_NAME = "FontAwesome5FreeSolid.otf";

    public static ImFontPtr TextFont { get; private set; }
    public static ImFontPtr IconFont { get; private set; }

    public unsafe void SetupFonts(float pxSize)
    {
        var ioFonts = ImGui.GetIO().Fonts;

        ImGui.GetIO().Fonts.Clear();

        var fontConfig = ImGui.ImFontConfig();
        fontConfig.PixelSnapH = true;

        var fontDataText = AppUtil.GetEmbeddedResourceBytes(TEXT_FONT_NAME);
        var fontDataIcons = AppUtil.GetEmbeddedResourceBytes(ICON_FONT_NAME);

        var fontDataTextPtr = Marshal.AllocHGlobal(fontDataText.Length);
        Marshal.Copy(fontDataText, 0, fontDataTextPtr, fontDataText.Length);

        var fontDataIconsPtr = Marshal.AllocHGlobal(fontDataIcons.Length);
        Marshal.Copy(fontDataIcons, 0, fontDataIconsPtr, fontDataIcons.Length);

        var japaneseRangeHandle = GCHandle.Alloc(GlyphRangesJapanese.GlyphRanges, GCHandleType.Pinned);

        TextFont = ioFonts.AddFontFromMemoryTTF(
            (void*)fontDataTextPtr,
            fontDataText.Length,
            pxSize,
            null,
            (uint*)japaneseRangeHandle.AddrOfPinnedObject());

        var iconRangeHandle = GCHandle.Alloc(
            new ushort[]
            {
                0xE000,
                0xF8FF,
                0,
            },
            GCHandleType.Pinned);

        IconFont = ioFonts.AddFontFromMemoryTTF(

            (void*)fontDataIconsPtr,
            fontDataIcons.Length,
            pxSize,
            fontConfig,
            (uint*)iconRangeHandle.AddrOfPinnedObject());

        fontConfig.Destroy();
        japaneseRangeHandle.Free();
        iconRangeHandle.Free();
    }
}
