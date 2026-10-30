using System.Text.Json.Serialization;

namespace Funkin.Modding;

/// <summary>
/// Port of the Polymod metadata file (`_polymod_meta.json`) that every mod folder ships with.
/// </summary>
public sealed class ModMetadata
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "Untitled Mod";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("homepage")]
    public string Homepage { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("contributors")]
    public List<ModContributor> Contributors { get; set; } = new();

    [JsonPropertyName("api_version")]
    public string ApiVersion { get; set; } = "1.0.0";

    [JsonPropertyName("mod_version")]
    public string ModVersion { get; set; } = "1.0.0";

    [JsonPropertyName("license")]
    public string License { get; set; } = string.Empty;

    [JsonPropertyName("license_ref")]
    public string LicenseRef { get; set; } = string.Empty;

    /// <summary>The mod's folder name. Populated when the metadata is read from disk.</summary>
    [JsonIgnore]
    public string Id { get; set; } = string.Empty;

    /// <summary>Absolute path of the mod folder.</summary>
    [JsonIgnore]
    public string FolderPath { get; set; } = string.Empty;

    [JsonIgnore]
    public string AssetsPath => Path.Combine(FolderPath, "assets");

    [JsonIgnore]
    public bool IsValid => !string.IsNullOrEmpty(FolderPath) && Directory.Exists(FolderPath);

    public override string ToString() => $"ModMetadata({Id}: {Title} v{ModVersion})";
}

public sealed class ModContributor
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
