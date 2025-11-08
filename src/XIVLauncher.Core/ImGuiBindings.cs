using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.SDL3;
using Hexa.NET.SDL3;

using HexaGen.Runtime;

using Serilog;

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using ImSDLEvent = Hexa.NET.ImGui.Backends.SDL3.SDLEvent;
using ImSDLGPUCommandBuffer = Hexa.NET.ImGui.Backends.SDL3.SDLGPUCommandBuffer;
using ImSDLGPUDevice = Hexa.NET.ImGui.Backends.SDL3.SDLGPUDevice;
using ImSDLGPURenderPass = Hexa.NET.ImGui.Backends.SDL3.SDLGPURenderPass;
using ImSDLWindow = Hexa.NET.ImGui.Backends.SDL3.SDLWindow;
using SDLEvent = Hexa.NET.SDL3.SDLEvent;
using SDLGPUDevice = Hexa.NET.SDL3.SDLGPUDevice;
using SDLGPUGraphicsPipeline = Hexa.NET.ImGui.Backends.SDL3.SDLGPUGraphicsPipeline;
using SDLRenderer = Hexa.NET.SDL3.SDLRenderer;
using SDLWindow = Hexa.NET.SDL3.SDLWindow;


// Veldrid objects are setup in method called by constructor
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace XIVLauncher.Core;

/// <summary>
/// A modified version of Veldrid.ImGui's ImGuiRenderer.
/// Manages input for ImGui and handles rendering ImGui's DrawLists with Veldrid.
/// </summary>
public class ImGuiBindings : IDisposable
{
    private bool frameBegun;

    // Hexa.NET objects
    private unsafe SDLWindow* window;
    private unsafe SDLGPUDevice* device;
    private unsafe SDLGPUTransferBuffer* transferBuffer = null!;
    private uint uploadSize = 0;
    private Vector4 clearColor = new(0.45f, 0.55f, 0.60f, 1.00f);

    private IntPtr fontAtlasID = (IntPtr)1;
    private bool controlDown;
    private bool shiftDown;
    private bool altDown;
    private bool winKeyDown;

    private IntPtr iniPathPtr;

    private int windowWidth;
    private int windowHeight;
    private Vector2 scaleFactor = Vector2.One;

    private readonly List<Tuple<Pointer<SDLGPUTexture>, Pointer<SDLSurface>>> texturesToBind = [];
    private readonly List<TextureWrap> texturesReferenced = [];
    private int lastAssignedID = 100;

    private delegate void SetClipboardTextDelegate(IntPtr userData, string text);

    private delegate string GetClipboardTextDelegate();

    // variables because they need to exist for the program duration without being gc'd
    private SetClipboardTextDelegate setText;
    private GetClipboardTextDelegate getText;

    /// <summary>
    /// Constructs a new ImGuiController.
    /// </summary>
    public unsafe ImGuiBindings(SDLWindow* window, SDLGPUDevice* device, FileInfo iniPath, float fontPxSize, float mainScale)
    {
        this.window = window;
        this.device = device;
        var ctx = ImGui.CreateContext();
        ImGui.SetCurrentContext(ctx);
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.NavEnableGamepad;
        io.BackendFlags |= ImGuiBackendFlags.HasGamepad;
#if DEBUG
        io.ConfigDebugIsDebuggerPresent = Debugger.IsAttached;
        io.ConfigErrorRecovery = true;
        io.ConfigErrorRecoveryEnableAssert = false;
        io.ConfigErrorRecoveryEnableDebugLog = false;
        io.ConfigErrorRecoveryEnableTooltip = true;
#endif
        this.SetIniPath(iniPath.FullName);
        var platformIo = ImGui.GetPlatformIO();
        platformIo.PlatformSetClipboardTextFn = (void*)Marshal.GetFunctionPointerForDelegate(SetClipboardText);
        platformIo.PlatformGetClipboardTextFn = (void*)Marshal.GetFunctionPointerForDelegate(GetClipboardText);
        platformIo.PlatformClipboardUserData = (void*)IntPtr.Zero;

        var style = ImGui.GetStyle();
        style.ScaleAllSizes(mainScale);
        style.FontScaleDpi = mainScale;
        io.ConfigDpiScaleFonts = true;
        io.ConfigDpiScaleViewports = true;

        if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
        {
            style.WindowRounding = 0;
            style.Colors[(int)ImGuiCol.WindowBg].W = 1;
        }

        ImGuiImplSDL3.SetCurrentContext(ctx);
        ImGuiImplSDL3.InitForSDLGPU((ImSDLWindow*)window);

        ImGuiImplSDLGPU3InitInfo initInfo = new()
        {
            Device = (ImSDLGPUDevice*)device,
            ColorTargetFormat = (int)SDL.GetGPUSwapchainTextureFormat(device, window),
            MSAASamples = (int)SDLGPUSampleCount.Samplecount1
        };
        ImGuiImplSDL3.SDLGPU3Init(&initInfo);

        var fontMr = new FontManager();
        fontMr.SetupFonts(fontPxSize);
    }

    private void SetIniPath(string iniPath)
    {
        if (this.iniPathPtr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(this.iniPathPtr);
        }

        this.iniPathPtr = Marshal.StringToHGlobalAnsi(iniPath);

        unsafe
        {
            var io = ImGui.GetIO();
            io.IniFilename = (byte*)this.iniPathPtr.ToPointer();
        }
    }


    private static unsafe void SetClipboardText(ImGuiContext* ctx, byte* text)
    {
        SDL.SetClipboardText(text);
    }

