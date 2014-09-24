using DirectShowVideoToXna;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Demo
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        private Rectangle fullscreen;
        private Effect swapRBEffect;

        private IVideoCaptureService videoCapture;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.IsFullScreen = false;
            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;
            graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            Components.Add(new VideoCaptureComponent(this));

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            videoCapture = (IVideoCaptureService)Services.GetService(typeof(IVideoCaptureService));
            fullscreen = new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            swapRBEffect = Content.Load<Effect>("SwapRB");
        }

        protected override void UnloadContent()
        {
            if (null != swapRBEffect)
                swapRBEffect.Dispose();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            if (null != videoCapture && null != videoCapture.TextureRGBA)
            {
                // The texture from videoCapture has to be rotated by 180 degrees (or flipped horizontally and vertically).,
                // The Red and Blue channels have to be swapped too. This is deferred to the GPU using a shader
                spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, swapRBEffect);
                spriteBatch.Draw(videoCapture.TextureRGBA, fullscreen, null, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 1f);
                spriteBatch.End();
            }

            base.Draw(gameTime);
        }
    }
}
