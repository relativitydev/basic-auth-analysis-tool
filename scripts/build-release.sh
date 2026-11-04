#!/usr/bin/env bash
# Builds the release artifact for one runtime identifier: a Native AOT publish of the command-line tool from the current commit.
#
# It refuses a working tree with modified tracked files, publishes the Release configuration for the requested runtime identifier,
# fails when the publish reports any warning, copies the artifact to the output folder as migrate-auth-<version>-<rid> (with .exe on Windows), and writes beside it the hash line
# (sha256sum format) and a small build record. It signs nothing, notarizes nothing, creates no release and no tag,
# and contacts no service other than what dotnet publish itself needs.
#
# Usage:
#   build-release.sh --rid <rid> --output-folder <folder> [--repository <folder>]
#
# The output folder must be outside the repository. Use one output folder for each runtime identifier.
# Exit codes: 0 built, 1 build or check failed, 2 invalid arguments.

set -u

readonly EXIT_FAILED=1
readonly EXIT_USAGE=2
readonly SCRIPT_FOLDER="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly DEFAULT_REPOSITORY="$(dirname "$SCRIPT_FOLDER")"
readonly PROJECT_RELATIVE_PATH="src/MigrateAuth.Cli/MigrateAuth.Cli.csproj"
readonly ARTIFACT_BASE_NAME="migrate-auth"
readonly WINDOWS_RID_PREFIX="win"
readonly WINDOWS_EXTENSION=".exe"
readonly VERSION_PATTERN='s:.*<Version>\([^<]*\)</Version>.*:\1:p'
readonly PUBLISH_LOG_NAME="publish.log"
readonly WARNING_PATTERN='warning [A-Za-z]+[0-9]+'
readonly ALLOWED_WARNING_COUNT=0
readonly SHA256_HEX_PATTERN='^[0-9a-f]{64}$'

rid=""
output_folder=""
repository="$DEFAULT_REPOSITORY"

parse_arguments() {
	while [ $# -gt 0 ]; do
		case "$1" in
			--rid) rid="${2:-}" ;;
			--output-folder) output_folder="${2:-}" ;;
			--repository) repository="${2:-}" ;;
			*) echo "unknown argument: $1"; return 1 ;;
		esac
		shift 2 || { echo "missing value for the last argument"; return 1; }
	done
	[ -n "$rid" ] && [ -n "$output_folder" ] || { echo "--rid and --output-folder are required"; return 1; }
}

published_binary_name() {
	case "$rid" in
		"$WINDOWS_RID_PREFIX"*) echo "${ARTIFACT_BASE_NAME}${WINDOWS_EXTENSION}" ;;
		*) echo "$ARTIFACT_BASE_NAME" ;;
	esac
}

artifact_name() {
	local version
	version="$(sed -n "$VERSION_PATTERN" "$repository/$PROJECT_RELATIVE_PATH" | head -n 1 | tr -d '\r')"
	[ -n "$version" ] || { echo "no <Version> in $PROJECT_RELATIVE_PATH" >&2; return 1; }
	case "$rid" in
		"$WINDOWS_RID_PREFIX"*) echo "${ARTIFACT_BASE_NAME}-${version}-${rid}${WINDOWS_EXTENSION}" ;;
		*) echo "${ARTIFACT_BASE_NAME}-${version}-${rid}" ;;
	esac
}

absolute_path() {
	(mkdir -p "$1" && cd "$1" && pwd)
}

output_is_outside_repository() {
	case "$1/" in
		"$repository"/*) return 1 ;;
	esac
	return 0
}

tracked_tree_is_clean() {
	[ -z "$(git -C "$repository" status --porcelain --untracked-files=no)" ]
}

hash_of_file() {
	if command -v sha256sum > /dev/null 2>&1; then
		sha256sum "$1" | cut -d ' ' -f 1
	else
		shasum -a 256 "$1" | cut -d ' ' -f 1
	fi
}

count_publish_warnings() {
	grep -Eo "[^ ]*${WARNING_PATTERN}[^ ]*" "$1" | sort -u | wc -l | tr -d ' '
}

run_publish() {
	local publish_folder="$1" log_path="$2"
	dotnet publish "$repository/$PROJECT_RELATIVE_PATH" -c Release -r "$rid" -o "$publish_folder" 2>&1 | tee "$log_path"
	return "${PIPESTATUS[0]}"
}

write_build_record() {
	local record_path="$1" commit="$2" warnings="$3" artifact_path="$4" artifact_hash="$5"
	{
		echo "commit: $commit"
		echo "runtime identifier: $rid"
		echo "dotnet sdk: $(dotnet --version)"
		echo "publish warnings: $warnings"
		echo "artifact: $(basename "$artifact_path")"
		echo "artifact size (bytes): $(wc -c < "$artifact_path" | tr -d ' ')"
		echo "sha256: $artifact_hash"
	} > "$record_path"
}

main() {
	parse_arguments "$@" || return $EXIT_USAGE
	repository="$(cd "$repository" && pwd)" || return $EXIT_USAGE
	output_folder="$(absolute_path "$output_folder")" || return $EXIT_USAGE
	output_is_outside_repository "$output_folder" || { echo "the output folder is inside the repository"; return $EXIT_USAGE; }
	tracked_tree_is_clean || { echo "the working tree has modified tracked files; commit or stash them first"; return $EXIT_FAILED; }

	local commit name published_name work_folder warnings artifact_hash
	commit="$(git -C "$repository" rev-parse HEAD)" || return $EXIT_FAILED
	name="$(artifact_name)" || return $EXIT_FAILED
	published_name="$(published_binary_name)"
	work_folder="$(mktemp -d)"
	run_publish "$work_folder/publish" "$output_folder/$PUBLISH_LOG_NAME" || { echo "publish failed"; return $EXIT_FAILED; }
	warnings="$(count_publish_warnings "$output_folder/$PUBLISH_LOG_NAME")"
	[ "$warnings" -eq "$ALLOWED_WARNING_COUNT" ] || { echo "the publish reported $warnings warning(s); none are allowed"; return $EXIT_FAILED; }
	[ -f "$work_folder/publish/$published_name" ] || { echo "the publish did not produce $published_name"; return $EXIT_FAILED; }

	cp "$work_folder/publish/$published_name" "$output_folder/$name"
	artifact_hash="$(hash_of_file "$output_folder/$name")"
	if ! echo "$artifact_hash" | grep -Eq "$SHA256_HEX_PATTERN"; then
		echo "could not compute the SHA-256 of $name: neither sha256sum nor shasum produced a valid hash"
		rm -f "$output_folder/$name"
		rm -rf "$work_folder"
		return $EXIT_FAILED
	fi
	echo "$artifact_hash  $name" > "$output_folder/$name.sha256"
	write_build_record "$output_folder/build-record-$rid.txt" "$commit" "$warnings" "$output_folder/$name" "$artifact_hash"
	rm -rf "$work_folder"
	echo "built $name for $rid from $commit: sha256 $artifact_hash, $warnings warnings"
}

main "$@"
exit $?
