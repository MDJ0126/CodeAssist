# VA / CodeAssist automatic comparison runner

Test-only VSIX, separate from shipping CodeAssist 0.0.86. The default suite contains 108 feature IDs with one limited probe per ID. It is **not 108 complete functional tests**.

## Scope

- 11 editor command samples inspect actual buffers, cursor, selection and relevant assertions.
- Other probes open/cancel a product command dialog or inspect a named options page.
- Opening a page or cancelling a dialog does not validate its feature or settings persistence.
- Opaque native options controls and unavailable command candidates are recorded as unobserved/error, never passed.
- Physical shortcuts, pixel appearance, indexing correctness, cross-file refactors, debug sessions, performance and complete feature gates need dedicated fixtures.

## Build / execute

```powershell
& 'C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe' tools/ParityRunner/ParityRunner.csproj /t:Rebuild /restore /p:Configuration=Release /p:VisualStudioVersion=17.0 /p:DeployExtension=false /p:CopyVsixExtensionFiles=false
./tools/ParityRunner/Run.ps1 -Python python -Install -Launch -Visible -TimeoutSeconds 420
python tools/ParityRunner/parity.py self-test
python tools/ParityRunner/parity.py compare .local/parity-runs/<run-id>
```

Save and close existing VS instances first. The script does not kill or reuse them. Normal profile targets VA; Exp targets CA. Profile names alone do not prove extension isolation; independent baseline confirmation is required. The command inventory may be empty and must not be treated as proof of enabled extensions.

The test package uses opted-in environment variables, role-specific generated solutions and a run ownership marker. It refuses to edit outside its own fixtures. Resume preserves previous rows and their original process IDs, archives old reports, and generates new PID-scoped files.

## Dialog client

A separate MTA executable observes only the explicitly launched process ID. It checks the current case ID before acting, reads controls and cancels recognized product dialogs. It never applies refactors, submits files, starts downloads or changes security/license settings. Late/stale clients are rejected. Some native controls are inaccessible; these produce missing observations rather than success.

A watchdog writes STOP if the deadline expires. A synchronous modal command can still require cancellation. OwnedHostLifecycle saves and gracefully quits only solutions/documents under the owned test directory. It refuses unrelated workspaces.

## Evidence

VA/CA.plan.json contain identical scenarios and input hashes. VA/CA.results.json contain incremental snapshots, errors and timing. Separate case-bound dialog JSON includes run, role, case and original process IDs. Comparison rejects stale plans, missing/duplicate IDs, incorrect documents, matching errors and no-ops. Only independently asserted editor output can be scenario-match; that still does not certify a complete feature.

Full feature parity remains uncertified and overall product similarity remains unknown. The tool does not decompile VA and is not REA.

## Automatic screenshot collection (0.7.1)

Pass `-Screenshots` to Run.ps1. The test program captures the explicitly owned VS main window before and after every case, and the owned modal/menu immediately before cancellation. PrintWindow with PW_RENDERFULLCONTENT targets that window; it does not capture the whole desktop or another app. HWND process IDs are checked, and failed captures retain error JSON. Window rendering can still be incomplete; image existence alone is not valid evidence.

`python tools/ParityRunner/screenshots.py .local/parity-runs/<run-id>` validates decoded images, dimensions, process/case/phase metadata and blank images, writes a paired full-size gallery and 12-case contact sheets. The fixed before/after denominator is 108 × 2 products × 2 phases = 432. Dialog shots are additional. Coverage is valid screenshots / 432, **not product similarity**. A wrong options page still invalidates the relevant functional observation even when its image is technically valid.

The initial external Computer Use collector failed filesystem permissions; that abandoned attempt is retained under 20261018-015407-139. The 0.7.1 test-program capture batch is 20261018-015759-745. Product CodeAssist 0.0.86 is unchanged.

## Fixed denominator report (108)
Use `./tools/ParityRunner/RunAcceptance108.ps1 -TargetProcessId <dedicated CA VS PID>`. It runs the strict physical editor checks, recomputes judgments from raw VA/CA snapshots, copies paired screenshots, validates VA evidence hashes, and produces one HTML/CSV/JSON report for all 108 catalog rows. Only failure summaries need agent review. A failed live run retains its actual failed cases; a missing live run never silently falls back to old passing evidence. The current adapters cover four representative editor scenarios and the recorded default Rename-key accessibility difference. Other legacy options/dialog-cancellation probes remain unverified functional cases. All 108 rows being accounted for is not 108 execution tests or full feature certification. Main-scenario evidence does not certify boundary cases, other languages, defaults, performance or all gates. The score is a fixed-denominator confirmed evidence count, not a guessed whole-product similarity. `Aggregate108.py --self-test` rejects changed inputs, no-ops, missing snapshots, altered VA expectations and wrong Undo.


