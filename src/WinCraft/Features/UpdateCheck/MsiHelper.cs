using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.ApplicationInstallationAndServicing;

namespace WinCraft.Features.UpdateCheck
{
    internal static class MsiHelper
    {
        private const int ProductCodeBufferLength = 39;

        public static unsafe bool IsProductInstalled(string productName, string publisher)
        {
            for (uint index = 0; index < uint.MaxValue; index++)
            {
                var productCodeBuffer = new char[ProductCodeBufferLength];
                var context = MSIINSTALLCONTEXT.MSIINSTALLCONTEXT_NONE;

                fixed (char* p = productCodeBuffer)
                {
                    uint result = PInvoke.MsiEnumProductsEx(
                        (string)null, (string)null,
                        (uint)MSIINSTALLCONTEXT.MSIINSTALLCONTEXT_ALL,
                        index, new PWSTR(p), &context, default, null);

                    if (result == (uint)WIN32_ERROR.ERROR_NO_MORE_ITEMS)
                        return false;
                    if (result != (uint)WIN32_ERROR.ERROR_SUCCESS)
                        continue;
                }

                string productCode = NullTerminated(productCodeBuffer);
                if (string.IsNullOrEmpty(productCode))
                    continue;

                if (Matches(productCode, context, productName, publisher))
                    return true;
            }

            return false;
        }

        private static unsafe bool Matches(
            string productCode, MSIINSTALLCONTEXT context, string productName, string publisher)
        {
            return StringEquals(GetProperty(productCode, context, "ProductName"), productName)
                && StringEquals(GetProperty(productCode, context, "Publisher"), publisher);
        }

        private static unsafe string GetProperty(
            string productCode, MSIINSTALLCONTEXT context, string propertyName)
        {
            uint charCount = 0;
            uint result = PInvoke.MsiGetProductInfoEx(
                productCode, null, context, propertyName, default, &charCount);

            if (result != (uint)WIN32_ERROR.ERROR_SUCCESS
                && result != (uint)WIN32_ERROR.ERROR_MORE_DATA)
                return null;

            var buffer = new char[checked((int)charCount + 1)];
            fixed (char* p = buffer)
            {
                uint len = (uint)buffer.Length;
                if (PInvoke.MsiGetProductInfoEx(
                        productCode, null, context, propertyName, new PWSTR(p), &len)
                    != (uint)WIN32_ERROR.ERROR_SUCCESS)
                    return null;
            }

            return NullTerminated(buffer);
        }

        private static string NullTerminated(char[] buffer)
        {
            int length = System.Array.IndexOf(buffer, '\0');
            return new string(buffer, 0, length < 0 ? buffer.Length : length);
        }

        private static bool StringEquals(string a, string b)
        {
            return string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
