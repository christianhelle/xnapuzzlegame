#region File Description
//-----------------------------------------------------------------------------
// GameplayScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PuzzleGameXNA.StateManager;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Diagnostics;
#endregion

namespace PuzzleGameXNA.Screens
{
    /// <summary>
    /// This screen implements the actual game logic. It is just a
    /// placeholder to get the idea across: you'll probably want to
    /// put some more interesting gameplay in here!
    /// </summary>
    class GameplayScreen : GameScreen
    {
        ContentManager content;
        SpriteBatch spriteBatch;
        Texture2D previewTexture;//, emptyTexture;
        Vector2 previewVector, totalGameTimeVector, congratulationsVector;
        Rectangle emptyPiece;
        SpriteFont gameTimerFont, congratulationsFont;
        Dictionary<int, Texture2D> puzzlePieces;
        Dictionary<int, PuzzlePiece> scrambledPieces;
        GameplayDrawMode Mode;
        int height, width;
        double elapsedTime, playingTime;
        Queue<Keys> pendingCommands;
        const int PIECE_COUNT = 4 * 4;
        bool solved;
        //static readonly object syncLock = new object();
        //static bool animating;
        InGameOptionsScreen gameOptionsScreen;
        PreviewScreen previewScreen;

        public GameplayScreen()
        {
            pendingCommands = new Queue<Keys>();
            Mode = GameplayDrawMode.Puzzle;
            previewVector = new Vector2(0, 0);
            totalGameTimeVector = new Vector2(10, 10);
            gameOptionsScreen = new InGameOptionsScreen(this);
            previewScreen = new PreviewScreen(this);

            TransitionOnTime = TimeSpan.Zero;
            TransitionOffTime = TimeSpan.Zero;
        }

        public override void LoadContent()
        {
            if (content == null)
                content = new ContentManager(ScreenManager.Game.Services, "Content");

            spriteBatch = ScreenManager.SpriteBatch;
            previewTexture = content.Load<Texture2D>(GetRandomPuzzleImage());
            gameTimerFont = content.Load<SpriteFont>("GameTimeFont");
            congratulationsFont = content.Load<SpriteFont>("CongratulationsFont");

            var size = congratulationsFont.MeasureString("Congrautulations!");
            congratulationsVector = new Vector2(
                (ScreenManager.GraphicsDevice.Viewport.Width - size.X) / 2,
                (ScreenManager.GraphicsDevice.Viewport.Height - size.Y) / 2);

            //emptyTexture = new Texture2D(ScreenManager.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            //emptyTexture.SetData<Color>(new[] { lineColor });

            Divide();
            if (scrambledPieces == null)
                Scramble();
            ScreenManager.Game.ResetElapsedTime();
        }

        readonly string[] puzzles = { "Chrysanthemum", "Desert", "Hydrangeas", "Jellyfish", "Koala", "Lighthouse", "Penguins", "Tulips" };
        public string CurrentPuzzleImage { get; private set; }

        static int lastUsedIndex = -1;
        private string GetRandomPuzzleImage()
        {
            if (!string.IsNullOrEmpty(CurrentPuzzleImage))
                return CurrentPuzzleImage;

            var random = new Random();
            int index = -1;
            do
            {
                index = random.Next(0, puzzles.Length - 1);
            }
            while (index == lastUsedIndex);
            lastUsedIndex = index;

            CurrentPuzzleImage = "Puzzles/" + puzzles[index];
            return CurrentPuzzleImage;
        }

        public override void UnloadContent()
        {
            //previewTexture.Dispose();
            foreach (var item in puzzlePieces)
                item.Value.Dispose();

            content.Unload();
        }

