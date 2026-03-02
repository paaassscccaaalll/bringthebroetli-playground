using System;

namespace BringTheBrotli.Players
{
    /// <summary>
    /// Tracks a player's physical position on the train, movement state, and task interaction.
    /// The player walks left/right along the train roof and interacts with TaskStations.
    /// </summary>
    public class PlayerCharacter
    {
        public int PlayerIndex { get; }             // 0 = Player 1, 1 = Player 2
        public float WorldX { get; set; }           // horizontal position in world space
        public float VelocityX { get; set; }
        public bool IsPerformingTask { get; set; }  // true while holding to complete a station task
        public int FacingDirection { get; set; } = 1; // 1 = right, -1 = left

        // The Y position is always fixed at the roofline:
        public float FeetY => Constants.RooflineY;
        public float TopY => Constants.RooflineY - Constants.PlayerTotalHeight;

        public PlayerCharacter(int playerIndex, float startWorldX)
        {
            PlayerIndex = playerIndex;
            WorldX = startWorldX;
        }

        /// <summary>
        /// Update player horizontal movement. Clamp to train bounds.
        /// </summary>
        public void Update(float dt, float moveInput, float trainLeftEdge, float trainRightEdge)
        {
            // Lock movement while performing a task
            if (IsPerformingTask)
            {
                VelocityX = 0f;
                return;
            }

            VelocityX = moveInput * Constants.PlayerWalkSpeed;
            WorldX += VelocityX * dt;

            // Track facing direction
            if (moveInput > 0.01f) FacingDirection = 1;
            else if (moveInput < -0.01f) FacingDirection = -1;

            // Clamp to train bounds (with a small margin so player stays on the roof)
            WorldX = Math.Clamp(WorldX, trainLeftEdge + 10f, trainRightEdge - 10f);
        }

        /// <summary>Reset character to a starting position.</summary>
        public void Reset(float startWorldX)
        {
            WorldX = startWorldX;
            VelocityX = 0f;
            IsPerformingTask = false;
            FacingDirection = 1;
        }
    }
}
