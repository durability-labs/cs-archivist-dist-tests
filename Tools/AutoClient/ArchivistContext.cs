using ArchivistClient;
using Utils;

namespace AutoClient
{
    public interface IArchivistContext
    {
        string NodeId { get; }
        App App { get; }
        IArchivistNode Archivist { get; }
        HttpClient Client { get; }
        Address Address { get; }
    }

    public class ArchivistContext : IArchivistContext
    {
        public ArchivistContext(App app, IArchivistNode archivist, HttpClient client, Address address)
        {
            App = app;
            Archivist = archivist;
            Client = client;
            Address = address;
            NodeId = Guid.NewGuid().ToString();
        }

        public string NodeId { get; }
        public App App { get; }
        public IArchivistNode Archivist { get; }
        public HttpClient Client { get; }
        public Address Address { get; }
    }
}