        public override void HandleInput(InputState input)
        {
            PlayerIndex player;
            if (input.IsNewButtonPress(Buttons.Back, ControllingPlayer, out player) || input.IsNewKeyPress(Keys.Escape, ControllingPlayer, out player))
            {
                switch (Mode)
                {
                    case GameplayDrawMode.Puzzle:
                        LoadingScreen.Load(ScreenManager, false, ControllingPlayer, previewScreen, gameOptionsScreen);
                        break;
                    case GameplayDrawMode.Congratulations:
                        LoadingScreen.Load(ScreenManager, true, ControllingPlayer, GameScreenFactory.Create<BackgroundScreen>(), GameScreenFactory.Create<MainMenuScreen>());
                        break;
                    default:
                        break;
                }
            }
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
#if WINDOWS_PHONE
            var mouseState = Mouse.GetState();
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                //if (animating)
                //{
                //    playingTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                //    base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
                //    return;
                //}

                var clickedRectangle = new Rectangle(mouseState.X, mouseState.Y, width, height);
                var pieceRect = new Rectangle(0, 0, width, height);

                for (int i = 0; i < scrambledPieces.Count; i++)
                {
                    pieceRect.X = (int)scrambledPieces[i].Bounds.X;
                    pieceRect.Y = (int)scrambledPieces[i].Bounds.Y;

                    if (!pieceRect.Intersects(clickedRectangle))
                        continue;

                    if (mouseState.X >= emptyPiece.X &&
                        mouseState.X <= emptyPiece.X + width &&
                        mouseState.Y >= emptyPiece.Y &&
                        mouseState.Y <= emptyPiece.Y + height)
                        continue;

                    Keys command = Keys.None;
                    if (pieceRect.X >= emptyPiece.X && pieceRect.X <= emptyPiece.X)
                    {
                        if (pieceRect.Y - height == emptyPiece.Y)
                            command = Keys.Up;
                        else if (pieceRect.Y + height == emptyPiece.Y)
                            command = Keys.Down;
                    }
                    else if (pieceRect.Y >= emptyPiece.Y && pieceRect.Y <= emptyPiece.Y)
                    {
                        if (pieceRect.X - width == emptyPiece.X)
                            command = Keys.Left;
                        else if (pieceRect.X + width == emptyPiece.X)
                            command = Keys.Right;
                    }

                    if (command != Keys.None && !pendingCommands.Contains(command))
                    {
                        pendingCommands.Enqueue(command);
                        Debug.WriteLine("Clicked: " + i);
                    }
                    break;
                }
            }
            else
                elapsedTime += gameTime.ElapsedGameTime.TotalMilliseconds;
#elif WINDOWS
            var keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Enter) || keyState.IsKeyDown(Keys.F1))
                Mode = GameplayDrawMode.Preview;
            else
            {
                //if (animating)
                //{
                //    playingTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                //    base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
                //    return;
                //}

                if (!solved)
                    Mode = GameplayDrawMode.Puzzle;

                Keys command = Keys.None;
                var pressedKeys = keyState.GetPressedKeys();
                if (pressedKeys.Length > 0)
                {
                    for (int i = 0; i < pressedKeys.Length; i++)
                    {
                        if (pressedKeys[i] != Keys.None)
                        {
                            command = pressedKeys[i];
                            Debug.WriteLine(string.Format("Key Pressed [{0}]:{1}", i, pressedKeys[i]));
                        }
                    }
                }

                switch (command)
                {
                    case Keys.Up:
                    case Keys.Down:
                    case Keys.Left:
                    case Keys.Right:
                        //if (animating)
                        //{
                        //    base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
                        //    return;
                        //}

                        if (!pendingCommands.Contains(command))
                            pendingCommands.Enqueue(command);
                        break;
                    //case Keys.Q:
                    //case Keys.Escape:
                    //    ScreenManager.Game.Exit();                       
                    //    break;
                    case Keys.R:
                    case Keys.F5:
                        Scramble();
                        solved = false;
                        playingTime = 0;
                        break;
                    default:
                        elapsedTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                        break;
                }
            }
