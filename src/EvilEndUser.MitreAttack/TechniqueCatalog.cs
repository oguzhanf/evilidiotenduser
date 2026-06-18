using EvilEndUser.Core.Models;

namespace EvilEndUser.MitreAttack;

/// <summary>
/// Static catalogue of the MITRE ATT&amp;CK techniques (Enterprise v19.1) emulated by this simulator.
/// Each entry focuses on the <i>user action</i> that an unsuspecting end user performs. IDs and names
/// are verified against attack.mitre.org.
/// </summary>
public static class TechniqueCatalog
{
    // ── TA0001 Initial Access ───────────────────────────────────────────────────────────────
    public static readonly MitreTechnique SpearphishingAttachment = new(
        "T1566.001", "Spearphishing Attachment", MitreTactic.InitialAccess,
        "User opens a malicious attachment from a lure email (e.g. 'Your Q4 Pay Slip').");

    public static readonly MitreTechnique SpearphishingLink = new(
        "T1566.002", "Spearphishing Link", MitreTactic.InitialAccess,
        "User clicks a link in an email leading to a credential-harvesting or drive-by page.");

    public static readonly MitreTechnique SpearphishingViaService = new(
        "T1566.003", "Spearphishing via Service", MitreTactic.InitialAccess,
        "User clicks a malicious link delivered through Teams/LinkedIn/social rather than email.");

    public static readonly MitreTechnique DriveByCompromise = new(
        "T1189", "Drive-by Compromise", MitreTactic.InitialAccess,
        "User browses to a compromised/watering-hole site or clicks a malvertisement.");

    // ── TA0002 Execution ────────────────────────────────────────────────────────────────────
    public static readonly MitreTechnique UserExecutionLink = new(
        "T1204.001", "User Execution: Malicious Link", MitreTactic.Execution,
        "User clicks a link that triggers a download/execution.");

    public static readonly MitreTechnique UserExecutionFile = new(
        "T1204.002", "User Execution: Malicious File", MitreTactic.Execution,
        "User double-clicks a masqueraded file such as Invoice_2026.pdf.exe.");

    public static readonly MitreTechnique PowerShell = new(
        "T1059.001", "Command and Scripting Interpreter: PowerShell", MitreTactic.Execution,
        "User is socially engineered into pasting/running a PowerShell command ('ClickFix').");

    public static readonly MitreTechnique RemoteAccessTools = new(
        "T1219", "Remote Access Tools", MitreTactic.Execution,
        "User installs a legitimate RAT after a vishing call, granting interactive control.");

    // ── TA0003 Persistence ──────────────────────────────────────────────────────────────────
    public static readonly MitreTechnique RegistryRunKeys = new(
        "T1547.001", "Boot/Logon Autostart: Registry Run Keys / Startup Folder", MitreTactic.Persistence,
        "Payload writes an HKCU Run key so it executes on every logon.");

    public static readonly MitreTechnique ScheduledTask = new(
        "T1053.005", "Scheduled Task", MitreTactic.Persistence,
        "Payload registers a user-context scheduled task for periodic re-execution.");

    public static readonly MitreTechnique BrowserExtensions = new(
        "T1176", "Browser Extensions", MitreTactic.Persistence,
        "User installs a malicious browser extension that exfiltrates cookies.");

    // ── TA0005 Stealth (formerly Defense Evasion) ───────────────────────────────────────────
    public static readonly MitreTechnique Masquerading = new(
        "T1036.005", "Masquerading: Match Legitimate Name or Location", MitreTactic.Stealth,
        "Malicious file is named/located to look legitimate (e.g. AdobeUpdate.exe in %TEMP%).");

    public static readonly MitreTechnique HtmlSmuggling = new(
        "T1027.006", "Obfuscated Files: HTML Smuggling", MitreTactic.Stealth,
        "Browser reassembles a payload from JavaScript blobs, bypassing gateway scanning.");

    public static readonly MitreTechnique HiddenFiles = new(
        "T1564.001", "Hide Artifacts: Hidden Files and Directories", MitreTactic.Stealth,
        "Dropped payload sets the +H attribute to hide inside user directories.");

    // ── TA0112 Defense Impairment ───────────────────────────────────────────────────────────
    public static readonly MitreTechnique DisableTools = new(
        "T1562.001", "Impair Defenses: Disable or Modify Tools", MitreTactic.DefenseImpairment,
        "User is told by 'IT' to turn off real-time protection or add an AV exclusion.");

    public static readonly MitreTechnique DisableFirewall = new(
        "T1562.004", "Impair Defenses: Disable or Modify System Firewall", MitreTactic.DefenseImpairment,
        "User or script disables the Windows Firewall.");

    public static readonly MitreTechnique ModifyRegistryDefense = new(
        "T1112", "Modify Registry", MitreTactic.DefenseImpairment,
        "Payload edits security-tool configuration registry keys.");

    // ── TA0006 Credential Access ────────────────────────────────────────────────────────────
    public static readonly MitreTechnique PhishingForInfoLink = new(
        "T1598.003", "Phishing for Information: Spearphishing Link", MitreTactic.CredentialAccess,
        "User types corporate credentials into a fake login page.");

    public static readonly MitreTechnique MfaFatigue = new(
        "T1621", "Multi-Factor Authentication Request Generation", MitreTactic.CredentialAccess,
        "User taps 'Approve' on a flood of MFA push prompts.");