097 expansion: seven actual VA editor-output scenarios now run in one batch (six distinguishing catalog entries). The shorthand prefix-only sample is not counted as a Shorthand feature pass. Completion and zoom-reset are newly tested. CRLF buffer offsets are converted before DTE character movement. Zoom comparison checks the real 140-to-100 change. Current self-tests: 11 comparator, 20 aggregation. Product remains 0.0.94.

098 paired runtime diagnostics: `ShortcutProbe/paired-plan.json` has 13 live scenarios. `PairedScenarios.cs` requires an owned Parity solution and adds generated files to its C++ project, because VA refactors are disabled for some loose files. `PairedDialogWorker.cs` applies only explicitly named generated-fixture sort/refactor prompts. Run VA and CA sequentially to avoid foreground input interference. `AggregatePaired.py` requires identical plan hashes, validates extension isolation, compares real text/caret/selection/Undo and keeps unavailable commands and matching no-ops unverified. The report retains the fixed 108 denominator. Product remains 0.0.94.

## Default bounded runner (099)
Use `RunBounded.ps1 -VaProcessId <VA-only PID> -CaProcessId <CA-only PID> -Plan <json>`. Build once before the batch. One VA/CA pair shares a 5-second item deadline; the supervisor reserves 0.7 seconds for cleanup/persistence. COM/UIA/capture occur in a disposable diagnostic child. Only that child is killed on timeout; uncertain hosts are quarantined. The observer is read-only and the supervisor owns one token/PID-scoped WM_CLOSE. Remaining98.exe, Remaining98Repair.exe and RemainingCancel.exe are retired stubs (exit 95). `test_bounded.py` exercises hung/crashed workers, shared deadlines, continuation, stale evidence, no-op and incomplete assertion rejection. `bounded-099.html` links synthetic and real timing evidence. This does not add missing feature assertion adapters or certify product parity.

## Read-only fast gate (0.0.116)

`./tools/ParityRunner/RunReadOnlyGate.ps1 -OutputDirectory <absolute repository output folder>`

This path does not activate windows, send input, invoke VS commands, or capture screens. It runs Core tests into a fresh TRX, checks source versions, package versus Release binaries, installed binary hashes, the identity of the owned CA process, and fixed 108 catalog IDs. The CLR module probe uses a ClrMD process snapshot and CLR EnumerateModules, guarded by the owned CA receipt, live RootSuffix, PID and process start time. It verifies the loaded module path and SHA256 of its backing file; it does not claim to hash relocated in-memory bytes. Missing processes, probe failures and mismatches remain unconfirmed. Build the probe once with `dotnet build tools/ManagedModuleProbe -c Release`; run its isolated fixture check with `dotnet tools/ManagedModuleProbe/bin/Release/net8.0-windows/ManagedModuleProbe.dll --self-test <output.json>`. The snapshot does not send input, activate windows, execute VS commands or export a heap dump; it can briefly affect the inspected process. Sources: https://github.com/microsoft/clrmd/blob/main/doc/FAQ.md and https://github.com/microsoft/clrmd/blob/main/doc/GettingStarted.md. Exit codes: 0 = all machine checks passed; 1 = failed check; 2 = unresolved machine check. None of these codes certify VA parity. The report accounts for every catalog requirement without promoting old screenshots or missing feature gates to new passes.

Comparator hardening (2026-10-23): Aggregate108 now requires VA before-state evidence to match the planned input and rejects VA Undo that does not restore input text, even when CA repeats the same broken Undo. Self-test: 24 adversarial cases. Historical reports are not automatically recertified.

Offline archive audit: `python tools/ParityRunner/AuditHistoricalBehavior.py --output docs/validation/historical-reassessment-116`. This verifies archived input/result receipts and applies the comparator and acceptance-eligibility gate. It reports zero new runtime executions and does not claim current-product parity.