    private static unsafe string GetClipboardText(ImGuiContext* ctx)
    {
        return SDL.GetClipboardTextS();
    }

    public unsafe void Dispose()
    {
        lock (this.texturesReferenced)
        {
            var textures = this.texturesReferenced.ToArray();
            foreach (var texture in textures)
            {
                texture.Dispose();
            }
        }

        SDL.WaitForGPUIdle(this.device);
        ImGuiImplSDL3.Shutdown();
        ImGuiImplSDL3.SDLGPU3Shutdown();
        ImGui.DestroyContext();
    }

    public unsafe bool ProcessExit()
    {
        SDLEvent e;
        while (SDL.PollEvent(&e))
        {
            ImGuiImplSDL3.ProcessEvent((ImSDLEvent*)&e);
            var type = (SDLEventType)e.Type;
            if (type == SDLEventType.Quit || (type == SDLEventType.WindowCloseRequested &&
                                              e.Window.WindowID == SDL.GetWindowID(this.window)))
                return true;
        }

        return false;
    }

    public void NewFrame()
    {
        ImGuiImplSDL3.SDLGPU3NewFrame();
        ImGuiImplSDL3.NewFrame();
        ImGui.NewFrame();
    }

    private unsafe void CreateTransferBuffer(uint size)
    {
        if (this.transferBuffer != null)
            SDL.ReleaseGPUTransferBuffer(this.device, this.transferBuffer);
        this.uploadSize = size;
        var transferCreateInfo = new SDLGPUTransferBufferCreateInfo
        {
            Size = this.uploadSize,
            Props = 0,
            Usage = SDLGPUTransferBufferUsage.Upload
        };

        this.transferBuffer = SDL.CreateGPUTransferBuffer(this.device, &transferCreateInfo);
    }

    public unsafe void Render()
    {
        ImGui.Render();
        ImDrawData* drawData = ImGui.GetDrawData();
        var isMinimized = drawData->DisplaySize.X <= 0 || drawData->DisplaySize.Y <= 0;

        var commandBuffer = SDL.AcquireGPUCommandBuffer(this.device);
        lock (this.texturesToBind)
        {
            var copyPass = SDL.BeginGPUCopyPass(commandBuffer);
            foreach (var (texture, surface) in this.texturesToBind)
            {
                var size = (uint)(surface.Handle->Pitch * surface.Handle->H);
                if (this.uploadSize < size)
                    this.CreateTransferBuffer(size);
                var transferBufferPtr = SDL.MapGPUTransferBuffer(Program.GPUDevice, this.transferBuffer, true);
                new Span<byte>(surface.Handle->Pixels, (int)size).CopyTo(new Span<byte>(transferBufferPtr, (int)size));
                SDL.UnmapGPUTransferBuffer(Program.GPUDevice, this.transferBuffer);
                var transferInfo = new SDLGPUTextureTransferInfo
                {
                    TransferBuffer = this.transferBuffer,
                    PixelsPerRow = 0,
                    RowsPerLayer = 0
                };
                var textureRegion = new SDLGPUTextureRegion
                {
                    Texture = texture,
                    X = 0,
                    Y = 0,
                    W = (uint)surface.Handle->W,
                    H = (uint)surface.Handle->H,
                    Z = 0,
                    D = 1,
                    Layer = 0,
                    MipLevel = 0
                };
                SDL.UploadToGPUTexture(copyPass, &transferInfo, &textureRegion, true);
            }
            SDL.EndGPUCopyPass(copyPass);
            this.texturesToBind.Clear();
        }

        SDLGPUTexture* swapTexture;
        SDL.AcquireGPUSwapchainTexture(commandBuffer, this.window, &swapTexture, null, null);

        if (swapTexture != null && !isMinimized)
        {
            ImGuiImplSDL3.SDLGPU3PrepareDrawData(drawData, (ImSDLGPUCommandBuffer*)commandBuffer);

            SDLGPUColorTargetInfo targetInfo = new()
            {
                Texture = swapTexture,
                ClearColor = new SDLFColor
                {
                    R = this.clearColor.X,
                    G = this.clearColor.Y,
                    B = this.clearColor.Z,
                    A = this.clearColor.W
                },
                LoadOp = SDLGPULoadOp.Clear,
                StoreOp = SDLGPUStoreOp.Store,
                MipLevel = 0,
                LayerOrDepthPlane = 0,
                Cycle = 0
            };

            var renderPass = SDL.BeginGPURenderPass(commandBuffer, &targetInfo, 1, null);
            ImGuiImplSDL3.SDLGPU3RenderDrawData(drawData, (ImSDLGPUCommandBuffer*)commandBuffer, (ImSDLGPURenderPass*)renderPass, (SDLGPUGraphicsPipeline*)null);
            SDL.EndGPURenderPass(renderPass);
        }

        var io = ImGui.GetIO();

        if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
        {
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault();
        }

        SDL.SubmitGPUCommandBuffer(commandBuffer);
    }

    public unsafe void AddTextureCreator(SDLGPUTexture* gpuTexture, SDLSurface* surface)
    {
        lock (this.texturesToBind)
        {
            this.texturesToBind.Add(Tuple.Create(new Pointer<SDLGPUTexture>(gpuTexture), new Pointer<SDLSurface>(surface)));
        }
    }

    public void RefTexture(TextureWrap wrap)
    {
        lock (this.texturesReferenced)
        {
            this.texturesReferenced.Add(wrap);
        }
    }

    public void DerefTexture(TextureWrap wrap)
    {
        lock (this.texturesReferenced)
        {
            this.texturesReferenced.Remove(wrap);
        }
    }
}
