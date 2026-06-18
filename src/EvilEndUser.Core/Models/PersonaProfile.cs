namespace EvilEndUser.Core.Models;

/// <summary>
/// Which browser the simulator should drive.
/// </summary>
public enum BrowserKind
{
    /// <summary>Microsoft Edge (drives SmartScreen / Network Protection).</summary>
    Edge,

    /// <summary>Google Chrome.</summary>
    Chrome,
}

/// <summary>
/// Defines the persona of the simulated user — how naive and risk-taking they are. The LLM brain
/// uses this to flavour its decision making so runs are non-deterministic but bounded.
/// </summary>
public sealed class PersonaProfile
{
    /// <summary>Display name of the persona, e.g. "Naive New Hire".</summary>
    public string Name { get; set; } = "Naive End User";

    /// <summary>The fictional job role, used in LLM prompts to shape behaviour.</summary>
    public string Role { get; set; } = "Office worker in the finance department";

    /// <summary>
    /// 0.0 = extremely cautious, 1.0 = clicks absolutely everything. Influences how often the
    /// brain chooses risky actions.
    /// </summary>
    public double Gullibility { get; set; } = 0.8;

    /// <summary>
    /// 0.0 = very predictable, 1.0 = highly erratic. Maps onto the LLM sampling temperature.
    /// </summary>
    public double Erraticness { get; set; } = 0.7;

    /// <summary>A short backstory injected into the system prompt for colour.</summary>
    public string Backstory { get; set; } =
        "You are not very tech-savvy. You trust emails that look official, you love free stuff, " +
        "and you are always in a hurry. You rarely read security warnings before clicking.";

    /// <summary>Built-in persona presets the operator can pick from.</summary>
    public static IReadOnlyList<PersonaProfile> Presets { get; } = new[]
    {
        new PersonaProfile
        {
            Name = "Naive New Hire",
            Role = "Brand-new employee eager to please",
            Gullibility = 0.9,
            Erraticness = 0.6,
            Backstory = "It is your first week. You want to be helpful and responsive, so you open " +
                        "every email and click every link your 'manager' or 'IT' sends you.",
        },
        new PersonaProfile
        {
            Name = "Busy Executive",
            Role = "Overworked finance executive",
            Gullibility = 0.75,
            Erraticness = 0.5,
            Backstory = "You are always rushing between meetings on your phone and laptop. You skim " +
                        "emails, approve things quickly, and tap 'Approve' on MFA prompts to make them stop.",
        },
        new PersonaProfile
        {
            Name = "Curious Tinkerer",
            Role = "Power user who downloads free tools",
            Gullibility = 0.6,
            Erraticness = 0.9,
            Backstory = "You love installing free software, browser extensions, and 'productivity hacks'. " +
                        "You will happily paste a command someone gave you to 'fix' an error.",
        },
        new PersonaProfile
        {
            Name = "Cautious Veteran",
            Role = "Experienced but occasionally fallible employee",
            Gullibility = 0.35,
            Erraticness = 0.4,
            Backstory = "You usually spot phishing, but under time pressure you still slip up now and then.",
        },
    };
}
