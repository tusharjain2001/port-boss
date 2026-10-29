# Shared paths for the tools/ scripts (sourced, not run). Every value can be overridden from the environment.
TOOLS=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
PROJECT=${PROJECT:-$(cd "$TOOLS/.." && (pwd -W 2>/dev/null || pwd))}         # the Unity project (repo root)
UNITY=${UNITY:-"/c/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Unity.exe"}
PB_LOGS=${PB_LOGS:-$PROJECT/Logs/pb}                                        # build + test logs (Logs/ is gitignored)
RELEASE_DIR=${RELEASE_DIR:-$PROJECT/Builds/releases}                        # stamped releases (Builds/ is gitignored)
mkdir -p "$PB_LOGS"
