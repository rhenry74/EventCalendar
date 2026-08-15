using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EventCalendar.API.Data
{
    /// <summary>
    /// Provides thread‑safe read/write access to a JSON file using a lock file
    /// and a retry loop to prevent corruption while allowing delayed readers.
    /// </summary>
    /// <typeparam name="T">The type of the data stored in the JSON file.</typeparam>
    public class JsonFileLockService<T>
    {
        private readonly string _filePath;
        private readonly string _lockPath;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public JsonFileLockService(string filePath)
        {
            _filePath = filePath;
            _lockPath = filePath + ".lock";
        }

        /// <summary>
        /// Reads the JSON file safely. If the file does not exist, returns the default value.
        /// </summary>
        public async Task<T?> ReadAsync(CancellationToken ct = default)
        {
            // Read can proceed concurrently; no exclusive lock needed.
            if (!File.Exists(_filePath))
                return default;

            string content;
            try
            {
                await using var reader = new StreamReader(_filePath, Encoding.UTF8);
                content = await reader.ReadToEndAsync(ct);
            }
            catch (IOException)
            {
                // File is locked for writing – wait and retry a few times.
                return await TryReadAfterLockAsync(ct);
            }

            return JsonSerializer.Deserialize<T>(content, _jsonOptions);
        }

        /// <summary>
        /// Attempts to write data to the JSON file using an exclusive lock.
        /// The method will retry with a small delay if the lock is unavailable,
        /// giving writers time to finish without failing the caller.
        /// </summary>
        public async Task<bool> WriteAsync(T data, CancellationToken ct = default)
        {
            var content = JsonSerializer.Serialize(data, _jsonOptions);
            var retries = 0;
            const int maxRetries = 10;
            const int delayMs = 100; // 100ms between attempts

            while (true)
            {
                // Try to create the lock file exclusively.
                try
                {
                    using var lockHandle = new FileStream(
                        _lockPath,
                        FileMode.OpenOrCreate,
                        FileAccess.Write,
                        FileShare.None); // exclusive access

                    // If we get here, we own the lock.
                    // Serialize to the target file.
                    await using var writer = new StreamWriter(_filePath, false, Encoding.UTF8);
                    await writer.WriteAsync(content, ct);
                    await writer.FlushAsync(ct);
                    lockHandle.Close();
                    return true;
                }
                catch (IOException)
                {
                    // Lock is held by another process – wait and retry.
                    if (retries++ >= maxRetries)
                        return false; // give up after max attempts

                    await Task.Delay(delayMs, ct);
                }
            }
        }

        private async Task<T?> TryReadAfterLockAsync(CancellationToken ct)
        {
            // Simple back‑off loop to wait for the lock to be released.
            const int maxAttempts = 10;
            const int attemptDelayMs = 100;
            var attempts = 0;

            while (attempts++ < maxAttempts)
            {
                if (File.Exists(_filePath))
                {
                    try
                    {
                        await using var reader = new StreamReader(_filePath, Encoding.UTF8);
                        var content = await reader.ReadToEndAsync(ct);
                        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
                    }
                    catch (IOException)
                    {
                        // Still locked – wait and retry.
                        await Task.Delay(attemptDelayMs, ct);
                    }
                }
                else
                {
                    // File disappeared – treat as empty.
                    return default;
                }
            }

            return default;
        }
    }
}