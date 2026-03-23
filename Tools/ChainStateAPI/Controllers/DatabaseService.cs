using ChainStateAPI.Database;
using Logging;

namespace ChainStateAPI.Controllers
{
    public interface IDatabaseService
    {
        TResult Query<TContext, TResult>(Func<TContext, TResult> query) where TContext : BaseContext, new();
        void Mutate<TContext>(Action<TContext> mutation) where TContext : BaseContext, new();
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly ILog log;
        private readonly Dictionary<Type, IDbCommon> contexts = new();

        public DatabaseService(ILog log)
        {
            this.log = log;
        }

        public TResult Query<TContext, TResult>(Func<TContext, TResult> query) where TContext : BaseContext, new()
        {
            var context = GetOrCreate<TContext>();
            try
            {
                return query(context);
            }
            catch (Exception ex)
            {
                log.Error($"Exception in '{nameof(Query)}'<{typeof(TContext).Name}, {typeof(TResult).Name}> = {ex}");
                throw;
            }
        }

        public void Mutate<TContext>(Action<TContext> mutation) where TContext : BaseContext, new()
        {
            var context = GetOrCreate<TContext>();
            lock (context)
            {
                try
                {
                    mutation(context);
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    log.Error($"Exception in '{nameof(Mutate)}'<{typeof(TContext).Name}> = {ex}");
                    throw;
                }
            }
        }

        private TContext GetOrCreate<TContext>() where TContext : BaseContext, new()
        {
            if (contexts.ContainsKey(typeof(TContext)))
            {
                if (contexts[typeof(TContext)] is DbCommon<TContext> handle)
                {
                    return handle.Context;
                }
                throw new InvalidOperationException($"DBContext for type {typeof(TContext).Name} is not of correct type.");
            }

            var newDbHandle = new DbCommon<TContext>(log);
            newDbHandle.Initialize().Wait();
            contexts.Add(typeof(TContext), newDbHandle);
            return newDbHandle.Context;
        }
    }
}
