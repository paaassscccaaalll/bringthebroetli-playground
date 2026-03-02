using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotli.World
{
    /// <summary>
    /// Represents one car of the train, with a world-space position, dimensions, colors,
    /// sprite texture, and the task stations located on its roof.
    /// </summary>
    public class TrainCar
    {
        public string Name { get; }
        public float WorldX { get; set; }           // left edge X in world space
        public float Width { get; }                 // width of this car in world-space pixels
        public Color RoofColor { get; }
        public Color WallColor { get; }
        public Texture2D? Texture { get; set; }     // sprite texture for this car
        public List<TaskStation> Stations { get; }

        public float RightEdge => WorldX + Width;

        public TrainCar(string name, float width, Color wallColor, Color? roofColor = null, Texture2D? texture = null)
        {
            Name = name;
            Width = width;
            WallColor = wallColor;
            Texture = texture;
            // Voxel style: top face is LIGHTER than front face (Crossy Road convention)
            RoofColor = roofColor ?? new Color(
                System.Math.Min((int)(wallColor.R * 1.3f), 255),
                System.Math.Min((int)(wallColor.G * 1.3f), 255),
                System.Math.Min((int)(wallColor.B * 1.3f), 255));
            Stations = new List<TaskStation>();
        }
    }
}
