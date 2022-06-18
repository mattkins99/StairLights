namespace LightsOrchestrator.DAL
{
    using System.Diagnostics;
    using System.Net;
    using Microsoft.Extensions.Logging;

    public interface IDataProvider<T>
            where T : IUnique
    {
        // Create/Update
        Task UpcertAsync(T entity);
        
        // Read
        Task<T> GetDataByIdAsync(string id);

        // Delete
        Task RemoveDataAsync(T entity);

        Task<IQueryable<T>> Table { get; }
    }

    public interface IUnique
    {
        string Id { get; }

        int Partition { get; }

        int MinPartition { get; }
    }
}