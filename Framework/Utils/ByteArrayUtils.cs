namespace Utils
{
    public static class ByteArrayUtils
    {
        public static bool Equal(byte[] a, byte[] b)
        {
            return a.SequenceEqual(b);
        }
    }
}
