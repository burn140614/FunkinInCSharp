using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Funkin.Paths;
using Funkin.Util;

namespace Funkin.Data.Dialogue;

/// <summary>A single line of dialogue, either a plain string or an object with extra fields.</summary>
public sealed class DialogueLine
{
    /// <summary>Character id of the speaker, e.g. 'bf', 'dad', 'gf'.</summary>
    public string Speaker { get; set; } = string.Empty;

    /// <summary>The text to display.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Optional portrait / emotion name.</summary>
    public string? Portrait { get; set; }

    /// <summary>Optional animation to play on the speaker.</summary>
    public string? Animation { get; set; }

    /// <summary>Optional sound effect to play with the line.</summary>
    public string? Sound { get; set; }

    public bool IsPlayer { get; set; }
}

/// <summary>
/// Port of funkin.play.cutscene.dialogue.Conversation: a cutscene dialogue file
/// (`data/songs/&lt;id&gt;/dialogue.json`), or the older `data/&lt;song&gt;/dialogue.txt` format.
/// </summary>
public sealed class ConversationData
{
    public List<DialogueLine> Lines { get; } = new();

    public string SongId { get; set; } = string.Empty;

    public static ConversationData Parse(string json)
    {
        var conversation = new ConversationData();
        var node = Json.ParseNode(json);

        JsonArray? dialogueArray = null;
        if (node is JsonArray directArray)
        {
            dialogueArray = directArray;
        }
        else if (node is JsonObject obj)
        {
            conversation.SongId = Json.Get<string>(obj, "song") ?? string.Empty;
            dialogueArray = obj["dialogue"] as JsonArray;
        }

        if (dialogueArray is null) return conversation;

        foreach (var entry in dialogueArray)
        {
            if (entry is null) continue;

            if (entry is JsonValue value && value.TryGetValue<string>(out var raw))
            {
                conversation.Lines.Add(ParseTextLine(raw));
                continue;
            }

            if (entry is not JsonObject lineObject) continue;

            conversation.Lines.Add(new DialogueLine
            {
                Speaker = Json.Get<string>(lineObject, "speaker") ?? Json.Get<string>(lineObject, "portrait") ?? "dad",
                Text = Json.Get<string>(lineObject, "text") ?? string.Empty,
                Portrait = Json.Get<string>(lineObject, "portrait"),
                Animation = Json.Get<string>(lineObject, "animation"),
                Sound = Json.Get<string>(lineObject, "sound"),
                IsPlayer = string.Equals(Json.Get<string>(lineObject, "side"), "bf", StringComparison.OrdinalIgnoreCase),
            });
        }

        return conversation;
    }

    /// <summary>Parses the classic `speaker: text` format.</summary>
    public static DialogueLine ParseTextLine(string raw)
    {
        int colon = raw.IndexOf(':');
        if (colon <= 0) return new DialogueLine { Speaker = "dad", Text = raw.Trim() };

        return new DialogueLine
        {
            Speaker = raw[..colon].Trim().ToLowerInvariant(),
            Text = raw[(colon + 1)..].Trim(),
        };
    }

    public override string ToString() => $"ConversationData({Lines.Count} lines)";
}