#endif
            if (elapsedTime >= 10)
            {
                if (pendingCommands.Count > 0)
                {
                    MovePiece(pendingCommands.Dequeue());
                    CheckForCompletion();
                }
                elapsedTime = 0;
            }

            if (!solved)
                playingTime += gameTime.ElapsedGameTime.TotalMilliseconds;

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

            switch (Mode)
            {
                case GameplayDrawMode.Preview:
                    spriteBatch.Draw(previewTexture, previewVector, Color.White);
                    break;
                case GameplayDrawMode.Puzzle:
                    //DrawLines();
                    DrawPuzzle();
                    break;
                case GameplayDrawMode.Congratulations:
                    spriteBatch.DrawString(gameTimerFont, new TimeSpan(0, 0, 0, 0, (int)playingTime).ToString(), totalGameTimeVector, Color.White);
                    spriteBatch.DrawString(congratulationsFont, "Congratulations!", congratulationsVector, Color.White);
                    break;
            }

            spriteBatch.End();

            //if (TransitionPosition > 0)
            //    ScreenManager.FadeBackBufferToBlack(1f - TransitionAlpha);

            base.Draw(gameTime);
        }

        Rectangle pieceRectangle = Rectangle.Empty;
        private void DrawPuzzle()
        {
            //spriteBatch.DrawString(gameTimerFont, new TimeSpan(0, 0, 0, 0, (int)playingTime).ToString(), totalGameTimeVector, Color.White);

            for (int i = 0; i < scrambledPieces.Count; i++)
            {
                if (scrambledPieces[i].Index == -1) continue;
                var piece = puzzlePieces[scrambledPieces[i].Index];
                //spriteBatch.Draw(piece, scrambledPieces[i].Bounds, Color.White);

                pieceRectangle.X = scrambledPieces[i].Bounds.X + BORDER_THICKNESS;
                pieceRectangle.Y = scrambledPieces[i].Bounds.Y + BORDER_THICKNESS;
                pieceRectangle.Width = scrambledPieces[i].Bounds.Width - (BORDER_THICKNESS * 2);
                pieceRectangle.Height = scrambledPieces[i].Bounds.Height - (BORDER_THICKNESS * 2);
                spriteBatch.Draw(piece, pieceRectangle, Color.White);
            }
        }

        const int BORDER_THICKNESS = 3;
        //Color lineColor = Color.Black;

        //private void DrawLines()
        //{
        //    spriteBatch.Draw(emptyTexture, new Rectangle(ScreenManager.GraphicsDevice.Viewport.Width - BORDER_THICKNESS, 0, BORDER_THICKNESS, ScreenManager.GraphicsDevice.Viewport.Height), lineColor);
        //    spriteBatch.Draw(emptyTexture, new Rectangle(0, ScreenManager.GraphicsDevice.Viewport.Height - BORDER_THICKNESS, ScreenManager.GraphicsDevice.Viewport.Width, BORDER_THICKNESS), lineColor);

        //    for (int y = 0; y <= ScreenManager.GraphicsDevice.Viewport.Height; y += height)
        //        spriteBatch.Draw(emptyTexture, new Rectangle(0, y, ScreenManager.GraphicsDevice.Viewport.Width, BORDER_THICKNESS), lineColor);
        //    for (int x = 0; x <= ScreenManager.GraphicsDevice.Viewport.Width; x += width)
        //        spriteBatch.Draw(emptyTexture, new Rectangle(x, 0, BORDER_THICKNESS, ScreenManager.GraphicsDevice.Viewport.Height), lineColor);
        //}

        private void Divide()
        {
            int idx = 0;
            int cells = Convert.ToInt32(Math.Sqrt(PIECE_COUNT));
            height = ScreenManager.GraphicsDevice.Viewport.Height / cells;
            width = ScreenManager.GraphicsDevice.Viewport.Width / cells;
            puzzlePieces = new Dictionary<int, Texture2D>();

            for (int y = 0; y < ScreenManager.GraphicsDevice.Viewport.Height; y += height)
            {
                for (int x = 0; x < ScreenManager.GraphicsDevice.Viewport.Width; x += width)
                {
                    var rectangle = new Rectangle(x, y, width, height);
                    var data = new Color[width * height];
                    previewTexture.GetData<Color>(0, rectangle, data, 0, data.Length);

                    var piece = new Texture2D(ScreenManager.GraphicsDevice, width, height);
                    piece.SetData(data);

                    puzzlePieces.Add(idx++, piece);
                }
            }
        }

        private void Scramble()
        {
            int idx = 0;
            var random = new Random();
            int capacity = puzzlePieces.Count - 1;
            scrambledPieces = new Dictionary<int, PuzzlePiece>(puzzlePieces.Count);

            for (int y = 0; y < ScreenManager.GraphicsDevice.Viewport.Height; y += height)
            {
                for (int x = 0; x < ScreenManager.GraphicsDevice.Viewport.Width; x += width)
                {
                    if (idx < capacity)
                    {
                        var piece = new PuzzlePiece();
                        while (true)
                        {
                            piece.Index = random.Next(0, capacity);
                            if (!scrambledPieces.ContainsValue(piece))
                                break;
                        }

                        piece.Bounds = new Rectangle(x, y, width, height);
                        scrambledPieces.Add(idx++, piece);
                    }
                    else
                        emptyPiece = new Rectangle(x, y, width, height);
                }
            }
            scrambledPieces.Add(idx, new PuzzlePiece { Index = -1 });

            for (int i = 0; i < scrambledPieces.Count; i++)
                Debug.WriteLine(i + " : " + scrambledPieces[i].Index);
        }

        private void MovePiece(Keys command)
        {
            for (int i = 0; i < scrambledPieces.Count; i++)
            {
                switch (command)
                {
                    case Keys.Up:
                        if (scrambledPieces[i].Bounds.X == emptyPiece.X && scrambledPieces[i].Bounds.Y - height == emptyPiece.Y)
                        {
                            UpdateLocation(i);
                            UpdateScrambledIndex(command, i);
                            //Animate(command, i);
                            return;
                        }
                        break;
                    case Keys.Down:
                        if (scrambledPieces[i].Bounds.X == emptyPiece.X && scrambledPieces[i].Bounds.Y + height == emptyPiece.Y)
                        {
                            UpdateLocation(i);
                            UpdateScrambledIndex(command, i);
                            //Animate(command, i);
                            return;
                        }
                        break;
                    case Keys.Left:
                        if (scrambledPieces[i].Bounds.Y == emptyPiece.Y && scrambledPieces[i].Bounds.X - width == emptyPiece.X)
                        {
                            UpdateLocation(i);
                            UpdateScrambledIndex(command, i);
                            //Animate(command, i);
                            return;
                        }
                        break;
                    case Keys.Right:
                        if (scrambledPieces[i].Bounds.Y == emptyPiece.Y && scrambledPieces[i].Bounds.X + width == emptyPiece.X)
                        {
                            UpdateLocation(i);
                            UpdateScrambledIndex(command, i);
                            //Animate(command, i);
                            return;
                        }
                        break;
                }
            }
        }

        private void CheckForCompletion()
        {
            for (int i = 0; i < scrambledPieces.Count - 1; i++)
                if (scrambledPieces[i].Index != i)
                    return;

            solved = true;
            Mode = GameplayDrawMode.Congratulations;
        }

        private void UpdateScrambledIndex(Keys command, int index)
        {
            int newIndex = -1;
            int INCREMENT = Convert.ToInt32(Math.Sqrt(PIECE_COUNT));
            switch (command)
            {
                case Keys.Up:
                    newIndex = index - INCREMENT;
                    if (newIndex < 0) newIndex = 0;
                    break;
                case Keys.Down:
                    newIndex = index + INCREMENT;
                    if (newIndex > scrambledPieces.Count - 1) newIndex = scrambledPieces.Count - 1;
                    break;
                case Keys.Left:
                    newIndex = index - 1;
                    if (newIndex < 0) newIndex = 0;
                    break;
                case Keys.Right:
                    newIndex = index + 1;
                    if (newIndex > scrambledPieces.Count - 1) newIndex = scrambledPieces.Count - 1;
                    break;
            }

            var temp = scrambledPieces[newIndex];
            scrambledPieces[newIndex] = scrambledPieces[index];
            scrambledPieces[index] = temp;

            for (int i = 0; i < scrambledPieces.Count; i++)
                Debug.WriteLine(i + " : " + scrambledPieces[i].Index);
        }

        private void UpdateLocation(int i)
        {
            var newEmptyPiece = scrambledPieces[i].Bounds;
            scrambledPieces[i].Bounds = emptyPiece;
            emptyPiece = newEmptyPiece;
        }

        //const int INCREMENT = 2;
        //private void Animate(Keys direction, int i)
        //{
        //    animating = true;

        //    ThreadPool.QueueUserWorkItem((state) =>
        //    {
        //        Debug.WriteLine("Animating Index=" + i + " Direction=" + direction);

        //        try
        //        {
        //            Monitor.Enter(syncLock);
        //            var newEmptyPiece = scrambledPieces[i].Bounds;

        //            while (scrambledPieces[i].Bounds != emptyPiece)
        //            {
        //                switch (direction)
        //                {
        //                    case Keys.Up:
        //                        scrambledPieces[i].Bounds = new Rectangle(emptyPiece.X, scrambledPieces[i].Bounds.Y - INCREMENT, width, height);
        //                        break;
        //                    case Keys.Down:
        //                        scrambledPieces[i].Bounds = new Rectangle(emptyPiece.X, scrambledPieces[i].Bounds.Y + INCREMENT, width, height);
        //                        break;
        //                    case Keys.Left:
        //                        scrambledPieces[i].Bounds = new Rectangle(scrambledPieces[i].Bounds.X - INCREMENT, emptyPiece.Y, width, height);
        //                        break;
        //                    case Keys.Right:
        //                        scrambledPieces[i].Bounds = new Rectangle(scrambledPieces[i].Bounds.X + INCREMENT, emptyPiece.Y, width, height);
        //                        break;
        //                }

        //                if (scrambledPieces[i].Bounds.Contains(emptyPiece))
        //                    scrambledPieces[i].Bounds = emptyPiece;

        //                Thread.Sleep(1);
        //            }

        //            emptyPiece = newEmptyPiece;
        //        }
        //        finally
        //        {
        //            UpdateScrambledIndex(direction, i);
        //            CheckForCompletion();
        //            animating = false;
        //            Monitor.Exit(syncLock);
        //        }
        //    });
        //}

        public override void Deserialize(Stream stream)
        {
#if WINDOWS_PHONE
            SaveState state = null;
            if (!SaveState.Load(ref state))
                return;

            playingTime = state.PlayingTime;
            emptyPiece = state.EmptyPiece;
            scrambledPieces = new Dictionary<int, PuzzlePiece>();
            CurrentPuzzleImage = state.PuzzleImage;

            for (int i = 0; i < state.ScrambledPieces.Count; i++)
                scrambledPieces.Add(i, state.ScrambledPieces[i]);
#endif
            base.Deserialize(stream);
        }

        public override void Serialize(Stream stream)
        {
#if WINDOWS_PHONE
            if (!solved)
            {
                SaveState.Save(new SaveState
                {
                    PlayingTime = playingTime,
                    ScrambledPieces = new List<PuzzlePiece>(scrambledPieces.Values),
                    EmptyPiece = emptyPiece,
                    PuzzleImage = CurrentPuzzleImage
                });
            }
#endif
            base.Serialize(stream);
        }

        //#if WINDOWS_PHONE
        //        const string FILENAME = "SaveState.xml";

        //        private void SaveGameState()
        //        {
        //            using (var userStore = IsolatedStorageFile.GetUserStoreForApplication())
        //            {
        //                if (userStore.FileExists(FILENAME))
        //                    userStore.DeleteFile(FILENAME);

        //                if (solved)
        //                    return;

        //                using (var stream = new IsolatedStorageFileStream(FILENAME, FileMode.OpenOrCreate, userStore))
        //                {
        //                    var state = new SaveState
        //                    {
        //                        PlayingTime = playingTime,
        //                        ScrambledPieces = new List<PuzzlePiece>(scrambledPieces.Values),
        //                        EmptyPiece = emptyPiece
        //                    };

        //                    var serializer = new XmlSerializer(typeof(SaveState));
        //                    serializer.Serialize(stream, state);
        //                }
        //            }
        //        }

        //        private bool LoadGameState(ref SaveState state)
        //        {
        //            var userStore = IsolatedStorageFile.GetUserStoreForApplication();

        //            if (!userStore.FileExists(FILENAME))
        //                return false;

        //            using (var stream = new IsolatedStorageFileStream(FILENAME, FileMode.Open, userStore))
        //            {
        //                var serializer = new XmlSerializer(typeof(SaveState));
        //                state = (SaveState)serializer.Deserialize(stream);
        //            }

        //            return true;
        //        }
        //#endif
    }

    public class SaveState
    {
        public double PlayingTime { get; set; }
        public List<PuzzlePiece> ScrambledPieces { get; set; }
        public Rectangle EmptyPiece { get; set; }
        public string PuzzleImage { get; set; }
        //public Difficulty Difficulty { get; set; }

        private const string FILENAME = "SaveState.xml";

        public static bool Load(ref SaveState state)
        {
            var userStore = IsolatedStorageFile.GetUserStoreForApplication();

            if (!userStore.FileExists(FILENAME))
                return false;

            using (var stream = new IsolatedStorageFileStream(FILENAME, FileMode.Open, userStore))
            {
                var serializer = new XmlSerializer(typeof(SaveState));
                state = (SaveState)serializer.Deserialize(stream);
            }

            return true;
        }

        public static void Save(SaveState state)
        {
            using (var userStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (userStore.FileExists(FILENAME))
                    userStore.DeleteFile(FILENAME);

                using (var stream = new IsolatedStorageFileStream(FILENAME, FileMode.OpenOrCreate, userStore))
                {
                    var serializer = new XmlSerializer(typeof(SaveState));
                    serializer.Serialize(stream, state);
                }
            }
        }
    }

    public class PuzzlePiece
    {
        public int Index { get; set; }
        public Rectangle Bounds { get; set; }

        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var piece = obj as PuzzlePiece;
            if (piece != null)
                return Index.Equals(piece.Index);
            return base.Equals(obj);
        }
    }

    public enum GameplayDrawMode
    {
        Puzzle,
        Congratulations,
        Preview
    }

    //public enum Difficulty
    //{
    //    Easy = 4 * 4,
    //    Normal = 8 * 8,
    //    Expert = 16 * 16,
    //    Guru = 32 * 32
    //}
}