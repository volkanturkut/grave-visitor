using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generic object pooling system for performance optimization
/// Reduces GC pressure by reusing GameObjects instead of instantiating/destroying
/// </summary>
/// <typeparam name="T">Component type to pool (must be a Component)</typeparam>
public class ObjectPool<T> where T : Component
{
    private readonly GameObject _prefab;
    private readonly Transform _parent;
    private readonly Queue<T> _availableObjects;
    private readonly List<T> _allObjects;
    private readonly int _initialSize;
    private readonly int _maxSize;
    private readonly bool _allowGrowth;

    // Constants
    private const int DEFAULT_INITIAL_SIZE = 10;
    private const int DEFAULT_MAX_SIZE = 50;
    private const bool DEFAULT_ALLOW_GROWTH = true;

    /// <summary>
    /// Creates a new object pool
    /// </summary>
    /// <param name="prefab">Prefab to instantiate</param>
    /// <param name="initialSize">Number of objects to pre-create</param>
    /// <param name="maxSize">Maximum pool size (if growth allowed)</param>
    /// <param name="parent">Parent transform for pooled objects</param>
    /// <param name="allowGrowth">Whether pool can grow beyond initial size</param>
    public ObjectPool(
        GameObject prefab, 
        int initialSize = DEFAULT_INITIAL_SIZE, 
        int maxSize = DEFAULT_MAX_SIZE, 
        Transform parent = null,
        bool allowGrowth = DEFAULT_ALLOW_GROWTH)
    {
        _prefab = prefab;
        _parent = parent;
        _initialSize = initialSize;
        _maxSize = maxSize;
        _allowGrowth = allowGrowth;
        _availableObjects = new Queue<T>(initialSize);
        _allObjects = new List<T>(maxSize);

        PreWarm();
    }

    /// <summary>
    /// Pre-instantiates objects to avoid runtime allocation spikes
    /// </summary>
    public void PreWarm()
    {
        for (int i = 0; i < _initialSize; i++)
        {
            CreateNewObject();
        }
    }

    /// <summary>
    /// Gets an object from the pool, creating a new one if necessary and allowed
    /// </summary>
    /// <returns>Pooled object component, or null if pool is at capacity</returns>
    public T Get()
    {
        T obj;

        // Try to get from available queue
        if (_availableObjects.Count > 0)
        {
            obj = _availableObjects.Dequeue();
        }
        else if (_allowGrowth && _allObjects.Count < _maxSize)
        {
            // Pool can grow - create new object
            obj = CreateNewObject();
        }
        else
        {
            // Pool is at capacity and growth not allowed
            DebugLogger.LogWarning($"[ObjectPool] Pool for {typeof(T).Name} is at max capacity ({_maxSize}). Consider increasing pool size.");
            return null;
        }

        // Activate and initialize
        obj.gameObject.SetActive(true);
        
        // Call lifecycle method if object implements IPoolable
        if (obj is IPoolable poolable)
        {
            poolable.OnSpawnFromPool();
        }

        return obj;
    }

    /// <summary>
    /// Returns an object to the pool for reuse
    /// </summary>
    /// <param name="obj">Object to return</param>
    public void Return(T obj)
    {
        if (obj == null)
        {
            DebugLogger.LogWarning("[ObjectPool] Attempted to return null object to pool");
            return;
        }

        // Check if object belongs to this pool
        if (!_allObjects.Contains(obj))
        {
            DebugLogger.LogWarning($"[ObjectPool] Attempted to return object that doesn't belong to this pool: {obj.name}");
            return;
        }

        // Check if object is already in the available queue
        if (_availableObjects.Contains(obj))
        {
            DebugLogger.LogWarning($"[ObjectPool] Attempted to return object that is already in the pool: {obj.name}");
            return;
        }

        // Call lifecycle method if object implements IPoolable
        if (obj is IPoolable poolable)
        {
            poolable.OnReturnToPool();
        }

        // Deactivate and return to queue
        obj.gameObject.SetActive(false);
        _availableObjects.Enqueue(obj);
    }

    /// <summary>
    /// Clears the pool and destroys all objects
    /// </summary>
    public void Clear()
    {
        foreach (T obj in _allObjects)
        {
            if (obj != null)
            {
                Object.Destroy(obj.gameObject);
            }
        }

        _availableObjects.Clear();
        _allObjects.Clear();
    }

    /// <summary>
    /// Gets the total number of objects in the pool (available + in use)
    /// </summary>
    public int TotalCount => _allObjects.Count;

    /// <summary>
    /// Gets the number of available objects in the pool
    /// </summary>
    public int AvailableCount => _availableObjects.Count;

    /// <summary>
    /// Gets the number of objects currently in use
    /// </summary>
    public int ActiveCount => _allObjects.Count - _availableObjects.Count;

    private T CreateNewObject()
    {
        GameObject instance = Object.Instantiate(_prefab, _parent);
        instance.SetActive(false);
        
        T component = instance.GetComponent<T>();
        if (component == null)
        {
            DebugLogger.LogError($"[ObjectPool] Prefab {_prefab.name} does not have component {typeof(T).Name}!");
            Object.Destroy(instance);
            return null;
        }

        _allObjects.Add(component);
        _availableObjects.Enqueue(component);

        return component;
    }
}
