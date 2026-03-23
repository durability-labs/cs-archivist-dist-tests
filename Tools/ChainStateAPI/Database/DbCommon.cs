using Logging;
using Microsoft.EntityFrameworkCore;

namespace ChainStateAPI.Database
{
    public interface IDbCommon
    {
    }

    public class DbCommon<TContext> : IDbCommon where TContext : BaseContext, new()
    {
        private readonly ILog logger;

        internal DbCommon(ILog logger)
        {
            this.logger = logger;
        }

        internal TContext Context { get; private set; } = null!;

        public async Task Initialize()
        {
            try
            {
                var host = GetEnv("DBHOST");
                Context = new TContext();
                Context.Decorate(
                    host: host,
                    login: GetEnv("DBLOGIN"),
                    password: GetEnv("DBPASSWORD")
                );
                await Context.Database.EnsureCreatedAsync();
                logger.Log($"Initialized database at host {host}");
            }
            catch (Exception ex)
            {
                logger.Error("Failed to initialize db: " + ex);
                throw;
            }
        }

        private string GetEnv(string v)
        {
            var value = Environment.GetEnvironmentVariable(v);
            if (string.IsNullOrEmpty(value)) throw new Exception("Missing envvar: " + v);
            return value;
        }
    }

    public abstract class BaseContext : DbContext
    {
        private string host = string.Empty;
        private string login = string.Empty;
        private string password = string.Empty;

        public void Decorate(string host, string login, string password)
        {
            this.host = host;
            this.login = login;
            this.password = password;
        }

        protected abstract string DatabaseName { get; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql($"Host={host};Username={login};Password={password};Database={DatabaseName}");
    }
}
