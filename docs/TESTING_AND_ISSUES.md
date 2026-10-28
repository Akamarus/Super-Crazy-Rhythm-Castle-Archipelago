# Reporting bugs and helping test

Use the latest published release and its [installation guide](INSTALL.md). Back up saves before changing builds; a compatible client update does not require erasing your current run.

For a report, include:

- Client and APWorld versions, AP difficulty and whether you play Regular or Pro.
- Where the problem happened, expected behavior, actual behavior and whether it repeats.
- `BepInEx/LogOutput.log` from that run, plus screenshots/video when useful.
- For movement problems, a video and reproduction steps; F8 recording was removed in v0.30.0.

Do not post passwords or private connection details. Do not edit saves or inject checks to reproduce a bug unless you are deliberately testing a backed-up local run.

Tests should target changed behavior and a clear expected result. Completed tests belong in [TESTING.md](TESTING.md); unresolved defects and coverage limits belong in [KNOWN_ISSUES.md](KNOWN_ISSUES.md). The [historical public checklist](archive/checkpoints/docs-TESTING_AND_ISSUES.md) is retained for reference, not as a current to-do list.
