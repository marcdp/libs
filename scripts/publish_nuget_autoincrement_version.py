#!/usr/bin/env python3
"""Selective NuGet publisher for marcdp/libs.

Python 3.9+; standard library only. Requires dotnet 10 SDK for this repo.

## Design:
- Each src/**/*.csproj is the only authority for its PackageId and Version.
- Compare a deterministic fingerprint of package inputs with the latest stable
  package's <releaseNotes> in NuGet.org's published .nuspec.
- If changed, bump only that project's <Version> and publish it.
- Unchanged downstream packages are *not* republished. A breaking dependency
  change requires an explicit compatibility/release decision.
- Dry run (the default) NEVER changes files or uploads anything.
- --publish updates affected .csproj files, builds/tests, packs, and pushes.
- If publishing is interrupted, the next run reconciles against NuGet.org;
  a locally bumped but unpublished version is reused, not bumped again.

This tool targets the standard SDK-style projects currently in marcdp/libs.
Complex conditional MSBuild versions, custom pack targets, and indirect build
inputs may require extending discover_projects()/fingerprint().

## Commands

```powershell
# From the repository root: inspect 45+ package decisions. Read-only.
python scripts/publish_nuget_autoincrement_version.py

# On a clean, CI-verified main commit: publish changed packages.
$env:NUGET_API_KEY = "your-secret-nuget-org-api-key"
python scripts/publish_nuget_autoincrement_version.py --publish

# Explicit feature/breaking releases (patch is default):
python scripts/publish_nuget_autoincrement_version.py --bump minor --publish
python scripts/publish_nuget_autoincrement_version.py --bump major --publish

# Force a specific package even when its fingerprint matches:
python scripts/publish_nuget_autoincrement_version.py --force-package DProjects.Fs.Aws --publish

# Skip local test suite only when CI verified the exact commit:
python scripts/publish_nuget_autoincrement_version.py --skip-tests --publish

# Local experiments, intentionally allowing uncommitted changes:
python scripts/publish_nuget_autoincrement_version.py --allow-dirty --publish
```

**The default is a dry run.** It queries NuGet.org and prints the version
plan but does not modify files or upload packages.

## How it works

1. Discover packable projects under `src/**` and read `PackageId`, `Version`,
   and `ProjectReference` directly from each `.csproj`.
2. Compute a deterministic SHA-256 over the package directory, excluding
   `bin/`, `obj/`, `.git/`, etc., plus relevant parent/root build files.
   `<Version>` is normalized to `__VERSION__` so the publisher's own version
   increment does not change the fingerprint.
3. Query NuGet.org's [flat container API](https://learn.microsoft.com/nuget/api/package-base-address-resource)
   for existing versions and download the latest stable version's small `.nuspec`.
   Compare the marker `DProjects-Source-Fingerprint-SHA256-v1:<SHA256>` in its
   `<releaseNotes>` field.
4. If the fingerprint matches, **skip** the package. If no version has been
   published, publish the project's currently declared version (typically `3.0.0`).
   If source changed, increment only the affected `.csproj` version (patch by
   default, or explicit minor/major). If NuGet.org already has `3.0.0` without
   a fingerprint, the first fingerprinted release becomes `3.0.1` by default.
5. Update all affected `.csproj` versions first, then restore/build/test the
   solution. Pack **only** planned projects in dependency order, embedding
   each source fingerprint via the SDK's `PackageReleaseNotes` property.
6. Before upload, open each `.nupkg` and verify its `.nuspec` package identity,
   version, hash and direct project-reference dependency versions. Publish via
   `dotnet nuget push` using `NUGET_API_KEY`.
7. The script checks for remote version collisions before each push. After an
   interruption, it can reconcile packages already published and reuse the
   local version number for packages not yet uploaded.

## Dependency policy

This is the **minimal publishing** strategy: modifying `DProjects.Utils`
*does not* automatically publish every dependent package. Existing packages'
NuGet dependency ranges must remain compatible with new versions. For breaking
or incompatible dependency changes, explicitly decide which dependent packages
must be updated/released. New packages' generated `.nuspec` dependencies are
verified before uploading.

## Cautions

- Commit your source changes, pass CI, and run publishing from that same commit.
  The script checks the tracked and untracked working tree unless
  `--allow-dirty` is supplied.
- **Version updates remain in `.csproj` files after a failed release**, because
  NuGet.org publications cannot be rolled back atomically. Review and commit
  the updated `.csproj` files after a release; rerun to reconcile partial runs.
- The source fingerprint is a change detector, *not* a guarantee of binary
  reproducibility nor an API-compatibility analysis. SDK/compiler or shared
  input changes may cause many package updates.
- `PackageReleaseNotes` is used to hold the hash. Other release notes passed
  through that same MSBuild property will be overridden in the generated
  package. The repository currently doesn't declare package release notes.
- `--bump major` or `--bump minor` applies to packages that need publishing;
  it isn't an automatic breaking-change detector.
- No live external-provider integration tests are run by the default test
  command; its arguments follow the repository's CI.
- It queries/pushes to **NuGet.org**, not a private feed. A NuGet.org API key
  with permission to push the affected package IDs is required.
- Before first use, check that none of the release's version numbers already
  exists on NuGet.org and that the user/account owns each PackageId.
- Do not put your NuGet.org API key into the script or `.csproj` files.

"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
import zipfile
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List, Optional, Sequence, Set, Tuple

NUGET_FLAT = "https://api.nuget.org/v3-flatcontainer"
NUGET_PUSH = "https://api.nuget.org/v3/index.json"
FINGERPRINT_PREFIX = "DProjects-Source-Fingerprint-SHA256-v1:"
FINGERPRINT_RE = re.compile(re.escape(FINGERPRINT_PREFIX) + r"([a-fA-F0-9]{64})")
VERSION_RE = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$")
VERSION_ELEMENT_RE = re.compile(r"(<Version\s*>)([^<]*)(</Version\s*>)", re.IGNORECASE)
SKIP_DIRS = {"bin", "obj", ".git", ".vs", ".idea", "__pycache__", "TestResults", "node_modules", ".pytest_cache"}
SKIP_FILES = {".DS_Store", "Thumbs.db"}
SKIP_SUFFIXES = {".user", ".suo", ".tmp", ".nupkg", ".snupkg"}
COMMON_BUILD_FILES = {"global.json", "NuGet.Config", "nuget.config", "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "Directory.Packages.targets", "packages.lock.json"}


class PublishError(RuntimeError):
    pass


def tag_name(element: ET.Element) -> str:
    return element.tag.rsplit("}", 1)[-1]


def direct_property(root: ET.Element, key: str) -> Optional[str]:
    for group in root:
        if tag_name(group) != "PropertyGroup":
            continue
        for child in group:
            if tag_name(child) == key and child.text:
                return child.text.strip()
    return None


def version_tuple(value: str) -> Tuple[int, int, int]:
    match = VERSION_RE.fullmatch(value)
    if not match:
        raise PublishError(f"Expected a stable three-part version (X.Y.Z), got {value!r}")
    return tuple(int(x) for x in match.groups())  # type: ignore[return-value]


def increment_version(value: str, bump: str) -> str:
    major, minor, patch = version_tuple(value)
    if bump == "patch":
        patch += 1
    elif bump == "minor":
        minor += 1
        patch = 0
    elif bump == "major":
        major += 1
        minor = patch = 0
    else:
        raise PublishError(f"Unknown version increment: {bump}")
    return f"{major}.{minor}.{patch}"


@dataclass
class Project:
    path: Path
    package_id: str
    version: str
    dependencies: Tuple[Path, ...]
    fingerprint: str = ""


@dataclass
class Remote:
    latest_version: Optional[str]
    latest_fingerprint: Optional[str]
    all_versions: Set[str]


@dataclass
class Decision:
    project: Project
    action: str  # publish, skip, sync
    target_version: str
    reason: str
    remote: Remote


def discover_projects(root: Path) -> List[Project]:
    projects: List[Project] = []
    for path in sorted((root / "src").rglob("*.csproj")):
        parsed = ET.parse(path).getroot()
        if (direct_property(parsed, "IsPackable") or "true").lower() == "false":
            continue
        package_id = direct_property(parsed, "PackageId")
        version = direct_property(parsed, "Version")
        if not package_id or not version:
            raise PublishError(f"Project must declare PackageId and Version: {path}")
        version_tuple(version)
        references: List[Path] = []
        for element in parsed.iter():
            if tag_name(element) != "ProjectReference":
                continue
            include = element.get("Include", "").replace("\\", "/")
            if "$" in include:
                raise PublishError(f"Conditional/MSBuild ProjectReference not supported: {path}: {include}")
            if include:
                references.append((path.parent / include).resolve())
        projects.append(Project(path.resolve(), package_id, version, tuple(references)))

    if not projects:
        raise PublishError(f"No packable .csproj files found below {root / 'src'}")
    ids = [p.package_id.casefold() for p in projects]
    if len(ids) != len(set(ids)):
        raise PublishError("Duplicate NuGet PackageId found in project files")
    known = {p.path for p in projects}
    for project in projects:
        for dep in project.dependencies:
            if dep not in known:
                raise PublishError(f"Project reference outside discovered packable projects: {project.path} -> {dep}")
    return projects


def topological_order(projects: Sequence[Project]) -> List[Project]:
    by_path = {p.path: p for p in projects}
    state: Dict[Path, int] = {}
    output: List[Project] = []

    def visit(path: Path) -> None:
        if state.get(path) == 1:
            raise PublishError(f"ProjectReference cycle detected at {path}")
        if state.get(path) == 2:
            return
        state[path] = 1
        for dependency in sorted(by_path[path].dependencies):
            visit(dependency)
        state[path] = 2
        output.append(by_path[path])

    for project in sorted(projects, key=lambda p: p.package_id.lower()):
        visit(project.path)
    return output


def included_files(project_dir: Path):
    for dirpath, dirnames, filenames in os.walk(project_dir):
        dirnames[:] = sorted(x for x in dirnames if x not in SKIP_DIRS)
        for name in sorted(filenames):
            p = Path(dirpath) / name
            if (name in SKIP_FILES or p.suffix.lower() in SKIP_SUFFIXES
                    or p.is_symlink() or not p.is_file()):
                continue
            yield p


def normalized_project_bytes(path: Path) -> bytes:
    # Preserve all non-version project metadata, not only the XML node order.
    raw = path.read_bytes().decode("utf-8-sig")
    matches = list(VERSION_ELEMENT_RE.finditer(raw))
    if len(matches) != 1:
        raise PublishError(f"Expected precisely one literal <Version> in {path}, got {len(matches)}")
    return VERSION_ELEMENT_RE.sub(lambda m: m.group(1) + "__VERSION__" + m.group(3), raw).encode("utf-8")


def common_files(root: Path, project_dir: Path) -> List[Path]:
    result: List[Path] = []
    cursor = project_dir
    while True:
        for name in COMMON_BUILD_FILES:
            p = cursor / name
            if p.is_file():
                result.append(p)
        if cursor == root:
            break
        if root not in cursor.parents:
            raise PublishError(f"Project is outside repository root: {project_dir}")
        cursor = cursor.parent
    return sorted(set(result))


def fingerprint(root: Path, project: Project) -> str:
    digest = hashlib.sha256()
    paths = set(included_files(project.path.parent))
    paths.update(common_files(root, project.path.parent))
    for path in sorted(paths, key=lambda p: p.relative_to(root).as_posix()):
        relative = path.relative_to(root).as_posix().encode("utf-8")
        data = normalized_project_bytes(path) if path == project.path else path.read_bytes()
        # Size and path length avoid concatenation ambiguities.
        digest.update(len(relative).to_bytes(8, "big"))
        digest.update(relative)
        digest.update(len(data).to_bytes(8, "big"))
        digest.update(data)
    return digest.hexdigest()


def http_get(url: str) -> Optional[bytes]:
    headers = {"User-Agent": "DProjects-Libs-NuGetPublisher/1.0", "Accept": "application/json,application/xml,text/xml,*/*"}
    for attempt in range(3):
        try:
            request = urllib.request.Request(url, headers=headers)
            with urllib.request.urlopen(request, timeout=30) as response:
                return response.read()
        except urllib.error.HTTPError as exc:
            if exc.code == 404:
                return None
            if exc.code not in (429, 500, 502, 503, 504) or attempt == 2:
                raise PublishError(f"NuGet HTTP {exc.code}: {url}") from exc
        except (urllib.error.URLError, TimeoutError, OSError) as exc:
            if attempt == 2:
                raise PublishError(f"Cannot query NuGet ({url}): {exc}") from exc
        time.sleep(1.5 * (2**attempt))
    raise AssertionError("unreachable")


def remote_versions(package_id: str) -> Set[str]:
    lower = package_id.lower()
    body = http_get(f"{NUGET_FLAT}/{lower}/index.json")
    if body is None:
        return set()
    try:
        versions = json.loads(body)["versions"]
        return {str(v).lower() for v in versions}
    except (ValueError, KeyError, TypeError) as exc:
        raise PublishError(f"Invalid NuGet version index for {package_id}") from exc


def stable_latest(versions: Set[str]) -> Optional[str]:
    stable = [v for v in versions if VERSION_RE.fullmatch(v)]
    return max(stable, key=version_tuple) if stable else None


def release_notes_from_nuspec(data: bytes) -> str:
    xml = ET.fromstring(data)
    for element in xml.iter():
        if tag_name(element).lower() == "releasenotes":
            return element.text or ""
    return ""


def get_published_fingerprint(package_id: str, version: str) -> Optional[str]:
    lower = package_id.lower()
    body = http_get(f"{NUGET_FLAT}/{lower}/{version.lower()}/{lower}.nuspec")
    if body is None:
        raise PublishError(f"NuGet indexed {package_id} {version} but its .nuspec was unavailable")
    match = FINGERPRINT_RE.search(release_notes_from_nuspec(body))
    return match.group(1).lower() if match else None


def remote_status(project: Project) -> Remote:
    versions = remote_versions(project.package_id)
    latest = stable_latest(versions)
    digest = get_published_fingerprint(project.package_id, latest) if latest else None
    return Remote(latest, digest, versions)


def decision(project: Project, remote: Remote, bump: str, force: bool = False) -> Decision:
    current = project.version
    if remote.latest_version is None:
        if current.lower() in remote.all_versions:
            raise PublishError(f"Version {current} already exists as a prerelease for {project.package_id}")
        return Decision(project, "publish", current, "not yet on NuGet.org", remote)
    latest = remote.latest_version
    if force:
        if version_tuple(current) > version_tuple(latest) and current.lower() not in remote.all_versions:
            target = current
        else:
            target = increment_version(max(current, latest, key=version_tuple), bump)
        if target.lower() in remote.all_versions:
            raise PublishError(f"Refusing to overwrite {project.package_id} {target}")
        return Decision(project, "publish", target, "forced package release", remote)
    if remote.latest_fingerprint == project.fingerprint:
        if version_tuple(current) < version_tuple(latest):
            return Decision(project, "sync", latest, "published fingerprint matches; synchronize local version", remote)
        if version_tuple(current) > version_tuple(latest):
            return Decision(project, "skip", current, f"unchanged; local version {current} is ahead of published {latest}", remote)
        return Decision(project, "skip", current, "published fingerprint matches", remote)
    # If the local csproj has already been bumped by a failed/interrupted release,
    # reuse that unpublished version. Otherwise bump from the latest published one.
    if version_tuple(current) > version_tuple(latest) and current.lower() not in remote.all_versions:
        target = current
        reason = "pending unpublished local version; reuse after interrupted release"
    else:
        target = increment_version(max(current, latest, key=version_tuple), bump)
        reason = "published fingerprint differs" if remote.latest_fingerprint else "published version has no fingerprint (migration)"
    if target.lower() in remote.all_versions:
        raise PublishError(f"Refusing to overwrite {project.package_id} {target}, which already exists")
    return Decision(project, "publish", target, reason, remote)


def update_project_version(path: Path, expected: str, target: str) -> None:
    if expected == target:
        return
    original = path.read_bytes()
    content = original.decode("utf-8-sig")
    matches = list(VERSION_ELEMENT_RE.finditer(content))
    if len(matches) != 1 or matches[0].group(2).strip() != expected:
        raise PublishError(f"Version changed unexpectedly in {path}; refusing to edit")
    amended = VERSION_ELEMENT_RE.sub(lambda m: m.group(1) + target + m.group(3), content)
    encoded = (b"\xef\xbb\xbf" if original.startswith(b"\xef\xbb\xbf") else b"") + amended.encode("utf-8")
    fd, tmp_path = tempfile.mkstemp(prefix=".nuget-version-", dir=path.parent)
    try:
        with os.fdopen(fd, "wb") as output:
            output.write(encoded)
        shutil.copymode(path, tmp_path)
        os.replace(tmp_path, path)
    finally:
        if os.path.exists(tmp_path):
            os.unlink(tmp_path)


def command(root: Path, args: Sequence[str]) -> None:
    # Never echo NuGet credentials, including in exception messages.
    display = list(args)
    for i, arg in enumerate(display):
        if arg == "--api-key" and i + 1 < len(display):
            display[i + 1] = "[REDACTED]"
    safe_command = " ".join(display)
    print("+ " + safe_command, flush=True)
    try:
        subprocess.run(list(args), cwd=root, check=True)
    except subprocess.CalledProcessError as exc:
        raise PublishError(f"Command exited with status {exc.returncode}: {safe_command}") from None


def git_check_clean(root: Path) -> None:
    try:
        result = subprocess.run(["git", "status", "--porcelain", "--untracked-files=all"],
                                cwd=root, capture_output=True, text=True, check=True)
    except (FileNotFoundError, subprocess.CalledProcessError) as exc:
        raise PublishError("Cannot check git working tree; use --allow-dirty only if deliberate") from exc
    if result.stdout.strip():
        raise PublishError("Tracked working tree has uncommitted changes. Commit and rerun, or use --allow-dirty.")


def validate_nupkg(path: Path, expected_id: str, expected_version: str, expected_hash: str,
                    dependency_versions: Dict[str, str], refs: Sequence[Path], by_path: Dict[Path, Project]) -> None:
    with zipfile.ZipFile(path) as archive:
        files = [x for x in archive.namelist() if x.endswith(".nuspec")]
        if len(files) != 1:
            raise PublishError(f"Expected one .nuspec in {path}")
        root = ET.fromstring(archive.read(files[0]))
        fields: Dict[str, str] = {}
        deps: Dict[str, List[str]] = {}
        for element in root.iter():
            name = tag_name(element).lower()
            if name in {"id", "version", "releasenotes"} and element.text:
                fields[name] = element.text.strip()
            if name == "dependency":
                pid = element.get("id", "").lower()
                deps.setdefault(pid, []).append(element.get("version", ""))
        if (fields.get("id", "").lower() != expected_id.lower()
                or fields.get("version") != expected_version):
            raise PublishError(f"Unexpected NuGet package ID/version in {path}: {fields}")
        if FINGERPRINT_PREFIX + expected_hash not in fields.get("releasenotes", ""):
            raise PublishError(f"Source fingerprint missing or invalid in {path}")
        for ref in refs:
            dep_id = by_path[ref].package_id.lower()
            target = dependency_versions[dep_id]
            specs = deps.get(dep_id, [])
            if not specs:
                raise PublishError(f"{expected_id}: missing package dependency on {dep_id}")
            # SDK-style ProjectReference dependencies have a minimum-bound version,
            # commonly [3.0.1, ), or [3.0.1, ); tolerate a bare 3.0.1.
            minima = [re.search(r"\d+\.\d+\.\d+", spec) for spec in specs]
            if not any(m and m.group(0) == target for m in minima):
                raise PublishError(f"{expected_id}: dependency {dep_id} uses {specs}, expected minimum {target}")


def pack_project(root: Path, project: Project, target_version: str, out_root: Path,
                 dependent_versions: Dict[str, str], by_path: Dict[Path, Project]) -> Path:
    package_dir = out_root / project.package_id
    if package_dir.exists():
        shutil.rmtree(package_dir)
    package_dir.mkdir(parents=True)
    command(root, ["dotnet", "pack", str(project.path), "--configuration", "Release",
                   "--no-build", "--no-restore", "--output", str(package_dir),
                   "-p:PackageReleaseNotes=" + FINGERPRINT_PREFIX + project.fingerprint])
    files = list(package_dir.glob("*.nupkg"))
    if len(files) != 1:
        raise PublishError(f"Expected precisely one .nupkg in {package_dir}, found {len(files)}")
    validate_nupkg(files[0], project.package_id, target_version, project.fingerprint,
                    dependent_versions, project.dependencies, by_path)
    return files[0]


def execute(root: Path, decisions: Sequence[Decision], skip_tests: bool, api_key: str) -> None:
    by_path = {d.project.path: d.project for d in decisions}
    target_versions = {d.project.package_id.lower(): d.target_version for d in decisions}
    for item in decisions:
        update_project_version(item.project.path, item.project.version, item.target_version)
    print("\nProject versions updated; they remain in .csproj after a failed publish.", flush=True)

    changed = [d for d in decisions if d.action == "publish"]
    if not changed:
        print("Nothing to publish.")
        return

    command(root, ["dotnet", "restore", str(root / "DProjects.Libs.sln")])
    command(root, ["dotnet", "build", str(root / "DProjects.Libs.sln"),
                   "--configuration", "Release", "--no-restore"])
    if not skip_tests:
        command(root, ["dotnet", "test", "--solution", str(root / "DProjects.Libs.sln"),
                       "--configuration", "Release", "--no-build", "--no-restore",
                       "--", "--filter-not-trait", "Category=Integration", "--ignore-exit-code", "8"])
    else:
        print("WARNING: --skip-tests selected; verify CI for this exact commit.", flush=True)

    out_root = root / "artifacts" / "nuget-selective"
    packages: Dict[str, Path] = {}
    for item in changed:
        path = pack_project(root, item.project, item.target_version, out_root, target_versions, by_path)
        packages[item.project.package_id] = path
    print(f"\nValidated {len(packages)} packages. Beginning NuGet.org upload.", flush=True)

    for item in changed:
        project = item.project
        version = item.target_version
        # Fail closed: verify the exact version was not independently published
        # between planning and push, avoiding accidental --skip-duplicate hiding it.
        versions = remote_versions(project.package_id)
        if version.lower() in versions:
            digest = get_published_fingerprint(project.package_id, version)
            if digest == project.fingerprint:
                print(f"Already published: {project.package_id} {version} (same fingerprint)")
                continue
            raise PublishError(f"Version collision: {project.package_id} {version} already exists with different metadata")
        command(root, ["dotnet", "nuget", "push", str(packages[project.package_id]),
                       "--source", NUGET_PUSH, "--api-key", api_key])
        print(f"Published: {project.package_id} {version}", flush=True)

    print("\nPublication commands completed. NuGet indexing can take a few minutes.")
    print("Commit the updated .csproj versions to the repository.")


def run_cli() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parent.parent,
                        help="Repository root (default: parent of scripts/)")
    parser.add_argument("--publish", action="store_true", help="Update .csproj, build/test/pack, and push to NuGet.org")
    parser.add_argument("--bump", choices=("patch", "minor", "major"), default="patch",
                        help="Version increment for changed packages (default: patch)")
    parser.add_argument("--force-package", action="append", default=[], metavar="PACKAGE_ID",
                        help="Publish this package even if unchanged (repeatable)")
    parser.add_argument("--skip-tests", action="store_true", help="Skip tests (requires verified green CI)")
    parser.add_argument("--allow-dirty", action="store_true", help="Allow publishing with uncommitted tracked changes")
    args = parser.parse_args()
    root = args.root.expanduser().resolve()
    if not (root / "DProjects.Libs.sln").is_file():
        raise PublishError(f"Not the repository root (missing DProjects.Libs.sln): {root}")
    if args.publish and not args.allow_dirty:
        git_check_clean(root)
    if args.publish and not os.environ.get("NUGET_API_KEY"):
        raise PublishError("NUGET_API_KEY must be set for --publish")

    projects = topological_order(discover_projects(root))
    forced = {x.lower() for x in args.force_package}
    unknown = forced - {p.package_id.lower() for p in projects}
    if unknown:
        raise PublishError(f"Unknown --force-package IDs: {', '.join(sorted(unknown))}")
    print(f"Discovered {len(projects)} packable projects in {root}", flush=True)
    decisions: List[Decision] = []
    for project in projects:
        project.fingerprint = fingerprint(root, project)
        remote = remote_status(project)
        item = decision(project, remote, args.bump, project.package_id.lower() in forced)
        decisions.append(item)
        previous = remote.latest_version or "(none)"
        print(f"{item.action.upper():7s} {project.package_id:40s} "
              f"{previous:>10s} -> {item.target_version:10s}  {item.reason}", flush=True)

    changed = sum(1 for d in decisions if d.action == "publish")
    synced = sum(1 for d in decisions if d.action == "sync")
    print(f"\nPlan: publish {changed}; sync versions {synced}; skip {len(decisions)-changed-synced}.")
    if not args.publish:
        print("Dry run: no .csproj files were modified and nothing was published.")
        return 0
    if not changed and not synced:
        print("Nothing to do.")
        return 0
    execute(root, decisions, args.skip_tests, os.environ["NUGET_API_KEY"])
    return 0


if __name__ == "__main__":
    try:
        sys.exit(run_cli())
    except (PublishError, OSError, ValueError, ET.ParseError, zipfile.BadZipFile,
            subprocess.CalledProcessError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        sys.exit(1)
