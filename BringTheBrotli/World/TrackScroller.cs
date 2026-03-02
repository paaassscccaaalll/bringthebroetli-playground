using System;
using System.Collections.Generic;

namespace BringTheBrotli.World
{
    /// <summary>
    /// Manages the scrolling background and station position triggers.
    /// Stations are at fixed distances along the track.
    /// </summary>
    public class TrackScroller
    {
        // --- Constants ---
        public static readonly List<float> StationPositions = new() { 8f, 16f, 24f };

        // --- Properties ---
        public float ScrollOffset { get; set; }
        public int NextStationIndex { get; set; }

        /// <summary>
        /// Returns the distance to the next station, or float.MaxValue if all stations passed.
        /// </summary>
        public float DistanceToNextStation(float distanceTraveled)
        {
            if (NextStationIndex >= StationPositions.Count)
                return float.MaxValue;
            return StationPositions[NextStationIndex] - distanceTraveled;
        }

        /// <summary>
        /// Update scroll offset based on train speed.
        /// Returns true if the train just arrived at a station.
        /// </summary>
        public bool Update(float speed, float distanceTraveled, float deltaTime)
        {
            // Scroll the background
            ScrollOffset += speed * deltaTime * 2f; // visual scroll speed multiplier

            // Check if we've reached the next station
            if (NextStationIndex < StationPositions.Count &&
                distanceTraveled >= StationPositions[NextStationIndex])
            {
                NextStationIndex++;
                return true; // Station reached!
            }

            return false;
        }

        public void Reset()
        {
            ScrollOffset = 0f;
            NextStationIndex = 0;
        }
    }
}
