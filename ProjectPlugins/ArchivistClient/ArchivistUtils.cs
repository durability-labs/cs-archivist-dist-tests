namespace ArchivistClient
{
    public static class ArchivistUtils
    {
        public static string ToShortId(string id)
        {
            if (id.Length > 10)
            {
                return $"{id[..3]}*{id[^6..]}";
            }
            return id;
        }

        // after update of archivist-dht, shortID should be consistent!
        public static string ToNodeIdShortId(string id)
        {
            if (id.Length > 10)
            {
                return $"{id[..2]}*{id[^6..]}";
            }
            return id;
        }
    }
}
