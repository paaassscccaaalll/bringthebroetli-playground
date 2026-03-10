using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotliDemo
{
    // Interface chosen over a base class because world objects (players, items,
    // NPCs) have unrelated class hierarchies. Any class can opt in by exposing
    // its foot-anchor Y as the depth value and a Draw method.
    public interface IDepthSortable
    {
        float DepthY { get; }
        void Draw(SpriteBatch spriteBatch);
    }
}
