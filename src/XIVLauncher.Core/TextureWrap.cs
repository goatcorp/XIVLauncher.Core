using System.Numerics;

#if HEXA
using System.Runtime.InteropServices;

using Hexa.NET.ImGui;
using Hexa.NET.SDL3;
using Hexa.NET.SDL3.Image;
#endif
#if VELDRID

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using Veldrid;
using Veldrid.ImageSharp;
#endif

namespace XIVLauncher.Core;

public class TextureWrap : IDisposable
{
#if VELDRID
    public IntPtr ImGuiHandle { get; }

    private readonly Texture deviceTexture;
#endif
#if HEXA
    public ImTextureRef ImGuiHandle { get; }
    private readonly unsafe ImTextureData* textureData;

    private readonly unsafe SDLGPUTexture* deviceTexture;
    private readonly unsafe SDLSurface* surface;
#endif

    public unsafe uint Width =>
#if VELDRID
        this.deviceTexture.Width;
#endif
#if HEXA
        (uint)this.ImGuiHandle.TexData->Width;
#endif

    public unsafe uint Height =>
#if VELDRID
        this.deviceTexture.Height;
#endif
#if HEXA
        (uint)this.ImGuiHandle.TexData->Height;
#endif

    public Vector2 Size => new(this.Width, this.Height);

    protected TextureWrap(byte[] data)
    {
#if VELDRID
        var image = Image.Load<Rgba32>(data);
        var texture = new ImageSharpTexture(image, false);
        this.deviceTexture = texture.CreateDeviceTexture(Program.GraphicsDevice, Program.GraphicsDevice.ResourceFactory);

        this.ImGuiHandle = Program.ImGuiBindings.GetOrCreateImGuiBinding(Program.GraphicsDevice.ResourceFactory, this.deviceTexture);
#endif
#if HEXA
        unsafe
        {
            fixed (byte* dataPtr = data)
            {
                var ioStream = SDL.IOFromMem(dataPtr, (nuint)data.Length);
                this.surface = SDLImage.LoadIO(ioStream, true);

                // SDLGPUTextureFormat expects ABGR8888 format from SDLSurface
                if (this.surface->Format != SDLPixelFormat.Abgr8888)
                    this.surface = SDL.ConvertSurface(this.surface, SDLPixelFormat.Abgr8888);
                var createInfo = new SDLGPUTextureCreateInfo
                {
                    Width = (uint)this.surface->W,
                    Height = (uint)this.surface->H,
                    Format = SDLGPUTextureFormat.R8G8B8A8Unorm,
                    Usage = SDLGPUTextureUsageFlags.Sampler,
                    LayerCountOrDepth = 1,
                    NumLevels = 1,
                    Type = SDLGPUTextureType.Texturetype2D,
                    SampleCount = SDLGPUSampleCount.Samplecount1,
                    Props = 0
                };
                this.deviceTexture = SDL.CreateGPUTexture(Program.GPUDevice, &createInfo);
                this.textureData = (ImTextureData*)Marshal.AllocHGlobal(sizeof(ImTextureData));
                this.textureData->Height = this.surface->H;
                this.textureData->Width = this.surface->W;
                this.textureData->Format = ImTextureFormat.Rgba32;
                this.textureData->TexID = this.deviceTexture;
                this.textureData->UsedRect.H = (ushort)this.surface->H;
                this.textureData->UsedRect.W = (ushort)this.surface->W;
                this.textureData->UsedRect.X = 0;
                this.textureData->UsedRect.Y = 0;
                this.textureData->WantDestroyNextFrame = 0;
                Program.ImGuiBindings.AddTextureCreator(this.deviceTexture, this.surface);
            }
            this.ImGuiHandle = new(this.textureData, this.deviceTexture);
        }
#endif
    }

    public static TextureWrap Load(byte[] data) => new(data);

    public void Dispose()
    {
#if VELDRID
        this.deviceTexture.Dispose();
#endif
#if HEXA
        unsafe
        {
            SDL.ReleaseGPUTexture(Program.GPUDevice, this.deviceTexture);
            Marshal.FreeHGlobal((nint)this.textureData);
            SDL.DestroySurface(this.surface);
        }
#endif
    }
}
