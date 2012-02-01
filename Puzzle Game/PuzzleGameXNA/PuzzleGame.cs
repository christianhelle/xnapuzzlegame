using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PuzzleGameXNA.StateManager;
using PuzzleGameXNA.Screens;

namespace PuzzleGameXNA
{
    public class PuzzleGame : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        ScreenManager screenManager;

        public PuzzleGame()
        {
            Content.RootDirectory = "Content";

            graphics = new GraphicsDeviceManager(this);
//#if WINDOWS_PHONE
//            graphics.IsFullScreen = true;
//#endif
            TargetElapsedTime = TimeSpan.FromTicks(333333);

            //InitializePortraitGraphics();
            //InitializeLandscapeGraphics();

            screenManager = new ScreenManager(this);
            Components.Add(screenManager);

#if WINDOWS_PHONE
            if (!screenManager.DeserializeState())
            {
                screenManager.AddScreen(GameScreenFactory.Create<BackgroundScreen>(), null);
                screenManager.AddScreen(GameScreenFactory.Create<MainMenuScreen>(), null);
            }
#else            
            screenManager.AddScreen(GameScreenFactory.Create<BackgroundScreen>(), null);
            screenManager.AddScreen(GameScreenFactory.Create<MainMenuScreen>(), null);
#endif
        }

        //private void InitializePortraitGraphics()
        //{
        //    graphics.PreferredBackBufferWidth = 480;
        //    graphics.PreferredBackBufferHeight = 800;
        //}

        //private void InitializeLandscapeGraphics()
        //{
        //    graphics.PreferredBackBufferWidth = 800;
        //    graphics.PreferredBackBufferHeight = 480;
        //}
        
#if WINDOWS_PHONE
        protected override void OnExiting(object sender, System.EventArgs args)
        {
            screenManager.SerializeState();
            base.OnExiting(sender, args);
        }
#endif

        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.Black);
            base.Draw(gameTime);
        }
    }
}
