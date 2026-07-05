namespace WinCraft.Compatibility
{
    internal static class FrameworkCompat
    {
#if NET30
        public const bool IsNet30 = true;
#else
        public const bool IsNet30 = false;
#endif
    }
}
