namespace EvilEndUser.Core.Models;

/// <summary>
/// Describes a single MITRE ATT&amp;CK technique (or sub-technique) that a scenario emulates.
/// </summary>
/// <param name="Id">The ATT&amp;CK technique ID, e.g. <c>T1566.001</c>.</param>
/// <param name="Name">The technique name, e.g. "Spearphishing Attachment".</param>
/// <param name="Tactic">The owning tactic / kill-chain stage.</param>
/// <param name="UserActionDescription">Plain-language description of the user action being emulated.</param>
public sealed record MitreTechnique(
    string Id,
    string Name,
    MitreTactic Tactic,
    string UserActionDescription)
{
    /// <summary>The canonical attack.mitre.org URL for this technique (handles sub-techniques).</summary>
    public string Url
    {
        get
        {
            var parts = Id.Split('.');
            return parts.Length == 2
                ? $"https://attack.mitre.org/techniques/{parts[0]}/{parts[1]}/"
                : $"https://attack.mitre.org/techniques/{Id}/";
        }
    }
}