    public static readonly MitreTechnique WebPortalCapture = new(
        "T1056.003", "Input Capture: Web Portal Capture", MitreTactic.CredentialAccess,
        "Cloned login page silently logs credentials as the user types them.");

    public static readonly MitreTechnique OAuthConsent = new(
        "T1528", "Steal Application Access Token", MitreTactic.CredentialAccess,
        "User clicks 'Accept' on a malicious OAuth consent screen.");

    // ── TA0007 Discovery ────────────────────────────────────────────────────────────────────
    public static readonly MitreTechnique FileDiscovery = new(
        "T1083", "File and Directory Discovery", MitreTactic.Discovery,
        "Payload enumerates local/mapped drives looking for sensitive documents.");

    public static readonly MitreTechnique SystemInfoDiscovery = new(
        "T1082", "System Information Discovery", MitreTactic.Discovery,
        "Payload runs systeminfo/whoami/ipconfig to fingerprint the host.");

    public static readonly MitreTechnique BrowserInfoDiscovery = new(
        "T1217", "Browser Information Discovery", MitreTactic.Discovery,
        "Payload reads browser history/saved-login databases.");

    // ── TA0008 Lateral Movement ─────────────────────────────────────────────────────────────
    public static readonly MitreTechnique InternalSpearphishing = new(
        "T1534", "Internal Spearphishing", MitreTactic.LateralMovement,
        "Compromised account sends phishing to colleagues over email/Teams.");

    public static readonly MitreTechnique RemovableMedia = new(
        "T1091", "Replication Through Removable Media", MitreTactic.LateralMovement,
        "User plugs in a found USB drive that launches a payload.");

    // ── TA0009 Collection ───────────────────────────────────────────────────────────────────
    public static readonly MitreTechnique DataFromCloud = new(
        "T1530", "Data from Cloud Storage", MitreTactic.Collection,
        "Sensitive files are pulled from OneDrive/SharePoint.");

    public static readonly MitreTechnique ArchiveData = new(
        "T1560.001", "Archive Collected Data: Archive via Utility", MitreTactic.Collection,
        "Collected files are zipped (optionally password-protected) before exfiltration.");

    public static readonly MitreTechnique ScreenCapture = new(
        "T1113", "Screen Capture", MitreTactic.Collection,
        "Payload periodically screenshots the desktop.");

    // ── TA0011 Command and Control ──────────────────────────────────────────────────────────
    public static readonly MitreTechnique WebProtocolsC2 = new(
        "T1071.001", "Application Layer Protocol: Web Protocols", MitreTactic.CommandAndControl,
        "Beacon check-ins over HTTPS blend with normal browsing.");

    public static readonly MitreTechnique WebServiceC2 = new(
        "T1102", "Web Service", MitreTactic.CommandAndControl,
        "C2 routed through legitimate services (Discord/Telegram/GitHub).");

    public static readonly MitreTechnique IngressToolTransfer = new(
        "T1105", "Ingress Tool Transfer", MitreTactic.CommandAndControl,
        "First-stage dropper fetches additional tooling (e.g. via certutil).");

    // ── TA0010 Exfiltration ─────────────────────────────────────────────────────────────────
    public static readonly MitreTechnique ExfilToCloud = new(
        "T1567.002", "Exfiltration to Cloud Storage", MitreTactic.Exfiltration,
        "Data uploaded to Dropbox/Drive/OneDrive using their APIs/sync clients.");

    public static readonly MitreTechnique ExfilToCodeRepo = new(
        "T1567.001", "Exfiltration to Code Repository", MitreTactic.Exfiltration,
        "Sensitive files pushed to a private GitHub/GitLab repo.");

    public static readonly MitreTechnique SelfExfiltration = new(
        "T1213", "Data from Information Repositories", MitreTactic.Exfiltration,
        "User emails/share-links sensitive data to a personal external account (insider).");

    // ── TA0040 Impact ───────────────────────────────────────────────────────────────────────
    public static readonly MitreTechnique RansomwareImpact = new(
        "T1486", "Data Encrypted for Impact", MitreTactic.Impact,
        "Payload encrypts local/mapped files and drops a ransom note (emulated harmlessly).");

    public static readonly MitreTechnique FinancialTheft = new(
        "T1657", "Financial Theft", MitreTactic.Impact,
        "Compromised mailbox sends fraudulent wire-transfer (BEC) instructions.");

    /// <summary>Every technique in the catalogue.</summary>
    public static readonly IReadOnlyList<MitreTechnique> All = new[]
    {
        SpearphishingAttachment, SpearphishingLink, SpearphishingViaService, DriveByCompromise,
        UserExecutionLink, UserExecutionFile, PowerShell, RemoteAccessTools,
        RegistryRunKeys, ScheduledTask, BrowserExtensions,
        Masquerading, HtmlSmuggling, HiddenFiles,
        DisableTools, DisableFirewall, ModifyRegistryDefense,
        PhishingForInfoLink, MfaFatigue, WebPortalCapture, OAuthConsent,
        FileDiscovery, SystemInfoDiscovery, BrowserInfoDiscovery,
        InternalSpearphishing, RemovableMedia,
        DataFromCloud, ArchiveData, ScreenCapture,
        WebProtocolsC2, WebServiceC2, IngressToolTransfer,
        ExfilToCloud, ExfilToCodeRepo, SelfExfiltration,
        RansomwareImpact, FinancialTheft,
    };

    /// <summary>Finds a technique by its ATT&amp;CK ID, or null.</summary>
    public static MitreTechnique? FindById(string id)
        => All.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
}
