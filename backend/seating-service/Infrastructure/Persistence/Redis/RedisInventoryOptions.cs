namespace seating_service.Infrastructure.Persistence.Redis;

public sealed class RedisInventoryOptions
{
    public const string DefaultRedisKey = "{reservation-inventory}:holds";

    public string ConnectionString { get; set; } = string.Empty;
    public string RedisKey { get; set; } = DefaultRedisKey;
}
