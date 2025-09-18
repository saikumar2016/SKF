using StackExchange.Redis;


namespace SKF.Services
{
    
    public class CacheService
    {
        private readonly IDatabase _db;

        public CacheService(string redisConnectionString)
        {
            var redis = ConnectionMultiplexer.Connect(redisConnectionString);
            _db = redis.GetDatabase();
        }

        public async Task<string> GetCachedAnswerAsync(string key)
        {
            return await _db.StringGetAsync(key);
        }

        public async Task SetCachedAnswerAsync(string key, string value)
        {
            await _db.StringSetAsync(key, value, TimeSpan.FromHours(1));
        }
    }

}
