namespace EvilEndUser.Core.Safety;

/// <summary>
/// A curated catalogue of <b>officially sanctioned, harmless</b> security test payloads and URLs.
/// <para>
/// Every value here is published by Microsoft, EICAR, or AMTSO specifically for safely validating
/// security controls. None of them is real malware — they are designed to generate genuine
/// Defender / SmartScreen / EOP detections <i>without</i> compromising the endpoint.
/// </para>
/// <para>
/// When <see cref="Models.SimulationConfig.SafeMode"/> is enabled (the default), the simulator is
/// only allowed to use targets drawn from this catalogue.
/// </para>
/// </summary>
public static class TestPayloads
{
    /// <summary>
    /// The canonical 68-byte EICAR anti-malware test string. Writing this to a file triggers
    /// Microsoft Defender Antivirus exactly like a real signature hit, but it is completely inert.
    /// Reference: https://www.eicar.org/download-anti-malware-testfile/
    /// </summary>
    public const string EicarString =
        @"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";

    /// <summary>
    /// The GTUBE (Generic Test for Unsolicited Bulk Email) string. Including this in an email body
    /// makes anti-spam engines (including Exchange Online Protection) classify the message as spam.
    /// Reference: https://spamassassin.apache.org/gtube/
    /// </summary>
    public const string GtubeString =
        "XJS*C4JDBQADN1.NSBN3*2IDNEN*GTUBE-STANDARD-ANTI-UBE-TEST-EMAIL*C.34X";

    /// <summary>
    /// EICAR download URLs hosted by eicar.org. Fetching one of these via the browser exercises
    /// Defender for Endpoint network protection and SmartScreen download inspection.
    /// </summary>
    public static readonly IReadOnlyList<string> EicarDownloadUrls = new[]
    {
        "https://secure.eicar.org/eicar.com.txt",
        "https://secure.eicar.org/eicar_com.zip",
        "https://secure.eicar.org/eicarcom2.zip",
    };

    /// <summary>
    /// Microsoft's official SmartScreen demonstration URLs (hosted on Microsoft test infrastructure).
    /// These render a benign page but cause Edge SmartScreen to display its block/warning experiences.
    /// Reference: https://learn.microsoft.com/defender-endpoint/defender-endpoint-demonstration-smartscreen-url-reputation
    /// </summary>
    public static readonly IReadOnlyList<SmartScreenDemo> SmartScreenDemos = new[]
    {
        new SmartScreenDemo("Is This Phishing prompt", "https://demo.smartscreen.msft.net/other/areyousure.html"),
        new SmartScreenDemo("Known phishing page (blocked)", "https://demo.smartscreen.msft.net/phishingdemo.html"),
        new SmartScreenDemo("Known malware page (blocked)", "https://demo.smartscreen.msft.net/other/malware.html"),
        new SmartScreenDemo("Blocked download (URL reputation)", "https://demo.smartscreen.msft.net/download/malwaredemo/freevideo.exe"),
        new SmartScreenDemo("Browser exploit page", "https://demo.smartscreen.msft.net/other/exploit.html"),
        new SmartScreenDemo("App reputation (unknown app)", "https://demo.smartscreen.msft.net/appreputation/"),
    };

    /// <summary>
    /// Microsoft Defender for Endpoint demonstration scenarios hub and network-protection test URL.
    /// Reference: https://learn.microsoft.com/defender-endpoint/defender-endpoint-demonstrations
    /// </summary>
    public static readonly IReadOnlyList<string> NetworkProtectionTestUrls = new[]
    {
        "https://smartscreentestratings2.net/",
    };

