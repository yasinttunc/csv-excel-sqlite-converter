#!/usr/bin/env python3
"""CI content checks: source code under src/ is in English (no Turkish letters), and no tracked file is over 1 MB.
Sample data and tests may contain Turkish text on purpose (they test Turkish CSV files)."""
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
TURKISH = re.compile("[çğıöşüÇĞİÖŞÜ]")
findings = []
files = subprocess.run(["git", "ls-files", "-z"], cwd=ROOT, check=True, capture_output=True).stdout.decode().split("\0")
for name in filter(None, files):
    path = ROOT / name
    if not path.is_file():
        continue
    if path.stat().st_size > 1024 * 1024:
        findings.append(f"{name}: larger than 1 MB")
    if name.startswith("src/") and path.suffix in {".cs", ".xaml"}:
        for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            if TURKISH.search(line):
                findings.append(f"{name}:{number}: Turkish letter in source code")
print("\n".join(findings))
print(f"Content checks: {len(findings)} finding(s).")
sys.exit(1 if findings else 0)
