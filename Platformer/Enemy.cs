using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Platformer
{
    class Enemy
    {
        Sprite enemySprite = new Sprite();
        Game1 game = null;
        Vector2 velocity = Vector2.Zero;
        public Rectangle Bounds
        {
            get { return enemySprite.Bounds; }
        }
        public Vector2 Position
        {
            get { return enemySprite.position; }
            set { enemySprite.position = value; }
        }
        private bool moveRight = true;

        static float acceleration = Game1.acceleration / 5.0f;
        static Vector2 maxVelocity = Game1.maxVelocity / 5.0f;

        public Enemy(Game1 game)
        {
            this.game = game;
        }
        public void Load(ContentManager content)
        {
            AnimatedTexture animation = new AnimatedTexture(Vector2.Zero, 0, 1, 1);
            animation.Load(content, "enemy", 1, 1);
            enemySprite.Add(animation, 0, -32);
        }
        public void Update(float deltaTime)
        {
            enemySprite.Update(deltaTime);

            float ddx = 0; // acceleration
            int tx = game.PixelToTile(Position.X);
            int ty = game.PixelToTile(Position.Y);
            bool nx = (Position.X) % Game1.tile != 0; // zombie overlaps right?
            bool ny = (Position.Y) % Game1.tile != 0; // zombie overlaps below?
            bool cell = game.CellAtTileCoord(tx, ty) != 0;
            bool cellright = game.CellAtTileCoord(tx + 1, ty) != 0;
            bool celldown = game.CellAtTileCoord(tx, ty + 1) != 0;
            bool celldiag = game.CellAtTileCoord(tx + 1, ty + 1) != 0;

            if (moveRight)
            {
                if (celldiag && !cellright)
                {
                    ddx = ddx + acceleration; // zombie wants to go right
                }
                else
                {
                    velocity.X = 0;
                    moveRight = false;
                }
            }
            if (!moveRight)
            {
                if (celldown && !cell)
                {
                    ddx = ddx - acceleration; // zombie wants to go left
                }
                else
                {
                    velocity.X = 0;
                    moveRight = true;
                }
            }

            Position = new Vector2((float)Math.Floor(
           Position.X + (deltaTime * velocity.X)), Position.Y);
            velocity.X = MathHelper.Clamp(velocity.X + (deltaTime * ddx),
           -maxVelocity.X, maxVelocity.X);
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            enemySprite.Draw(spriteBatch);
        }
    }
}
