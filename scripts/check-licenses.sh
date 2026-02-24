#!/usr/bin/env bash
# Eclipse Dash License Check for iceoryx2-csharp
#
# Runs the Eclipse Dash License Tool per-project to verify NuGet dependency licenses.
# Usage: ./scripts/check-licenses.sh [path-to-dash-licenses.jar]
#
# Requires: Java 11+, .NET SDK, dotnet CLI

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

DASH_JAR="${1:-$REPO_ROOT/tools/org.eclipse.dash.licenses.jar}"

if [[ ! -f "$DASH_JAR" ]]; then
    echo "ERROR: Dash License Tool JAR not found at: $DASH_JAR"
    echo "Download it from: https://repo.eclipse.org/service/local/artifact/maven/redirect?r=dash-licenses&g=org.eclipse.dash&a=org.eclipse.dash.licenses&v=LATEST"
    exit 1
fi

if ! command -v java &>/dev/null; then
    echo "ERROR: Java is required but not found. Install Java 11+."
    exit 1
fi

if ! command -v dotnet &>/dev/null; then
    echo "ERROR: dotnet CLI is required but not found."
    exit 1
fi

DEPENDENCIES_FILE="$REPO_ROOT/DEPENDENCIES"

# Projects with NuGet PackageReference dependencies to check
PROJECTS=(
    "src/Iceoryx2/Iceoryx2.csproj"
    "src/Iceoryx2.Reactive/Iceoryx2.Reactive.csproj"
    "tests/Iceoryx2.Tests.csproj"
    "examples/LoggingIntegration/LoggingIntegration.csproj"
    "examples/ObservableWaitSet/ObservableWaitSet.csproj"
    "examples/ReactiveExample/ReactiveExample.csproj"
    "examples/ReactiveEventExample/ReactiveEventExample.csproj"
)

# Convert dotnet list package output to ClearlyDefined format.
# Handles both top-level (3 columns: Name Requested Resolved)
# and transitive (2 columns: Name Resolved) output formats.
# Uses awk for portability (BSD sed lacks \b and \s support).
# Filters out Microsoft, NETStandard, NuGet, System, and runtime packages
# per the standard dash-licenses .NET example.
convert_to_clearlydefined() {
    grep ">" \
        | grep -v -E '>\s+(Microsoft|NETStandard|NuGet|System|runtime)\.' \
        | awk '{ for(i=NF; i>=1; i--) if($i ~ /^[0-9]+\.[0-9]+\.[0-9]/) { print "nuget/nuget/-/"$2"/"$i; break } }' \
        | sort -u
}

echo "============================================="
echo "Eclipse Dash License Check - iceoryx2-csharp"
echo "============================================="
echo ""

# Collect all unique dependencies across all projects
ALL_DEPS_FILE=$(mktemp)
trap 'rm -f "$ALL_DEPS_FILE"' EXIT

for project in "${PROJECTS[@]}"; do
    project_path="$REPO_ROOT/$project"
    if [[ ! -f "$project_path" ]]; then
        echo "WARNING: Project not found: $project_path (skipping)"
        continue
    fi

    echo "--- Checking: $project ---"

    # Collect both top-level and transitive packages
    dotnet list "$project_path" package --include-transitive 2>/dev/null \
        | convert_to_clearlydefined \
        >> "$ALL_DEPS_FILE" || true

    echo ""
done

# Deduplicate
sort -u "$ALL_DEPS_FILE" -o "$ALL_DEPS_FILE"

TOTAL=$(wc -l < "$ALL_DEPS_FILE" | tr -d ' ')
echo "Total unique dependencies to check: $TOTAL"
echo ""

if [[ "$TOTAL" -eq 0 ]]; then
    echo "No dependencies found. Nothing to check."
    exit 0
fi

echo "Dependencies to check:"
cat "$ALL_DEPS_FILE"
echo ""
echo "Running Eclipse Dash License Tool..."
echo ""

# Run dash-licenses with extended timeout and smaller batch size
# (ClearlyDefined API can be slow with large batches)
java -jar "$DASH_JAR" -timeout 240 -batch 20 -summary "$DEPENDENCIES_FILE" - < "$ALL_DEPS_FILE" 2>&1

echo ""
echo "============================================="
echo "Results written to: $DEPENDENCIES_FILE"
echo "============================================="

# Check for restricted dependencies
if grep -q "restricted" "$DEPENDENCIES_FILE" 2>/dev/null; then
    echo ""
    echo "WARNING: Some dependencies have restricted licenses."
    echo "You may need to create issues at https://gitlab.eclipse.org/eclipsefdn/emo-team/iplab"
    echo ""
    echo "Restricted dependencies:"
    grep "restricted" "$DEPENDENCIES_FILE"
fi

echo ""
echo "Done."
