namespace LightsOrchestrator.DAL
{
    using System.Reflection;
    using System.Linq;
    using Newtonsoft.Json;
    using LightsOrchestrator;
    using Microsoft.Extensions.Logging;

    public class CacheFileDataProvider<T> : IDataProvider<T>
        where T : IUnique
    {
        static ILogger<CacheFileDataProvider<T>> logger = (ILogger<CacheFileDataProvider<T>>)Program.container.GetService(typeof(ILogger<CacheFileDataProvider<T>>));

        static JsonSerializerSettings jsonSettings = new JsonSerializerSettings()
        { 
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };

        string cacheFile;

        public Task<IQueryable<T>> Table
        {
            get 
            {
                return GetDataCacheAsync();
            }
        }

        public CacheFileDataProvider()
        {
            Assembly ass = Assembly.GetExecutingAssembly();
            var file = new FileInfo(ass.Location);
            cacheFile = $"{file.Directory.FullName}\\{typeof(T).Name}.cache";

        }

        // Create/Update
        public async Task UpcertAsync(T entity)
        {
            await this.RemoveDataAsync(entity);

            var cache = await this.Table;
            var newData = cache.ToList();
            newData.Add(entity);    
            
            await WriteAllDataAsync(newData);
        }
        
        // Read
        public async Task<T> GetDataByIdAsync(string id)
        {
            return (await this.Table).FirstOrDefault(x => x.Id == id);
        }
        
        // Delete
        public async Task RemoveDataAsync(T entity)
        {            
            var cache = await this.Table;
            var newData = cache.ToList();
            if (cache.Any(x => x.Id == entity.Id))
            {
                var item = newData.First(x => x.Id == entity.Id);
                newData.Remove(item);                
            }

            await WriteAllDataAsync(newData);
        }

        private async Task WriteAllDataAsync(IEnumerable<T> data)
        {
            await File.WriteAllTextAsync(cacheFile, JsonConvert.SerializeObject(TruncatePartitions(data), jsonSettings));
        }

        private IEnumerable<T> TruncatePartitions(IEnumerable<T> data)
        {
            var dataList = data.ToList();
            // for (int i = 0; i < dataList.Count; i++)
            // {
            //     if (dataList[i].Partition < dataList[i].MinPartition)
            //     {
            //         logger.LogTrace("Truncating: {dataList[i].Id}", dataList[i].Id);
            //         dataList.Remove(dataList[i]);
            //     }
            // }
            dataList.RemoveAll(x => x.Partition < x.MinPartition);
            return dataList;
        }

        private async Task<IQueryable<T>> GetDataCacheAsync()
        {
            IEnumerable<T> data = new List<T>();
            if (File.Exists(cacheFile))
            {
                data = JsonConvert.DeserializeObject<IEnumerable<T>>(await File.ReadAllTextAsync(cacheFile));    
            }

            return data.AsQueryable<T>();
        }
    }
}