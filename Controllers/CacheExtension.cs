using Microsoft.Extensions.Caching.Memory;

namespace MyFinances.Controllers
{
    public static class CacheExtension
    {
        public static async Task<T> GetOrCreateAsync<T>(
            this IMemoryCache cache,
            string key,
            Func<Task<T>> factory,
            int minutes = 10)
        {
            if (!cache.TryGetValue(key, out T? value))
            {
                value = await factory();
                cache.Set(key, value, TimeSpan.FromMinutes(minutes));
            }
            else
            {
                if(value == null)
                {
                    value = await factory();
                    cache.Set(key, value, TimeSpan.FromMinutes(minutes));
                }
            }
            return value;
        }

        public static T GetOrCreate<T>(
        this IMemoryCache cache,
        string key,
        Func<T> factory,
        int minutes = 10)
        {
            if (!cache.TryGetValue(key, out T? value))
            {
                value = factory();
                cache.Set(key, value, TimeSpan.FromMinutes(minutes));
            }
            else
            {
                if(value == null)
                {
                    value = factory();
                    cache.Set(key, value, TimeSpan.FromMinutes(minutes));
                }
            }
            return value;
        }
    }
}
