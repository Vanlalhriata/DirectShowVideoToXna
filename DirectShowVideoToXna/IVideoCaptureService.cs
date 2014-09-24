using Microsoft.Xna.Framework.Graphics;

namespace DirectShowVideoToXna
{
    public interface IVideoCaptureService
    {
        Texture2D TextureRGBA { get; }
        byte[] BufferGray { get; }
    }
}
