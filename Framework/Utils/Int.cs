namespace Utils
{
    public static class Int
    {
        public static int DivUp(int num, int over)
        {
            var result = 0;
            var remain = num;
            while (remain > over)
            {
                remain -= over;
                result++;
            }
            if (remain > 0) result++;
            return result;
        }
    }
}
