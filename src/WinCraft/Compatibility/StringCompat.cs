namespace WinCraft.Compatibility
{
    public static class StringCompat
    {
        /// <summary>
        /// Provides a single whitespace check that works across supported framework variants.
        /// </summary>
        public static bool IsNullOrWhiteSpace(string value)
        {
#if NET45
            return string.IsNullOrWhiteSpace(value);
#else
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            for (int index = 0; index < value.Length; index++)
            {
                if (!char.IsWhiteSpace(value[index]))
                {
                    return false;
                }
            }

            return true;
#endif
        }
    }
}
