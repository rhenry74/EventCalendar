using System.Text.Json;

public sealed record EventCategory(
    string Name,
    string Icon,
    string Type,
    string PrimaryColor,
    string SecondaryColor);

public sealed class CategoryStore(string runtimePath, string developmentPath)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<EventCategory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var path = File.Exists(runtimePath) ? runtimePath : developmentPath;
        if (!File.Exists(path))
            throw new FileNotFoundException("The runtime categories.json file was not found.", path);

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<EventCategory>>(stream, JsonOptions, cancellationToken) ?? [];
    }
}
