namespace BringTheBrotli
{
    /// <summary>
    /// Global constants for screen layout, voxel train geometry, and player settings.
    /// Layout is Crossy-Road-style 2.5D: each train car is a voxel block with
    /// a visible top face and south (front) face, sitting on a continuous ground plane.
    /// </summary>
    public static class Constants
    {
        // Screen
        public const int ScreenWidth = 1280;
        public const int ScreenHeight = 720;

        // ── Sky & Horizon ──
        public const float SkyTopY = 0f;
        public const float SkyBottomY = 150f;
        public const float HillsY = 150f;

        // ── Ground plane (continuous, fills behind and in front of train) ──
        public const float GroundStartY = 220f;
        public const float MidTerrainY = 220f;    // parallax alias

        // ── Voxel train block ──
        public const float VoxelTopY = 268f;       // top edge of the top face
        public const float VoxelTopH = 90f;        // top face height
        public const float VoxelFrontY = 358f;     // top of the front face (= VoxelTopY + VoxelTopH)
        public const float VoxelFrontH = 112f;     // front face height
        public const float VoxelBottomY = 470f;    // bottom of the front face
        public const float VoxelSideW = 14f;       // east-face (right side) width
        public const float VoxelOutline = 3f;      // dark outline width for voxel edges

        // ── Legacy aliases (keep PlayerCharacter, TaskStationManager, etc. working) ──
        public const float RoofTopY = VoxelTopY;
        public const float RoofHeight = VoxelTopH;
        public const float RooflineY = VoxelFrontY;   // player feet Y
        public const float WallTopY = VoxelFrontY;
        public const float WallHeight = VoxelFrontH;
        public const float WallBottomY = VoxelBottomY;

        // ── Ground / Track ──
        public const float TrackY = 472f;
        public const float GroundY = 490f;
        public const float HudTopY = 520f;

        // ── Player ──
        public const float PlayerWalkSpeed = 150f;
        public const float PlayerWidth = 14f;
        public const float PlayerTotalHeight = 52f;
        public const float TaskInteractRadius = 40f;

        // ── Camera ──
        public const float CameraLeadOffset = 300f;
    }
}