    /// <summary>
    /// AMTSO (Anti-Malware Testing Standards Organisation) feature-validation URLs. AMTSO is an
    /// industry body and these pages safely confirm individual protection features are active.
    /// Reference: https://www.amtso.org/security-features-check/
    /// </summary>
    public static readonly IReadOnlyList<AmtsoCheck> AmtsoChecks = new[]
    {
        new AmtsoCheck("Potentially Unwanted Application (PUA) check",
            "https://www.amtso.org/feature-settings-check-potentially-unwanted-applications/"),
        new AmtsoCheck("Cloud-delivered protection check",
            "https://www.amtso.org/feature-settings-check-cloud-lookup/"),
        new AmtsoCheck("Phishing protection check",
            "https://www.amtso.org/feature-settings-check-phishing-page/"),
    };

    /// <summary>
    /// A harmless, well-known living-off-the-land command line that Defender for Endpoint flags as
    /// suspicious behaviour (it merely prints a string — it downloads nothing real).
    /// </summary>
    public static readonly IReadOnlyList<string> BenignSuspiciousCommands = new[]
    {
        // Encoded "Write-Host 'EvilEndUser simulation test'" — exercises encoded-command detection.
        "powershell.exe -NoProfile -EncodedCommand VwByAGkAdABlAC0ASABvAHMAdAAgACcARQB2AGkAbABFAG4AZABVAHMAZQByACAAcwBpAG0AdQBsAGEAdABpAG8AbgAgAHQAZQBzAHQAJwA=",
        // certutil URL-cache pattern against an inert localhost target — exercises LOLBin telemetry.
        "certutil.exe -urlcache -split -f http://127.0.0.1/eviltest.txt %TEMP%\\eviltest.txt",
    };

    /// <summary>
    /// Built-in Windows reconnaissance commands. Individually harmless, but a rapid burst of them is
    /// classic discovery behaviour that Defender for Endpoint correlates. None modify the system.
    /// </summary>
    public static readonly IReadOnlyList<string> SafeReconCommands = new[]
    {
        "cmd.exe /c whoami /all",
        "cmd.exe /c systeminfo",
        "cmd.exe /c ipconfig /all",
        "cmd.exe /c net user",
        "cmd.exe /c net localgroup administrators",
        "cmd.exe /c nltest /dclist:",
        "cmd.exe /c tasklist /v",
        "cmd.exe /c net view",
    };

    /// <summary>
    /// Read-only Microsoft Defender status queries. Safe to run in SafeMode and used by the
    /// Defense-Impairment scenario to emulate a user "checking" their AV before being told to disable it.
    /// </summary>
    public static readonly IReadOnlyList<string> SafeDefenderProbeCommands = new[]
    {
        "powershell.exe -NoProfile -Command Get-MpComputerStatus",
        "powershell.exe -NoProfile -Command Get-MpPreference",
    };

    /// <summary>Every command the simulator is permitted to run in SafeMode.</summary>
    public static IReadOnlyList<string> AllSafeCommands { get; } =
        BenignSuspiciousCommands.Concat(SafeReconCommands).Concat(SafeDefenderProbeCommands).ToArray();

    /// <summary>
    /// The real "disable Defender real-time protection" command. Only used when SafeMode is OFF and
    /// the risk ceiling allows <see cref="Models.RiskLevel.Critical"/>. On an E5 endpoint with Tamper
    /// Protection enabled this is blocked and generates a high-value alert rather than truly disabling AV.
    /// </summary>
    public const string DisableDefenderRealtimeCommand =
        "powershell.exe -NoProfile -Command Set-MpPreference -DisableRealtimeMonitoring $true";

    /// <summary>Returns every safe URL in the catalogue (for allow-list validation).</summary>
    public static IEnumerable<string> AllSafeUrls()
    {
        foreach (var u in EicarDownloadUrls) yield return u;
        foreach (var d in SmartScreenDemos) yield return d.Url;
        foreach (var u in NetworkProtectionTestUrls) yield return u;
        foreach (var a in AmtsoChecks) yield return a.Url;
    }
}

/// <summary>A named Microsoft SmartScreen demonstration page.</summary>
public sealed record SmartScreenDemo(string Name, string Url);

/// <summary>A named AMTSO security-feature validation page.</summary>
public sealed record AmtsoCheck(string Name, string Url);
