namespace Core
{
    public static class EnvironmentVariables
    {
        public static string GetStringOrDefault(string name, string defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return value;
        }

        public static string? GetNullableStringOrDefault(string name, string? defaultValue = null)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return value;
        }
    }
}
