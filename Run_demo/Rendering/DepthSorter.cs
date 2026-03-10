using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotliDemo
{
    public static class DepthSorter
    {
        private static readonly List<IDepthSortable> _sortBuffer = new();

        public static void DrawSorted(SpriteBatch spriteBatch, IEnumerable<IDepthSortable> renderables)
        {
            _sortBuffer.Clear();
            _sortBuffer.AddRange(renderables);
            _sortBuffer.Sort((a, b) => a.DepthY.CompareTo(b.DepthY));

            foreach (var item in _sortBuffer)
                item.Draw(spriteBatch);
        }
    }
}
