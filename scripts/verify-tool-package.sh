#!/usr/bin/env bash
# Verifies a packed BlogDoFT.Ciir .NET tool package (see .specs/02-dotnet-tool.md, sections 4.4 and 10):
#   1. the .nupkg has the required contents and none of the forbidden ones;
#   2. the package installs as a tool and `ciir --version` reports the package version;
#   3. the installed tool analyzes fixtures/BasicSolution end to end.
#
# Usage: scripts/verify-tool-package.sh <version> [artifacts-dir]
#   e.g. dotnet pack src/Ciir.Cli -c Release -p:Version=0.2.0 -o artifacts
#        scripts/verify-tool-package.sh 0.2.0
set -euo pipefail

version="${1:?usage: verify-tool-package.sh <version> [artifacts-dir]}"
artifacts="${2:-artifacts}"
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
package="$artifacts/BlogDoFT.Ciir.$version.nupkg"

fail() { echo "::error::$*" >&2; exit 1; }

[[ -f "$package" ]] || fail "package not found: $package"

work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT

echo "== 1. Package contents"
listing="$work_dir/listing.txt"
unzip -l "$package" > "$listing"

for required in \
  "tools/net10.0/any/DotnetToolSettings.xml" \
  "tools/net10.0/any/ciir.dll" \
  "tools/net10.0/any/Ciir.Serialization.dll" \
  "tools/net10.0/any/BuildHost-netcore/Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.dll"; do
  grep -qF -- "$required" "$listing" || fail "package is missing $required"
done

# The MSBuildLocator loads MSBuild from the installed SDK; bundling it would cause version conflicts.
if grep -E 'Microsoft\.Build(\.Framework|\.Utilities\.Core)?\.dll' "$listing"; then
  fail "package must not bundle Microsoft.Build assemblies"
fi

echo "== 2. Install as a tool and check the version"
tool_path="$work_dir/tool"
dotnet tool install BlogDoFT.Ciir --tool-path "$tool_path" --source "$artifacts" --version "$version" >/dev/null

reported="$("$tool_path/ciir" --version)"
[[ "$reported" == "$version"* ]] || fail "ciir --version reported '$reported', expected '$version'"
echo "ciir --version -> $reported"

echo "== 3. Analyze fixtures/BasicSolution with the installed tool"
fixture="$work_dir/BasicSolution"
cp -r "$repo_root/fixtures/BasicSolution" "$fixture"
rm -rf "$fixture/bin" "$fixture/obj"
# The fixture inherits fixtures/Directory.Build.props; keep it next to the copy.
cp "$repo_root/fixtures/Directory.Build.props" "$work_dir/Directory.Build.props"
dotnet restore "$fixture" >/dev/null

output="$work_dir/ciir-output"
"$tool_path/ciir" "$fixture" --output "$output" --fail-on-error

for artifact in ciir.jsonl ciir.schema.json manifest.json analysis-report.json; do
  [[ -s "$output/$artifact" ]] || fail "missing or empty output artifact: $artifact"
done

manifest_version="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["generator"]["version"])' "$output/manifest.json")"
[[ "$manifest_version" == "$version" ]] || fail "manifest generator.version is '$manifest_version', expected '$version'"

echo "OK: BlogDoFT.Ciir $version verified"
