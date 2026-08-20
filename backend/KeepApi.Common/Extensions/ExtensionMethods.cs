namespace KeepApi.Common.Extensions
{
    public static class ExtensionMethods
    {
        public static string? Truncate(this string? value, int maxLength)
        {
            if (value is null)
            {
                return null;
            }

            return value.Length > maxLength ? value[..maxLength] : value;
        }
    }
}
