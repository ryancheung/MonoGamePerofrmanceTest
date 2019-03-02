using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Game1
{
    public class Game1 : Game
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly GraphicsDeviceManager _graphicsDeviceManager;
        private SpriteBatch _spriteBatch;

        public PresentationParameters Parameters { get; private set; }

        public RenderTarget2D CurrentRenderTarget { get; private set; } = null;

        public float Opacity { get; private set; } = 1F;

        public bool Blending { get; private set; }
        public float BlendRate { get; private set; } = 1F;
        public GraphicsDevice Device {  get { return this.GraphicsDevice; } }
        public SpriteBatch Sprite {  get { return _spriteBatch; } }

        Texture2D _testTex;


        public Game1()
        {
            _graphicsDeviceManager = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            Parameters = new PresentationParameters
            {
                IsFullScreen = false,
                BackBufferFormat = SurfaceFormat.Color,
                DepthStencilFormat = DepthFormat.Depth24,
                DeviceWindowHandle = Window.Handle,
                PresentationInterval = false ? PresentInterval.Default : PresentInterval.Immediate,
                BackBufferWidth = 1024,
                BackBufferHeight = 768,
                RenderTargetUsage = RenderTargetUsage.PreserveContents,
            };

            _graphicsDeviceManager.PreparingDeviceSettings += (sender, e) =>
            {
                e.GraphicsDeviceInformation.PresentationParameters = Parameters;
            };
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            if (_customEffect == null)
            {
                _customEffect = new AlphaTestEffect(GraphicsDevice);
                _customEffect.AlphaFunction = CompareFunction.Greater;
                _customEffect.VertexColorEnabled = true;
            }

            _customEffect.Projection = Matrix.CreateOrthographicOffCenter(0, Parameters.BackBufferWidth, Parameters.BackBufferHeight, 0, 0, -1);

            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _testTex = Content.Load<Texture2D>("test");

            GC.Collect();
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Escape))
                Exit();


            base.Update(gameTime);
        }

        public String GetMetrics()
        {
            return String.Format("ClearCount:{0}, DrawCount:{1}, PixelShaderCount:{2}, VeterxShaderCount:{3}, PrimitiveCount:{4}, SpriteCount:{5}, TargetCount:{6}, TextureCount:{7}",
                GraphicsDevice.Metrics.ClearCount,
                GraphicsDevice.Metrics.DrawCount,
                GraphicsDevice.Metrics.PixelShaderCount,
                GraphicsDevice.Metrics.VertexShaderCount,
                GraphicsDevice.Metrics.PrimitiveCount,
                GraphicsDevice.Metrics.SpriteCount,
                GraphicsDevice.Metrics.TargetCount,
                GraphicsDevice.Metrics.TextureCount);
        }

        private AlphaTestEffect _customEffect;

        void DisableAlphaTest()
        {
            _spriteBatch.CustomEffect = null;
        }

        void EnableAlphaTest()
        {
            _spriteBatch.CustomEffect = _customEffect;
        }

        private Matrix _customTransform;
        public Matrix SpriteTransform
        {
            set
            {
                Sprite.Flush();
                _customTransform = value;

                _customEffect.View = _customTransform;
                Sprite.Transform = _customTransform;
            }
        }

        public void SetRenderTarget(RenderTarget2D renderTarget)
        {
            if (CurrentRenderTarget == renderTarget) return;

            Sprite.Flush();

            CurrentRenderTarget = renderTarget;
            Device.SetRenderTarget(renderTarget);
        }

        public void SetOpacity(float opacity)
        {
            if (Opacity == opacity)
                return;

            Sprite.Flush();

            var newBlendState = Device.BlendState.Clone();
            if (opacity >= 1 || opacity < 0)
            {
                newBlendState.ColorSourceBlend = Blend.SourceAlpha;
                newBlendState.ColorDestinationBlend = Blend.InverseSourceAlpha;
                newBlendState.AlphaSourceBlend = Blend.One;
                newBlendState.BlendFactor = new Color(255, 255, 255, 255);
                Device.BlendState = newBlendState;
                DisableAlphaTest();
            }
            else
            {
                EnableAlphaTest();
                newBlendState.ColorSourceBlend = Blend.BlendFactor;
                newBlendState.ColorDestinationBlend = Blend.InverseBlendFactor;
                newBlendState.AlphaSourceBlend = Blend.SourceAlpha;
                newBlendState.BlendFactor = new Color((byte)(255 * opacity), (byte)(255 * opacity), (byte)(255 * opacity), (byte)(255 * opacity));
                Device.BlendState = newBlendState;
            }
            
            Opacity = opacity;
            Sprite.Flush();
        }
        public void SetBlend(bool value, float rate = 1F)
        {
            if (value == Blending) return;

            Blending = value;
            BlendRate = 1F;
            Sprite.Flush();

            Sprite.End();
            if (Blending)
            {
                Sprite.Begin(SpriteFlags.DoNotSaveState);
                EnableAlphaTest();

                var newBlendState = Device.BlendState.Clone();
                newBlendState.ColorSourceBlend = Blend.BlendFactor;
                newBlendState.ColorDestinationBlend = Blend.One;
                newBlendState.BlendFactor = new Color((byte)(255 * rate), (byte)(255 * rate), (byte)(255 * rate), (byte)(255 * rate));
                Device.BlendState = newBlendState;
            }
            else
            {
                Sprite.Begin(SpriteFlags.AlphaBlend);
            }
            

            Device.SetRenderTarget(CurrentRenderTarget);
        }

        void RenderGame()
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            _spriteBatch.Draw(_testTex, new Vector2(0, 0), Color.White);

            for (int i = 0; i < 500; ++i) {
                Random rand = new Random();

                int posX = (int)(rand.NextDouble() * GraphicsDevice.Viewport.Width);
                int posY = (int)(rand.NextDouble() * GraphicsDevice.Viewport.Height);

                if ((float)rand.NextDouble() > 0.5F)
                {
                    SpriteTransform = Matrix.CreateScale((float)rand.NextDouble() + 0.01F);

                    SetOpacity(0.3F);
                    _spriteBatch.Draw(_testTex, new Vector2(posX, posY), Color.Red);
                    SetOpacity(1F);

                    SpriteTransform = Matrix.Identity;
                }
                else
                {

                    SetBlend(true, 0.7F);
                    _spriteBatch.Draw(_testTex, new Vector2(posX, posY), Color.Red);
                    SetBlend(false);
                }


            }
            _spriteBatch.End();
        }

        protected override void Draw(GameTime gameTime)
        {
            Stopwatch watch = Stopwatch.StartNew();

            RenderGame();

            watch.Stop();
            double time = watch.Elapsed.TotalMilliseconds;
            if (time > 10)
            {
                Console.WriteLine("====FrameLogStart====");
                Console.WriteLine(String.Format("{0} - Slow update: {1}", DateTime.Now.ToLocalTime().ToLongTimeString(), time));

                Console.WriteLine(String.Format("Metrics: {0}", GetMetrics()));
                Console.WriteLine("====FrameLogEnd====");
            }

            base.Draw(gameTime);
        }
    }
}
