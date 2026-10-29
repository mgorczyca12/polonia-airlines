using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using seating_service.Application.Abstractions;
using StackExchange.Redis;

namespace seating_service.Infrastructure.Persistence.Redis;

public sealed class RedisInventoryHolds(
    IConnectionMultiplexer connection,
    IOptions<RedisInventoryOptions> options) : IInventoryHolds
{
    private readonly RedisKey key = options.Value.RedisKey;
    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string Script = """
        local nowParts = redis.call('TIME')
        local now = tonumber(nowParts[1]) * 1000 + math.floor(tonumber(nowParts[2]) / 1000)
        local all = redis.call('HGETALL', KEYS[1])
        local active = {}
        for i = 1, #all, 2 do
            local hold = cjson.decode(all[i + 1])
            if hold.expires <= now then
                redis.call('HDEL', KEYS[1], all[i])
            else
                active[all[i]] = hold
            end
        end
        local owner = ARGV[2]
        if ARGV[1] == 'release' then
            redis.call('HDEL', KEYS[1], owner)
            return 1
        end
        local proposed = cjson.decode(ARGV[3])
        if proposed.expires <= now then return 0 end
        local existing = active[owner]
        if existing then
            if existing.token ~= proposed.token then return 0 end
            if ARGV[1] == 'validate' then return 1 end
        end
        if ARGV[1] == 'validate' then return 0 end
        local counts = {}
        local physical = {}
        local passengers = {}
        local seats = {}
        for heldOwner, hold in pairs(active) do
            if heldOwner ~= owner then
                for _, item in ipairs(hold.items) do
                    counts[item.bucket] = (counts[item.bucket] or 0) + 1
                    passengers[item.passenger] = true
                    if item.seat then
                        seats[item.seat] = true
                        physical[item.bucket] = (physical[item.bucket] or 0) + 1
                    end
                end
            end
        end
        local capacities = cjson.decode(ARGV[4])
        for _, passenger in ipairs(cjson.decode(ARGV[5])) do passengers[passenger] = true end
        for _, seat in ipairs(cjson.decode(ARGV[6])) do seats[seat] = true end
        for _, item in ipairs(proposed.items) do
            if passengers[item.passenger] then return 0 end
            passengers[item.passenger] = true
            counts[item.bucket] = (counts[item.bucket] or 0) + 1
            if item.seat then
                if seats[item.seat] then return 0 end
                seats[item.seat] = true
                physical[item.bucket] = (physical[item.bucket] or 0) + 1
            end
        end
        for _, capacity in ipairs(capacities) do
            if capacity.confirmed + (counts[capacity.bucket] or 0) > capacity.limit then return 0 end
            if capacity.confirmedPhysical + (physical[capacity.bucket] or 0) > capacity.physicalLimit then return 0 end
        end
        redis.call('HSET', KEYS[1], owner, ARGV[3])
        return 1
        """;

    public async Task<bool> Acquire(
        InventoryHold hold,
        HoldCapacity[] capacities,
        string[] confirmedPassengers,
        string[] confirmedSeats,
        CancellationToken cancellationToken)
    {
        var capacityJson = JsonSerializer.Serialize(capacities.Select(value => new
        {
            bucket = value.Bucket,
            limit = value.BookingLimit,
            confirmed = value.Confirmed,
            physicalLimit = value.PhysicalLimit,
            confirmedPhysical = value.ConfirmedPhysical
        }));

        return await Execute(
            "acquire",
            hold.Owner,
            Serialize(hold),
            capacityJson,
            JsonSerializer.Serialize(confirmedPassengers),
            JsonSerializer.Serialize(confirmedSeats),
            cancellationToken);
    }

    public Task<bool> IsValid(InventoryHold hold, CancellationToken cancellationToken)
    {
        return Execute("validate", hold.Owner, Serialize(hold), "[]", "[]", "[]", cancellationToken);
    }

    public async Task Release(Guid owner, CancellationToken cancellationToken)
    {
        await Execute("release", owner, "{}", "[]", "[]", "[]", cancellationToken);
    }

    private async Task<bool> Execute(
        string operation,
        Guid owner,
        string payload,
        string capacities,
        string passengers,
        string seats,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await connection.GetDatabase().ScriptEvaluateAsync(
            Script,
            [key],
            [operation, owner.ToString("D"), payload, capacities, passengers, seats])
            .WaitAsync(cancellationToken);

        return (int)result == 1;
    }

    private static string Serialize(InventoryHold hold)
    {
        var token = JsonSerializer.Serialize(hold);
        return JsonSerializer.Serialize(new
        {
            token,
            expires = hold.ExpiresAt.ToUnixTimeMilliseconds(),
            items = hold.Items.Select(item => new
            {
                bucket = item.Bucket,
                passenger = item.Passenger,
                seat = item.Seat
            })
        }, PayloadOptions);
    }
}
