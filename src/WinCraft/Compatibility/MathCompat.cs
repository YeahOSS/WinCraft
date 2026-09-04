using System;

namespace WinCraft.Compatibility
{
    /// <summary>
    /// Math helpers that bridge framework gaps.
    /// <c>Math.Clamp</c> was introduced in .NET Core 2.0 and is not available
    /// on the project's net45 / net30 targets.
    /// </summary>
    internal static class MathCompat
    {
        internal static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
