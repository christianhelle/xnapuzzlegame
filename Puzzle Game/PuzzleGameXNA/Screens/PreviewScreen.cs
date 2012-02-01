using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PuzzleGameXNA.StateManager;

namespace PuzzleGameXNA.Screens
{
    class PreviewScreen : GameScreen
    {
        ContentManager content;
        SpriteBatch spriteBatch;
        Texture2D previewTexture, emptyTexture;
        Vector2 previewVector;
        GameplayScreen gameplayScreen;

        public PreviewScreen()
            : this(GameScreenFactory.Create<GameplayScreen>())
        {
        }

        public PreviewScreen(GameplayScreen screen)
        {
            gameplayScreen = screen;
            previewVector = new Vector2(0, 0);

            //TransitionOnTime = TimeSpan.FromSeconds(1.5);
            //TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        public override void LoadContent()
        {
            if (content == null)
                content = new ContentManager(ScreenManager.Game.Services, "Content");

            spriteBatch = ScreenManager.SpriteBatch;
            previewTexture = content.Load<Texture2D>(gameplayScreen.CurrentPuzzleImage);
            emptyTexture = new Texture2D(ScreenManager.GraphicsDevice, 1, 1);
        }

        public override void UnloadContent()
        {
            content.Unload();
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, false);
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
            spriteBatch.Draw(previewTexture, ScreenManager.GraphicsDevice.Viewport.Bounds, Color.Gray);
            spriteBatch.End();

            if (TransitionPosition > 0)
                ScreenManager.FadeBackBufferToBlack(1f - TransitionAlpha);

            base.Draw(gameTime);
        }

        public override void Deserialize(System.IO.Stream stream)
        {
            gameplayScreen.Deserialize(stream);
            base.Deserialize(stream);
        }

        public override void Serialize(System.IO.Stream stream)
        {
            gameplayScreen.Serialize(stream);
            base.Serialize(stream);
        }
    }
}
