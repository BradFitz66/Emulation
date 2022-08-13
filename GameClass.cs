using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Emulators.EmuChip8;
namespace Emulation
{
    public enum State{
        Menu,
        InEmulator
    }
    public class GameClass : Game
    {
        
        public State currentState=State.Menu;
        public static GraphicsDeviceManager _graphics{get; private set;}
        public  static SpriteBatch _spriteBatch{get; private set;}
        //Create stopwatch for input delay
        private System.Diagnostics.Stopwatch _stopwatch;
        private string[] emulatorOptions={
            "Chip8",
            "Nintendo GameBoy"
        };
        private int emulatorIndex = 0;
        SpriteFont font;

        IEmulator runningEmulator;

        public GameClass()
        {
            _stopwatch = new System.Diagnostics.Stopwatch();
            _stopwatch.Start();
            Console.WriteLine("Initializing...");
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("Fonts/Arial");
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            //Add delay to input using stopwatch
            if(currentState==State.Menu){
                if (_stopwatch.ElapsedMilliseconds > 100)
                {
                    _stopwatch.Restart();
                    if (Keyboard.GetState().IsKeyDown(Keys.Up))
                    {
                        //Decrease if there's options
                        if (emulatorIndex > 0)
                            emulatorIndex--;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.Down))
                    {
                        //Increase if there's options
                        if (emulatorIndex < emulatorOptions.Length - 1)
                            emulatorIndex++;
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                    {
                        //Start emulator
                        switch (emulatorIndex)
                        {
                            case 0:
                                Console.WriteLine("Starting Chip8...");
                                runningEmulator=new Chip8();
                                runningEmulator.Initialize();
                                //Update state
                                currentState=State.InEmulator;

                                break;
                            case 1:
                                Console.WriteLine("Starting GameBoy...");
                                break;
                        }
                    }
                }
            }
            if(currentState==State.InEmulator){
                runningEmulator.Update(gameTime);
            }
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            //Draw all menu items in a loop in center of screen with an arrow pointing to selected emulator
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise); 
            if(currentState==State.Menu){
                for (int i = 0; i < emulatorOptions.Length; i++)
                {
                    if (i == emulatorIndex)
                    {
                        _spriteBatch.DrawString(font, emulatorOptions[i]+" <", new Vector2(GraphicsDevice.Viewport.Width / 2 - font.MeasureString(emulatorOptions[i]).X / 2, GraphicsDevice.Viewport.Height / 2 - font.MeasureString(emulatorOptions[i]).Y / 2 + i * font.MeasureString(emulatorOptions[i]).Y), Color.White);
                    }
                    else
                    {
                        _spriteBatch.DrawString(font, emulatorOptions[i], new Vector2(GraphicsDevice.Viewport.Width / 2 - font.MeasureString(emulatorOptions[i]).X / 2, GraphicsDevice.Viewport.Height / 2 - font.MeasureString(emulatorOptions[i]).Y / 2 + i * font.MeasureString(emulatorOptions[i]).Y), Color.White);
                    }
                }
            }
            else{
                runningEmulator.Draw();
            }
            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
