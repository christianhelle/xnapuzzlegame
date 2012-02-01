using System;
using Microsoft.Xna.Framework;
using PuzzleGameXNA.StateManager;

namespace PuzzleGameXNA.Screens
{
    class InGameOptionsScreen : MenuScreen
    {
        GameplayScreen gameplayScreen;

        public InGameOptionsScreen()
            : this(GameScreenFactory.Create<GameplayScreen>())
        {
        }

        public InGameOptionsScreen(GameplayScreen gameplayScreen)
            : base("Options")
        {
            this.gameplayScreen = gameplayScreen;

            //var preview = new MenuEntry("Preview image");
            var quitGame = new MenuEntry("Return to the main menu");
            var resumeGame = new MenuEntry("Resume my game");

            //preview.Selected += new EventHandler<PlayerIndexEventArgs>(OnPreviewSelected);
            quitGame.Selected += new EventHandler<PlayerIndexEventArgs>(OnQuitGameSelected);
            resumeGame.Selected += new EventHandler<PlayerIndexEventArgs>(OnResumeGameSelected);

            //MenuEntries.Add(preview);
            MenuEntries.Add(resumeGame);
            MenuEntries.Add(quitGame);

            TransitionOffTime = TimeSpan.Zero;
            TransitionOnTime = TimeSpan.Zero;
        }

        //void OnPreviewSelected(object sender, PlayerIndexEventArgs e)
        //{
        //    LoadingScreen.Load(ScreenManager, true, e.PlayerIndex, new PreviewScreen(gameplayScreen));
        //}

        void OnResumeGameSelected(object sender, PlayerIndexEventArgs e)
        {
            LoadingScreen.Load(ScreenManager, true, e.PlayerIndex, gameplayScreen);
        }

        void OnQuitGameSelected(object sender, PlayerIndexEventArgs e)
        {
            LoadingScreen.Load(ScreenManager, true, e.PlayerIndex, GameScreenFactory.Create<BackgroundScreen>(), GameScreenFactory.Create<MainMenuScreen>());
        }

        protected override void OnCancel(PlayerIndex playerIndex)
        {
            LoadingScreen.Load(ScreenManager, true, playerIndex, gameplayScreen);
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
