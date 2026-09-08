using System;

namespace GhostShift
{
    public sealed class RunScore
    {
        private float seconds;
        private int bonus;
        public int Value { get { return (int)Math.Floor(seconds * 10f) + bonus; } }
        public void Advance(float dt) { seconds += Math.Max(0f, dt); }
        public void CatchEcho() { bonus += 25; }
        public static bool CrossesPlayer(float previousY, float currentY, float playerY, float radius)
        {
            return previousY >= playerY - radius && currentY <= playerY + radius;
        }
    }
}
