using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Graphics;
using MonoGame.Extended.ViewportAdapters;


namespace Platformer
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        public const int GAME_STATE_MENU = 0;
        public const int GAME_STATE_GAME = 1;
        public const int GAME_STATE_GAMEOVER = 2;
        public const int GAME_STATE_WIN = 3;
        public static int tile = 64;
        // abitrary choice for 1m (1 tile = 1 meter)
        public static float meter = tile;
        // very exaggerated gravity (6x)
        public static float gravity = meter * 9.8f * 5.0f;
        // max vertical speed (10 tiles/sec horizontal, 15 tiles/sec vertical)
        public static Vector2 maxVelocity = new Vector2(meter * 10, meter * 15);
        // horizontal acceleration - take 1/2 second to reach max velocity
        public static float acceleration = maxVelocity.X * 2;
        // horizontal friction - take 1/6 second to stop from max velocity
        public static float friction = maxVelocity.X * 6;
        // (a large) instantaneous jump impulse
        public static float jumpImpulse = meter * 1500;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont arialFont;
        int score = 0;
        int lives = 3;
        int gameState = GAME_STATE_MENU;

        List<Enemy> enemies = new List<Enemy>();
        Sprite goal = null;

        Song gameMusic;

        Song gameoverSound;
        Song winSound;

        static public int viewWidth;
        static public int viewHeight;
        static public float pi;

        Player player = null;
        Texture2D heart = null;

        public Camera2D camera = null;
        TiledMap map = null;
        TiledMapRenderer mapRenderer = null;
        TiledMapTileLayer collisionLayer;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            viewWidth = graphics.GraphicsDevice.Viewport.Width;
            viewHeight = graphics.GraphicsDevice.Viewport.Height;
            pi = 3.141592f;
            player = new Player(this);
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            player.Load(Content);
            arialFont = Content.Load<SpriteFont>("Arial");
            heart = Content.Load<Texture2D>("heart");

            var viewportAdapter = new BoxingViewportAdapter(Window, GraphicsDevice, viewWidth, viewHeight);

            camera = new Camera2D(viewportAdapter);
            camera.Position = new Vector2(0, viewHeight);

            map = Content.Load<TiledMap>("Level");
            mapRenderer = new TiledMapRenderer(GraphicsDevice);

            gameoverSound = Content.Load<Song>("GameOver");
            winSound = Content.Load<Song>("Win");

            foreach (TiledMapTileLayer layer in map.TileLayers)
            {
                if (layer.Name == "Solid")
                {
                    collisionLayer = layer;
                }
            }

                gameMusic = Content.Load<Song>("SuperHero_original_no_Intro");

            // TODO: use this.Content to load your game content here
        }
        private void Start()
        {
            player.SetPosition(new Vector2(64, 448));
            score = 0;
            enemies.Clear();
            foreach (TiledMapObjectLayer layer in map.ObjectLayers)
            {
                if (layer.Name == "Enemies")
                {
                    foreach (TiledMapObject obj in layer.Objects)
                    {
                        Enemy enemy = new Enemy(this);
                        enemy.Load(Content);
                        enemy.Position = new Vector2(
                        obj.Position.X, obj.Position.Y);
                        enemies.Add(enemy);
                    }
                }
                if (layer.Name == "Goal")
                {
                    TiledMapObject obj = layer.Objects[0];
                    if (obj != null)
                    {
                        AnimatedTexture anim = new AnimatedTexture(
                       Vector2.Zero, 0, 1, 1);
                        anim.Load(Content, "goal", 1, 1);
                        goal = new Sprite();
                        goal.Add(anim, 0, -32);
                        goal.position = new Vector2(
                       obj.Position.X, obj.Position.Y);
                    }
                }
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        /// 
        private void CheckCollisions()
        {
            foreach (Enemy e in enemies)
            {
                if (IsColliding(player.Bounds, e.Bounds) == true)
                {
                    if (player.Velocity.Y > 0)
                    {
                        player.JumpOnCollision();
                        enemies.Remove(e);
                        score++;
                        break;
                    }
                    else
                    {
                        if (lives > 1)
                        {
                            player.Death();
                            Start();
                            lives--;
                        }
                        else
                        {
                            MediaPlayer.Stop();
                            MediaPlayer.IsRepeating = false;
                            MediaPlayer.Play(gameoverSound);
                            gameState = GAME_STATE_GAMEOVER;
                        }
                        break;
                        // player just died
                    }
                }
            }
            if (IsColliding(player.Bounds, goal.Bounds) == true)
            {
                MediaPlayer.Stop();
                MediaPlayer.IsRepeating = false;
                MediaPlayer.Play(winSound);
                gameState = GAME_STATE_WIN;
            }
        }
        private bool IsColliding(Rectangle rect1, Rectangle rect2)
        {
            if (rect1.X + rect1.Width < rect2.X ||
            rect1.X > rect2.X + rect2.Width ||
            rect1.Y + rect1.Height < rect2.Y ||
            rect1.Y > rect2.Y + rect2.Height)
            {
                // these two rectangles are not colliding
                return false;
            }
            // else, the two AABB rectangles overlap, therefore collision
            return true;
        }
        private void UpdateCamera(float deltaTime)
        {
            camera.Position = player.GetPosition - new Vector2(viewWidth / 2, viewHeight / 2);
        }
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            camera.Position = Vector2.Zero;
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            switch (gameState)
            {
                case GAME_STATE_MENU:
                    if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                    {
                        MediaPlayer.IsRepeating = true;
                        MediaPlayer.Play(gameMusic);
                        Start();
                        gameState = GAME_STATE_GAME;
                    }
                    break;
                case GAME_STATE_GAME:
                    player.Update(deltaTime);
                    foreach (Enemy e in enemies)
                    {
                        e.Update(deltaTime);
                    }
                    UpdateCamera(deltaTime);
                    CheckCollisions();
                    break;
                case GAME_STATE_GAMEOVER:
                    if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                    {
                        MediaPlayer.Stop();
                        MediaPlayer.IsRepeating = true;
                        MediaPlayer.Play(gameMusic);
                        lives = 3;
                        Start();
                        gameState = GAME_STATE_GAME;
                    }
                    break;
                case GAME_STATE_WIN:
                    if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                    {
                        MediaPlayer.Stop();
                        MediaPlayer.IsRepeating = true;
                        MediaPlayer.Play(gameMusic);
                        lives = 3;
                        Start();
                        gameState = GAME_STATE_GAME;
                    }
                    break;
            }

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            float offset;
            GraphicsDevice.Clear(Color.Black);


            var viewMatrix = camera.GetViewMatrix();
            var projectionMatrix = Matrix.CreateOrthographicOffCenter(0, viewWidth, viewHeight, 0, 0f, -1f);

            // TODO: Add your drawing code here
            spriteBatch.Begin(transformMatrix: viewMatrix);
            switch (gameState)
            {
                case GAME_STATE_MENU:
                    offset = arialFont.MeasureString("Zander's Platformer").X / 2;
                    spriteBatch.DrawString(arialFont, "Zander's Platformer", new Vector2(viewWidth / 2 - offset, 80), Color.Red);
                    offset = arialFont.MeasureString("Press Enter").X / 2;
                    spriteBatch.DrawString(arialFont, "Press Enter", new Vector2(viewWidth / 2 - offset, 120), Color.Red);
                    break;
                case GAME_STATE_GAME:
                    GraphicsDevice.Clear(Color.CornflowerBlue);
                    mapRenderer.Draw(map, ref viewMatrix, ref projectionMatrix);
                    player.Draw(spriteBatch);
                    foreach (Enemy e in enemies)
                    {
                        e.Draw(spriteBatch);
                    }
                    goal.Draw(spriteBatch);

                    //HUD
                    for (int i = 0; i < lives; i++)
                    {
                        spriteBatch.Draw(heart, new Vector2(viewWidth - 80 - i * 20, 20) + camera.Position, Color.White);
                    }
                    spriteBatch.DrawString(arialFont, "Score: " + score.ToString(), new Vector2(20, 20)+camera.Position, Color.Black);
                    break;
                case GAME_STATE_GAMEOVER:
                    offset = arialFont.MeasureString("Game Over").X / 2;
                    spriteBatch.DrawString(arialFont, "Game Over", new Vector2(viewWidth / 2 - offset, 80), Color.Red);
                    offset = arialFont.MeasureString("Press Enter to retry").X / 2;
                    spriteBatch.DrawString(arialFont, "Press Enter to retry", new Vector2(viewWidth / 2 - offset, 120), Color.Red);
                    break;
                case GAME_STATE_WIN:
                    offset = arialFont.MeasureString("You win!").X / 2;
                    spriteBatch.DrawString(arialFont, "You win!", new Vector2(viewWidth / 2 - offset, 80), Color.Red);
                    offset = arialFont.MeasureString("Score: " + score.ToString()).X / 2;
                    spriteBatch.DrawString(arialFont, "Score: " + score.ToString(), new Vector2(viewWidth / 2 - offset, 100), Color.Red);
                    offset = arialFont.MeasureString("Life Multiplier: " + lives.ToString()).X / 2;
                    spriteBatch.DrawString(arialFont, "Life Multiplier: " + lives.ToString(), new Vector2(viewWidth / 2 - offset, 120), Color.Red);
                    int finalScore = score * lives;
                    offset = arialFont.MeasureString("Final Score: " + finalScore.ToString()).X / 2;
                    spriteBatch.DrawString(arialFont, "Final Score: " + finalScore.ToString(), new Vector2(viewWidth / 2 - offset, 140), Color.Red);
                    offset = arialFont.MeasureString("Press Enter to play again").X / 2;
                    spriteBatch.DrawString(arialFont, "Press Enter to play again", new Vector2(viewWidth / 2 - offset, 180), Color.Red);
                    break;
            }
        spriteBatch.End();

            base.Draw(gameTime);
        }

        public int PixelToTile(float pixelCoord)
        {
            return (int)Math.Floor(pixelCoord / tile);
        }
        public int TileToPixel(int tileCoord)
        {
            return tile * tileCoord;
        }
        public int CellAtPixelCoord(Vector2 pixelCoords)
        {
            if (pixelCoords.X < 0 ||
           pixelCoords.X > map.WidthInPixels || pixelCoords.Y < 0)
                return 1;
            // let the player drop of the bottom of the screen (this means death)
            if (pixelCoords.Y > map.HeightInPixels)
                return 0;
            return CellAtTileCoord(
           PixelToTile(pixelCoords.X), PixelToTile(pixelCoords.Y));
        }
        public int CellAtTileCoord(int tx, int ty)
        {
            if (tx < 0 || tx >= map.Width || ty < 0)
                return 1;
            // let the player drop of the bottom of the screen (this means death)
            if (ty >= map.Height)
                return 0;
            TiledMapTile? tile;
            collisionLayer.TryGetTile(tx, ty, out tile);
            return tile.Value.GlobalIdentifier;
        }
    }
}
