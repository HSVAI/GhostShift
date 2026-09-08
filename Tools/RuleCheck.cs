using System;
using GhostShift;

// Optional standalone check: compile this together with Assets/Scripts/RunScore.cs.
public static class RuleCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Main()
    {
        var score = new RunScore();
        Check(score.Value == 0, "Initial");
        score.Advance(1); Check(score.Value == 10, "Time");
        score.CatchEcho(); Check(score.Value == 35, "Bonus");
        score.Advance(1); Check(score.Value == 45, "Persistent bonus");
        score.Advance(-1); Check(score.Value == 45, "Negative time");
        var random = new Random(7301);
        for (int i = 0; i < 10000; i++)
        {
            float previous = random.Next(-1000, 1000);
            float current = previous - random.Next(0, 1000);
            bool expected = Math.Max(current, -545) <= Math.Min(previous, -405);
            Check(RunScore.CrossesPlayer(previous, current, -475, 70) == expected, "Swept collision " + i);
        }
        Console.WriteLine("PASS: scoring regressions + 10000 swept-collision comparisons");
    }
}
