# Security Policy & Responsible Use

## Intended use

**Evil End User** is a security *testing* tool. It is intended to be run **only** on endpoints and
Microsoft 365 tenants that you own or are explicitly authorised to test, for the purpose of
validating Microsoft Defender for Endpoint / Defender for Office 365 detections.

## Built-in safeguards

- **Safe Mode (default):** all activity is restricted to officially sanctioned, harmless test
  artefacts (EICAR, GTUBE, Microsoft SmartScreen demo URLs, AMTSO checks, inert commands). No real
  malware is used.
- **Risk ceiling:** a global maximum risk level caps how far scenarios go.
- **Reversible artefacts** and a `scripts/Cleanup.ps1` helper.
- **Dry-run** mode to preview actions without executing them.

## What this tool does NOT do

- It does not download, generate, or execute real malware.
- It does not exfiltrate real user data (collection scenarios create decoy files only).
- It does not persist credentials or secrets to disk.

## Reporting a vulnerability

If you discover a security issue in this project (for example a way Safe Mode could be bypassed to
execute a genuinely harmful action), please open a private security advisory on the GitHub repository
rather than a public issue.

## Your responsibility

You are solely responsible for obtaining authorisation before running this tool against any system
or tenant, and for complying with all applicable laws and organisational policies.
