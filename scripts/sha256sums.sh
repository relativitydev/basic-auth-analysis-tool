#!/usr/bin/env bash
# Composes a SHA256SUMS file from hash lines and artifacts, sorted by file name, so that the same inputs always
# give the same bytes. Later packaging steps add their own lines (for example a disk image) by running it again
# with all the inputs.
#
# Usage:
#   sha256sums.sh --output <file> [--hash-lines <file>]... [--artifact <file>]...
#
#   --hash-lines  a file of lines in sha256sum format ("<64 hex characters>  <file name>"), as build-release writes them
#   --artifact    a file whose SHA-256 is computed here and listed under its own file name
#
# A file name that appears twice is an error. The output is written with LF line endings.
# Exit codes: 0 written, 1 an input was invalid, 2 invalid arguments.

set -u

readonly EXIT_FAILED=1
readonly EXIT_USAGE=2
readonly HASH_LINE_PATTERN='^[0-9a-f]{64}  [^ /\\][^/\\]*$'
readonly SORT_LOCALE="C"

output_path=""
hash_line_files=()
artifact_files=()

parse_arguments() {
	while [ $# -gt 0 ]; do
		case "$1" in
			--output) output_path="${2:-}" ;;
			--hash-lines) hash_line_files+=("${2:-}") ;;
			--artifact) artifact_files+=("${2:-}") ;;
			*) echo "unknown argument: $1"; return 1 ;;
		esac
		shift 2 || { echo "missing value for the last argument"; return 1; }
	done
	[ -n "$output_path" ] || { echo "--output is required"; return 1; }
	[ $(( ${#hash_line_files[@]} + ${#artifact_files[@]} )) -gt 0 ] || { echo "give at least one --hash-lines or --artifact"; return 1; }
}

hash_of_file() {
	if command -v sha256sum > /dev/null 2>&1; then
		sha256sum "$1" | cut -d ' ' -f 1
	else
		shasum -a 256 "$1" | cut -d ' ' -f 1
	fi
}

collect_lines() {
	local file line
	for file in "${hash_line_files[@]+"${hash_line_files[@]}"}"; do
		[ -f "$file" ] || { echo "hash-line file not found: $file" >&2; return 1; }
		while IFS= read -r line || [ -n "$line" ]; do
			line="${line%$'\r'}"
			[ -n "$line" ] || continue
			echo "$line" | grep -Eq "$HASH_LINE_PATTERN" || { echo "not a sha256sum line in $file: $line" >&2; return 1; }
			echo "$line"
		done < "$file"
	done
	for file in "${artifact_files[@]+"${artifact_files[@]}"}"; do
		[ -f "$file" ] || { echo "artifact not found: $file" >&2; return 1; }
		echo "$(hash_of_file "$file")  $(basename "$file")"
	done
}

main() {
	parse_arguments "$@" || return $EXIT_USAGE
	local lines sorted duplicates
	lines="$(collect_lines)" || return $EXIT_FAILED
	sorted="$(echo "$lines" | LC_ALL=$SORT_LOCALE sort -k2)"
	duplicates="$(echo "$sorted" | cut -d ' ' -f 3- | LC_ALL=$SORT_LOCALE sort | uniq -d)"
	[ -z "$duplicates" ] || { echo "file name listed more than once: $duplicates"; return $EXIT_FAILED; }
	printf '%s\n' "$sorted" > "$output_path"
	echo "wrote $(echo "$sorted" | wc -l | tr -d ' ') line(s) to $output_path"
}

main "$@"
exit $?
