using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventCalendar.API.Data;

public interface IFileLock<T> where T : class
{
    bool TryAcquireLock(TimeSpan timeout);
    void ReleaseLock();
    Task<T?> Load();
    Task Save(T? data);
}

public class FileLock<T> : IFileLock<T> where T : class
{
    private readonly string _filePath;
    private readonly object _lockObject = new object();
    private FileStream? _fileStream;
    private bool _isLocked;

    public FileLock(string filePath)
    {
        _filePath = filePath;
    }

    public bool TryAcquireLock(TimeSpan timeout)
    {
        lock (_lockObject)
        {
            if (_isLocked)
            {
                return false;
            }

            try
            {
                var start = DateTime.UtcNow;
                while ((DateTime.UtcNow - start).TotalSeconds < timeout.TotalSeconds)
                {
                    try
                    {
                        _fileStream = File.Open(_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                        _isLocked = true;
                        return true;
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(100);
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to acquire lock: {ex.Message}");
                return false;
            }
        }
    }

    public void ReleaseLock()
    {
        lock (_lockObject)
        {
            if (_fileStream != null && _isLocked)
            {
                try
                {
                    _fileStream.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error releasing lock: {ex.Message}");
                }
                finally
                {
                    _fileStream = null;
                    _isLocked = false;
                }
            }
        }
    }

    public async Task<T?> Load()
    {
        if (!_isLocked) return default(T?);

        try
        {
            var content = await File.ReadAllTextAsync(_filePath);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.Deserialize<T>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading file: {ex.Message}");
            return default(T?);
        }
    }

    public async Task Save(T? data)
    {
        if (!_isLocked || data == null) return;

        try
        {
            var content = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            });
            await File.WriteAllTextAsync(_filePath, content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving file: {ex.Message}");
        }
    }
}