using System.Numerics;
using System.Runtime.InteropServices;

using Hexa.NET.ImGui;
using Hexa.NET.SDL3;
using Hexa.NET.SDL3.Image;

namespace XIVLauncher.Core;

public class TextureWrap : IDisposable, IEquatable<TextureWrap>
{
    public ImTextureRef ImGuiHandle { get; }
    private readonly unsafe ImTextureData* textureData;

    private readonly unsafe SDLGPUTexture* deviceTexture;
    private readonly unsafe SDLSurface* surface;

    public unsafe uint Width => (uint)this.surface->W;

    public unsafe uint Height => (uint)this.surface->H;

    public Vector2 Size => new(this.Width, this.Height);

    protected TextureWrap(byte[] data)
    {
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

        Program.ImGuiBindings.RefTexture(this);
    }

    public static TextureWrap Load(byte[] data) => new(data);

    public void Dispose()
    {
        unsafe
        {
            SDL.ReleaseGPUTexture(Program.GPUDevice, this.deviceTexture);
            Marshal.FreeHGlobal((nint)this.textureData);
            SDL.DestroySurface(this.surface);
        }

        Program.ImGuiBindings.DerefTexture(this);
    }

    public unsafe bool Equals(TextureWrap? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return this.textureData == other.textureData && this.deviceTexture == other.deviceTexture && this.surface == other.surface && this.ImGuiHandle.Equals(other.ImGuiHandle);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return this.Equals((TextureWrap)obj);
    }

    public override unsafe int GetHashCode()
    {
        return HashCode.Combine(unchecked((int)(long)this.textureData), unchecked((int)(long)this.deviceTexture), unchecked((int)(long)this.surface), this.ImGuiHandle);
    }
}
