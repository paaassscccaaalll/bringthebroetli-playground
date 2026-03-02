namespace BringTheBrotli.World
{
    /// <summary>
    /// Tracks the camera's world-space X position for converting world coordinates to screen space.
    /// Y is unchanged (no vertical scrolling).
    /// </summary>
    public class Camera
    {
        public float WorldX { get; set; }

        /// <summary>Convert a world-space X to screen-space X.</summary>
        public float WorldToScreenX(float worldX) => worldX - WorldX;
    }
}
