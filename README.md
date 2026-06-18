# 😈 Evil End User — Defender / MITRE ATT&CK Endpoint Simulator

A **WinUI 3** desktop application that role-plays a careless, click-happy end user on a Windows
endpoint to **validate Microsoft Defender for Endpoint (MDE) and Defender for Office 365 (MDO)
alerting** across the MITRE ATT&CK kill chain.

It drives the *real* desktop apps a "stupid end user" would touch — **Edge/Chrome, Outlook/Exchange,
Microsoft Teams, PowerShell, the filesystem and registry** — performing risky-but-**safe** actions
(opening phishing links, downloading the EICAR test file, pasting "ClickFix" commands, forwarding
data to personal accounts, …) so blue teams can confirm their detections, alerts, and automated
responses actually fire.

An optional **LLM brain** (Azure OpenAI / OpenAI / Ollama via Semantic Kernel) makes the simulated
user behave **non-deterministically**, choosing varied actions run-to-run like a real human.

> ⚠️ **Authorised testing only.** Run this exclusively on endpoints and tenants you are explicitly
> permitted to test. See [Safety model](#-safety-model).

---

## ✨ What it does

- **Drives real desktop apps**, not synthetic API calls:
  - **Browser** (Edge/Chrome) via [Playwright](https://playwright.dev/dotnet/) — navigates to
    SmartScreen demo URLs, triggers downloads, fills phishing login forms.
  - **Outlook / Exchange** via **Microsoft Graph** — sends mail (incl. external recipients), reads
    the inbox for "phishing" links, exercises Safe Links / Safe Attachments / EOP.
  - **Microsoft Teams** via **Microsoft Graph** — sends chat messages with risky links (internal
    spearphishing / Safe Links for Teams).
  - **PowerShell / cmd** — runs benign-but-suspicious commands (encoded PowerShell, LOLBins,
    discovery bursts).
  - **Filesystem & registry** — drops the EICAR test file, writes a reversible Run-key for
    persistence telemetry.
- **MITRE ATT&CK v19.1 scenario harnesses** spanning 12 tactics (see [coverage](#-mitre-attck-coverage)).
- **LLM-driven free-roam** mode for emergent, non-scripted behaviour.
- **Live activity log** with severity colouring and **JSON/CSV export** for correlating your
  simulator timeline against the alerts that show up in the Defender portal.
- A hard **safety model**: a global risk ceiling plus a Safe-Mode allow-list of vetted, harmless
  test payloads.

---

## 🧱 Architecture

A clean, layered .NET 9 solution:

```
EvilEndUser.sln
├── src/EvilEndUser.Core         # Models, abstractions, engine, safety guard, event bus (net9.0)
├── src/EvilEndUser.MitreAttack  # Technique catalogue + scenario harnesses        (net9.0)
├── src/EvilEndUser.Automation   # Playwright / Graph / process / filesystem drivers (net9.0-windows)
├── src/EvilEndUser.LLM          # Semantic Kernel agent brain + plugins + prompts  (net9.0)
└── src/EvilEndUser.App          # WinUI 3 control-panel UI                          (net9.0-windows)
```

**Execution flow.** Every action — whether scripted by a scenario or improvised by the LLM — is a
`SimAction` that flows through a single funnel, `IScenarioContext.ExecuteAsync`, which:

1. validates it against the **`SafetyGuard`** (risk ceiling + Safe-Mode allow-list),
2. dispatches it to the correct **driver**,
3. records an **`ActionResult`** and publishes a **`SimulationEvent`** to the UI/log.

This guarantees safety and telemetry are *always* applied — drivers are never called directly.

```
Scenario ─┐
          ├─▶ IScenarioContext.ExecuteAsync ─▶ SafetyGuard ─▶ Driver ─▶ ActionResult ─▶ Event Bus ─▶ UI/Log
LLM brain ┘
```

---

## 🗺️ MITRE ATT&CK coverage

Scenarios focus on **user actions** (no kernel exploits). Mapped to Enterprise ATT&CK **v19.1**.

| Tactic | ID | Scenario | Key techniques |
|---|---|---|---|
| Initial Access | TA0001 | Spearphishing Link → Credential Entry | T1566.002, T1598.003 |
| Initial Access | TA0001 | Drive-by Compromise | T1189 |
| Execution | TA0002 | Malicious File Execution (EICAR) | T1204.002, T1036.005 |
| Execution | TA0002 | ClickFix PowerShell | T1059.001 |
| Persistence | TA0003 | Registry Run-Key Persistence | T1547.001 |
| Defense Impairment | TA0112 | Disable Antivirus (social engineering) | T1562.001 |
| Credential Access | TA0006 | MFA Fatigue | T1621 |
| Discovery | TA0007 | System & Account Discovery | T1082, T1083 |
| Lateral Movement | TA0008 | Internal Spearphishing via Teams | T1534, T1566.003 |
| Collection | TA0009 | Local Data Collection & Staging | T1083, T1560.001 |
| Exfiltration | TA0010 | Self-Exfiltration to External Account | T1567.002, T1213 |
| Impact | TA0040 | Business Email Compromise (wire fraud) | T1657 |

Run any single scenario, every scenario for a tactic, or the curated **full kill chain** (phishing →
execution → persistence → defense impairment → discovery → collection → exfiltration → impact).

---

## 🛡️ Safety model

This tool is designed to be **safe to run inside a production-monitored tenant**.

- **Safe Mode (default ON).** All network/file/command targets must come from a vetted allow-list of
  **officially sanctioned, harmless** test artefacts:
  - [EICAR](https://www.eicar.org/) anti-malware test file & string
  - [GTUBE](https://spamassassin.apache.org/gtube/) anti-spam test string
  - Microsoft **SmartScreen** demo URLs (`demo.smartscreen.msft.net`)
  - **Network protection** test domain (`smartscreentestratings2.net`)
  - [AMTSO](https://www.amtso.org/security-features-check/) feature-check pages
  - Built-in discovery commands (`whoami`, `systeminfo`, …) and inert encoded-PowerShell / LOLBin patterns
- **Risk ceiling.** A global `MaxRiskLevel` (Safe → Low → Medium → High → Critical) caps how far any
  scenario can go. Genuinely destructive emulation (e.g. *really* disabling Defender) requires **both**
  Safe Mode OFF **and** an explicit `Critical` ceiling — and on an E5 endpoint with Tamper Protection
  that attempt is blocked and alerts instead of succeeding.
- **Reversible artefacts.** Persistence writes a clearly-labelled `EvilEndUserSimPersistence` value
  under `HKCU\…\Run`; dropped files live under your Downloads/working directory.
- **Dry-run** mode logs every action without executing it.

No real malware is ever used. See `EvilEndUser.Core/Safety/TestPayloads.cs` for the full catalogue.

Use the cleanup script after a run:

```powershell
pwsh ./scripts/Cleanup.ps1
```

---

## 🚀 Getting started

### Prerequisites

- Windows 10 19041+ / Windows 11 (or Windows 365 Cloud PC)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Microsoft Edge and/or Google Chrome installed
- (For email/Teams) an M365 account in the tenant under test
- (For LLM mode) an Azure OpenAI deployment or OpenAI/Ollama endpoint

### Build

```powershell
dotnet build EvilEndUser.sln -c Release
```

### Run

The app builds **unpackaged & self-contained**, so you can just run the executable:

```powershell
dotnet build src/EvilEndUser.App/EvilEndUser.App.csproj -c Release
& "src/EvilEndUser.App/bin/Release/net9.0-windows10.0.26100.0/win-x64/EvilEndUser.App.exe"
```

On first browser use, install the Playwright browsers if needed:

```powershell
pwsh src/EvilEndUser.Automation/bin/Release/net9.0-windows10.0.19041.0/playwright.ps1 install msedge
```

---

## ⚙️ Configuration

Everything is configured from the **Configuration** tab in the app:

- **Identity & targets** — your signed-in mailbox, Entra tenant ID, external recipient(s) used for
  self-exfiltration / BEC lures, and the working directory for drops & exports.
- **Desktop apps to drive** — toggle Browser (Edge/Chrome), Outlook/Graph, Teams/Graph, process
  execution, and filesystem.
- **Microsoft Graph sign-in** — uses the **device-code flow**; the first email/Teams action prints a
  code + `aka.ms/devicelogin` URL to the Activity Log.
- **LLM brain** — provider, endpoint, deployment/model, optional API key (leave blank to use Azure
  credential / device sign-in), and base temperature.

### LLM setup (Azure OpenAI)

```bash
az cognitiveservices account create -n my-aoai -g my-rg --kind OpenAI --sku S0 -l eastus2
az cognitiveservices account deployment create -n my-aoai -g my-rg \
  --deployment-name gpt-4o --model-name gpt-4o --model-version "2024-11-20" \
  --model-format OpenAI --sku-name Standard --sku-capacity 80
```

Then set **Provider = AzureOpenAI**, **Endpoint = https://my-aoai.openai.azure.com/**, and
**Deployment = gpt-4o** in the Configuration tab.

---

## 🔬 Validating detections

1. Run a scenario (or the full kill chain) from the **Dashboard** / **Scenarios** tab.
2. Watch the **Activity Log**; risky actions are flagged and tagged with their MITRE technique ID.
3. **Export** the log to JSON/CSV.
4. In the **Microsoft Defender portal**, correlate the resulting alerts/incidents against your
   exported simulator timeline to confirm coverage and measure time-to-detect.

---

## 📁 Project layout

| Path | Purpose |
|---|---|
| `EvilEndUser.Core/Models` | `SimAction`, `SimulationEvent`, `SimulationConfig`, `RiskLevel`, MITRE models |
| `EvilEndUser.Core/Safety` | `TestPayloads` catalogue + `SafetyGuard` enforcement |
| `EvilEndUser.Core/Engine` | `SimulationEngine` + `ScenarioContext` (the action funnel) |
| `EvilEndUser.MitreAttack` | `TechniqueCatalog`, `ScenarioRegistry`, per-tactic scenarios |
| `EvilEndUser.Automation/Drivers` | Browser, Graph email/Teams, process, filesystem drivers |
| `EvilEndUser.LLM` | `SemanticKernelAgentBrain`, `UserActionPlugin`, persona prompts |
| `EvilEndUser.App` | WinUI 3 pages: Dashboard, Scenarios, Activity Log, Configuration, About |

---

## ⚖️ Disclaimer

This software is provided for **authorised security testing and education only**. You are solely
responsible for ensuring you have permission to run it against any system or tenant. The authors
accept no liability for misuse. See [LICENSE](LICENSE).
