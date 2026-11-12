"""Builds the fake agent and the sentinel folder, and prints the sentinel folder's path.

Usage: python -I tests/FakeAgent/make-sentinels.py [--output <folder outside the repository>] [--rid <runtime identifier>]

The fake agent is published self-contained and single-file for the platform it will run on, so it
starts under a replaced PATH without DOTNET_ROOT. Two folders are written under --output:

  fake-agent/   the published fake (fake-agent or fake-agent.exe); the tests copy it under the names
                they need, in their own temporary folder
  sentinels/    claude, codex and copilot, each a copy of the fake with a ".sentinel" marker beside it
                (.exe and .cmd on Windows, executables on Linux). An invocation writes "<file>.invoked"
                beside the copy and exits 97.

The last line printed on standard output is the sentinel folder's absolute path; everything else goes to
standard error. Put that folder FIRST on PATH for every test command. The folder is built outside the
repository and is never committed. A sentinel flag left from an earlier run stops the build: it means a
sentinel was invoked, and it is looked into before anything is rebuilt.
"""

import argparse
import platform
import shutil
import stat
import subprocess
import sys
import tempfile
from pathlib import Path

AGENT_NAMES = ("claude", "codex", "copilot")
SENTINEL_MARKER_SUFFIX = ".sentinel"
SENTINEL_FLAG_SUFFIX = ".invoked"
FAKE_BASE_NAME = "fake-agent"
FAKE_FOLDER_NAME = "fake-agent"
SENTINEL_FOLDER_NAME = "sentinels"
DEFAULT_OUTPUT_FOLDER_NAME = "migrate-auth-fake-agent"
WINDOWS_CMD_WRAPPER = '@"%~dp0{name}.exe" %*\r\n@exit /b %ERRORLEVEL%\r\n'
REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
PROJECT_PATH = REPOSITORY_ROOT / "tests" / "FakeAgent" / "FakeAgent.csproj"
EXIT_REFUSED = 2


def default_runtime_identifier() -> str:
	machine = platform.machine().lower()
	architecture = "arm64" if machine in ("arm64", "aarch64") else "x64"
	operating_system = "win" if sys.platform == "win32" else "linux"
	return f"{operating_system}-{architecture}"


def is_inside(path: Path, folder: Path) -> bool:
	return folder == path or folder in path.parents


def publish_fake(runtime_identifier: str, fake_folder: Path) -> Path:
	command = [
		"dotnet", "publish", str(PROJECT_PATH),
		"-c", "Release",
		"-r", runtime_identifier,
		"--self-contained", "true",
		"-p:PublishSingleFile=true",
		"-p:IncludeNativeLibrariesForSelfExtract=true",
		"-o", str(fake_folder),
	]
	completed = subprocess.run(command, stdout=sys.stderr, stderr=sys.stderr, check=False)
	if completed.returncode != 0:
		raise SystemExit(f"dotnet publish failed with exit code {completed.returncode}")
	suffix = ".exe" if runtime_identifier.startswith("win") else ""
	return fake_folder / (FAKE_BASE_NAME + suffix)


def refuse_when_a_flag_exists(sentinel_folder: Path) -> None:
	if not sentinel_folder.is_dir():
		return
	flags = sorted(path.name for path in sentinel_folder.glob("*" + SENTINEL_FLAG_SUFFIX))
	if flags:
		raise SystemExit("A sentinel was invoked (" + ", ".join(flags) + "); look into it before a rebuild.")


def write_sentinels(published_fake: Path, sentinel_folder: Path, windows: bool) -> None:
	sentinel_folder.mkdir(parents=True, exist_ok=True)
	for name in AGENT_NAMES:
		target = sentinel_folder / (name + (".exe" if windows else ""))
		shutil.copy2(published_fake, target)
		target.chmod(target.stat().st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)
		Path(str(target) + SENTINEL_MARKER_SUFFIX).write_text("sentinel\n", encoding="utf8")
		if windows:
			(sentinel_folder / (name + ".cmd")).write_bytes(WINDOWS_CMD_WRAPPER.format(name=name).encode("ascii"))


def main() -> int:
	parser = argparse.ArgumentParser(description="Builds the fake agent and the sentinel folder.")
	parser.add_argument("--output", default=str(Path(tempfile.gettempdir()) / DEFAULT_OUTPUT_FOLDER_NAME))
	parser.add_argument("--rid", default=default_runtime_identifier())
	arguments = parser.parse_args()

	output = Path(arguments.output).resolve()
	if is_inside(output, REPOSITORY_ROOT):
		print("--output must be outside the repository.", file=sys.stderr)
		return EXIT_REFUSED

	sentinel_folder = output / SENTINEL_FOLDER_NAME
	refuse_when_a_flag_exists(sentinel_folder)
	published_fake = publish_fake(arguments.rid, output / FAKE_FOLDER_NAME)
	write_sentinels(published_fake, sentinel_folder, arguments.rid.startswith("win"))
	print(sentinel_folder)
	return 0


if __name__ == "__main__":
	sys.exit(main())
