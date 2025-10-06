namespace Utils
{
    public static class EnvVar
    {
        public static string GetOrDefault(string varName, string defaultValue)
        {
            var v = Environment.GetEnvironmentVariable(varName);
            if (v == null) return defaultValue;
            return v;
        }

        public static string GetOrThrow(string varName)
        {
            var v = Environment.GetEnvironmentVariable(varName);
            if (string.IsNullOrEmpty(v)) throw new Exception($"required env-var '{varName}' was null or empty.");
            return v;
        }
    }
}
