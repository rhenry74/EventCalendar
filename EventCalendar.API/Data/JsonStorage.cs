using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace EventCalendar.API.Data;

/// <summary>
/// Generic JSON storage wrapper with file locking support.
/// Provides type-safe read/write operations for JSON files.
/// </summary>
public class JsonStorage<T> : IFileLock<List<T>> where T : class
{
    private readonly string _filePath;
    private readonly IFileLock<List<T>>? _fileLock;

    public JsonStorage(string filePath, IFileLock<List<T>>? fileLock = null)
    {
        _filePath = filePath;
        _fileLock = fileLock;
    }

    /// <summary>
    /// Load all items from the JSON file.
    /// </summary>
    public async Task<List<T>> GetAllAsync()
    {
        if (_fileLock != null)
        {
            var data = await _fileLock.Load();
            return data ?? new List<T>();
        }

        try
        {
            var content = File.ReadAllText(_filePath);
            using var doc = JsonDocument.Parse(content);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            };
            return doc.RootElement.Deserialize<List<T>>(options) ?? new List<T>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading file: {ex.Message}");
            return new List<T>();
        }
    }

    /// <summary>
    /// Load a single item by ID using reflection.
    /// </summary>
    public async Task<T?> GetByIdAsync(string id)
    {
        var items = await GetAllAsync();
        foreach (var item in items)
        {
            var itemId = GetIdProperty(item);
            if (itemId != null && itemId == id)
                return item;
        }
        return null;
    }

    /// <summary>
    /// Add a new item to the collection.
    /// </summary>
    public async Task<bool> AddAsync(T item)
    {
        if (_fileLock != null)
        {
            var items = await _fileLock.Load();
            if (items == null)
            {
                items = new List<T>();
            }

            // Check for duplicate ID using reflection
            var idProp = GetIdProperty(item);
            if (idProp != null && items.Any(x => GetIdProperty(x) == idProp))
            {
                return false;
            }

            items.Add(item);
            await _fileLock.Save(items);
            return true;
        }
        else
        {
            var items = await GetAllAsync();
            var idProp = GetIdProperty(item);
            if (idProp != null && items.Any(x => GetIdProperty(x) == idProp))
            {
                return false;
            }

            items.Add(item);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            };

            await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(items, options));
            return true;
        }
    }

    /// <summary>
    /// Update an existing item by ID using reflection.
    /// </summary>
    public async Task<bool> UpdateAsync(T item)
    {
        if (_fileLock != null)
        {
            var items = await _fileLock.Load();
            if (items == null)
            {
                return false;
            }

            // Find existing item by ID using reflection
            var idProp = GetIdProperty(item);
            if (idProp == null || !items.Any(x => GetIdProperty(x) == idProp))
            {
                return false;
            }

            var existingIndex = items.FindIndex(x => GetIdProperty(x) == idProp);
            items[existingIndex] = item;
            await _fileLock.Save(items);
            return true;
        }
        else
        {
            var items = await GetAllAsync();
            var idProp = GetIdProperty(item);
            if (idProp == null || !items.Any(x => GetIdProperty(x) == idProp))
            {
                return false;
            }

            var existingIndex = items.FindIndex(x => GetIdProperty(x) == idProp);
            items[existingIndex] = item;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            };

            await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(items, options));
            return true;
        }
    }

    /// <summary>
    /// Delete an item by ID using reflection.
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        if (_fileLock != null)
        {
            var items = await _fileLock.Load();
            if (items == null || !items.Any(x => GetIdProperty(x) == id))
            {
                return false;
            }

            items.RemoveAll(x => GetIdProperty(x) == id);
            await _fileLock.Save(items);
            return true;
        }
        else
        {
            var items = await GetAllAsync();
            if (!items.Any(x => GetIdProperty(x) == id))
            {
                return false;
            }

            items.RemoveAll(x => GetIdProperty(x) == id);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            };

            await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(items, options));
            return true;
        }
    }

    /// <summary>
    /// Filter items by a predicate.
    /// </summary>
    public async Task<List<T>> FilterAsync(Func<T, bool> predicate)
    {
        var items = await GetAllAsync();
        return items.Where(predicate).ToList();
    }

    /// <summary>
    /// Get the Id property value using reflection.
    /// </summary>
    private string? GetIdProperty(object obj)
    {
        var type = obj.GetType();
        var idProp = type.GetProperty("Id");
        if (idProp != null && idProp.CanRead)
        {
            return (string?)idProp.GetValue(obj);
        }
        return null;
    }

    /// <summary>
    /// Acquire file lock for write operations.
    /// </summary>
    public bool TryAcquireLock(TimeSpan timeout) => _fileLock?.TryAcquireLock(timeout) ?? false;

    /// <summary>
    /// Release the file lock.
    /// </summary>
    public void ReleaseLock() => _fileLock?.ReleaseLock();

    /// <summary>
    /// Load all items from the JSON file (implements IFileLock interface).
    /// </summary>
    public async Task<List<T>?> Load()
    {
        if (_fileLock != null)
        {
            var data = await _fileLock.Load();
            return data ?? new List<T>();
        }

        try
        {
            var content = File.ReadAllText(_filePath);
            using var doc = JsonDocument.Parse(content);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            };
            return doc.RootElement.Deserialize<List<T>>(options) ?? new List<T>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading file: {ex.Message}");
            return new List<T>();
        }
    }

    /// <summary>
    /// Save all items to the JSON file (implements IFileLock interface).
    /// </summary>
    public async Task Save(List<T>? data)
    {
        if (_fileLock != null && data != null)
        {
            await _fileLock.Save(data);
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        if (data != null)
        {
            await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(data, options));
        }
    }
}