// file:	Squidex.Extensions.AzureServiceBus\ServiceBusClientpool.cs
//
// summary:	Implements the service bus clientpool class
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Squidex.Extensions.AzureServiceBus;

/// <summary>
/// Service bus client pool evicted.
/// </summary>
/// <typeparam name="TKey"> Type of the key.</typeparam>
/// <typeparam name="TClient"> Type of the client.</typeparam>
/// <param name="pool"> The pool.</param>
/// <param name="key"> The key.</param>
/// <param name="client"> The client.</param>
internal delegate Task ServiceBusClientPoolEvicted<TKey, TClient>(ServiceBusClientPool<TKey, TClient> pool, TKey key, TClient client)
     where TKey : notnull;

/// <summary>
/// A service bus clientpool.
/// </summary>
/// <typeparam name="TKey"> Type of the key.</typeparam>
/// <typeparam name="TClient"> Type of the client.</typeparam>
internal class ServiceBusClientPool<TKey, TClient> where TKey : notnull
{
    /// <summary>
    /// (Immutable) the time to live.
    /// </summary>
    private static readonly TimeSpan _timeToLive = TimeSpan.FromMinutes(30);

    /// <summary>
    /// (Immutable) the factory.
    /// </summary>
    private readonly Func<TKey, Task<TClient>> _factory;

    /// <summary>
    /// (Immutable) the memory cache.
    /// </summary>
    private readonly MemoryCache _memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));

    /// <summary>
    /// (Immutable) the evicted callback.
    /// </summary>
    private readonly ServiceBusClientPoolEvicted<TKey, TClient>? _evictedCallback;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="factory"> The factory.</param>
    /// <param name="callback"> (Optional) The callback.</param>
    public ServiceBusClientPool(Func<TKey, TClient> factory, ServiceBusClientPoolEvicted<TKey, TClient>? callback = null)
    {
        _factory = x => Task.FromResult(factory(x));
        _evictedCallback = callback;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="factory"> The factory.</param>
    /// <param name="callback"> (Optional) The callback.</param>
    public ServiceBusClientPool(Func<TKey, Task<TClient>> factory, ServiceBusClientPoolEvicted<TKey, TClient>? callback = null)
    {
        _factory = factory;
        _evictedCallback = callback;
    }

    /// <summary>
    /// Gets client asynchronous.
    /// </summary>
    /// <param name="key"> The key.</param>
    /// <returns>
    /// The client.
    /// </returns>
    public async Task<TClient> GetClientAsync(TKey key)
    {
        if (!_memoryCache.TryGetValue<TClient>(key, out var client))
        {
            client = await _factory(key);

            var item = _memoryCache.CreateEntry(key);

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _timeToLive,
                Size = 1,
            };

            options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = EvictionCallback
            });


            _memoryCache.Set(key, client, options);
        }

        return client!;
    }

    /// <summary>
    /// Callback, called when the eviction.
    /// </summary>
    /// <param name="key"> The key.</param>
    /// <param name="value"> The value.</param>
    /// <param name="evictionReason"> The eviction reason.</param>
    /// <param name="state"> The state.</param>
    private void EvictionCallback(object key, object? value, EvictionReason evictionReason, object? state)
    {
        if(_evictedCallback == null)
        {
            return;
        }

        // Run when value is removed from a cache
        if (evictionReason == EvictionReason.Replaced)
            return;

        if(key is not TKey typedKey || value is not TClient typedClient)
        {
            return;
        }

        _ = _evictedCallback.Invoke(this, typedKey, typedClient);
    }
}
