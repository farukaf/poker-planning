"""Fail CI if the machine-readable NuGet audit reports errors or vulnerabilities."""
import json
import sys

with open(sys.argv[1], encoding="utf-8-sig") as report:
    audit = json.load(report)
if audit.get("problems"):
    raise SystemExit(f"NuGet audit errors: {audit['problems']}")
for project in audit.get("projects", []):
    for framework in project.get("frameworks", []):
        for package in framework.get("topLevelPackages", []) + framework.get("transitivePackages", []):
            if package.get("vulnerabilities"):
                raise SystemExit(f"Vulnerable dependency: {package['id']}")
print("NuGet audit: no known vulnerabilities in direct or transitive packages.")
