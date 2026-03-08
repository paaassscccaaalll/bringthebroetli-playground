using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotliDemo
{
    public interface IMinigame
    {
        void Start();
        void Update(GameTime gameTime);
        void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel);
        bool IsComplete { get; }
        MinigameResult Result { get; }
        Rectangle OverlayBounds { get; }
    }
}
