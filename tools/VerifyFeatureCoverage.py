"""Audit catalog counts and publish an evidence checklist, without inferring parity from code."""
import collections
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
BASE = ROOT / "docs/validation"
ledger_path = BASE / "integrated-spec.json"
ledger = json.loads(ledger_path.read_text(encoding="utf-8-sig"))
features = ledger["features"]
ids = [f["id"] for f in features]
assert len(ids) == len(set(ids)) == ledger["baselineDenominator"] == 108
counts = collections.Counter(f["implementationState"] for f in features)
assert counts["partial"] == ledger["partialImplementationCount"]
assert sum(f["fullParityPassed"] for f in features) == ledger["fullParityCount"] == 0
# Existing rows carry no per-case implementation pass evidence. Do not promote mappings.
rows = []
for f in features:
    rows.append({
        "id": f["id"], "name": f["name"],
        "partialSourceMapping": f["implementationState"] == "partial",
        "documentSpecification": f.get("analysisStatus"),
        "scenarios": f.get("documentationSpec", {}).get("comparisonScenarios", []),
        "gates": [{"requirement": g, "implementation": "unverified", "parity": "unverified", "evidence": []} for g in f["requiredGates"]],
        "implementationCompleteVerified": False, "parityCompleteVerified": False,
    })
report = {
    "scope": "108 public catalog features; not all VA settings or product requirements",
    "ledgerSHA256": hashlib.sha256(ledger_path.read_bytes()).hexdigest(),
    "denominator": len(rows), "documentedPartial": ledger["documentedPartialCount"],
    "partialSourceMappings": counts["partial"], "partialSourceMappingPercent": round(counts["partial"]*100/len(rows), 1),
    "implementationCompleteVerified": 0, "verifiedImplementationPercent": 0,
    "actualImplementationPercent": None,
    "parityCompleteVerified": 0, "verifiedParityPercent": 0,
    "actualParityPercent": None,
    "rules": [
        "A mapping, command registration, screenshot or unit-test count does not constitute a completed feature.",
        "Implementation completion requires every agreed scenario and applicable gate to pass in CodeAssist; untested or failed gates block completion.",
        "Parity additionally requires VA2630 and CodeAssist isolated runs on the same inputs, settings and IDE version, with compared results and artifacts.",
        "Record versions, extension DLL SHA256, fixture SHA256, input/caret, expected and observed output, settings, keys, Undo and timings per scenario.",
        "Record non-applicable gates with a reason; never silently skip a required gate.",
        "Keep cold/warm startup, p50/p95 latency, CPU and memory separate; establish the acceptance budget from measured VA results before evaluating CodeAssist.",
        "Hash and retain raw logs/screenshots/result files and reject evidence from a different build or modified fixture.",
        "New requirements expand a versioned denominator; never shrink the denominator to implemented features.",
        "This audit checks the existing ledger and generates unverified checklists; it does not execute VA or certify evidence automatically.",
    ],
    "additionalProductRequirements": [],
    "features": rows,
}
# Keep separately verified product requirements outside the frozen catalog denominator.
welcome_path = BASE / "welcome-live-ui-0.0.61.json"
if welcome_path.exists():
    welcome = json.loads(welcome_path.read_text(encoding="utf-8-sig"))
    log_path = BASE / "welcome-native-0.0.61.log"
    screenshot_path = BASE / welcome["Screenshot"]
    log = log_path.read_text(encoding="utf-8-sig")
    verified = (welcome["Status"] == "PassedActualPcControl" and screenshot_path.is_file()
                and "CreatedDocumentFrame type=1" in log and "ClosedDocumentFrame" in log
                and "ReusedDocumentFrame type=1" in log and welcome["MVID"] in log)
    assert verified, "Welcome runtime evidence is missing or inconsistent"
    report["additionalProductRequirements"].append({
        "name": "Welcome opens as editor document tab", "state": "runtime-verified",
        "version": welcome["Version"], "checks": welcome["Checks"],
        "evidence": [{"path": p.name, "sha256": hashlib.sha256(p.read_bytes()).hexdigest()}
                     for p in (welcome_path, log_path, screenshot_path)],
        "scope": "Separate product requirement, not a completed VA catalog feature",
    })
# Retain actual editor evidence without promoting a whole catalog feature.
editor_path = BASE / "editor-integration-0.0.73.json"
if editor_path.exists():
    editor = json.loads(editor_path.read_text(encoding="utf-8-sig"))
    assert editor["version"] == "0.0.73" and len(editor["cases"]) == 14
    assert len({c["name"] for c in editor["cases"]}) == 14 and all(c["passed"] for c in editor["cases"])
    assert not editor.get("error") and not editor.get("cleanupError")
    evidence = {"path": editor_path.name, "sha256": hashlib.sha256(editor_path.read_bytes()).hexdigest(), "mvid": editor["mvid"]}
    report["additionalProductRequirements"].append({"name": "Isolated actual VS editor integration", "state": "runtime-verified", "version": editor["version"], "checks": [c["name"] for c in editor["cases"]], "evidence": [evidence], "scope": editor["scope"]})
    for row in rows:
        if row["id"] in ("VA-F043", "VA-F046", "VA-F055"):
            row["implementationEvidence"] = [evidence]
            row["implementationEvidenceScope"] = "Selected editor scenarios passed; other options, languages and VA comparison unverified"
report["requiredGateCount"] = sum(len(row["gates"]) for row in rows)
report["unverifiedGateCount"] = sum(g["implementation"] == "unverified" for row in rows for g in row["gates"])
report["unmappedFeatureCount"] = len(rows) - counts["partial"]
(BASE / "feature-verification-matrix.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
print(f"Catalog audit: {len(rows)} unique features, {counts['partial']} partial mappings; verified implementation 0, verified parity 0. Actual percentages unknown.")
