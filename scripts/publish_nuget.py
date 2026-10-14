
#!/usr/bin/env python3

import argparse
import os
import subprocess
import sys
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

VERSION = "3.0.0"
SOURCE = "https://api.nuget.org/v3/index.json"

ROOT = Path(__file__).resolve().parent.parent
SOLUTION = ROOT / "DProjects.Libs.sln"
OUTPUT = ROOT / "artifacts" / "packages" / VERSION


def run(command):
    subprocess.run(command, cwd=ROOT, check=True)


def package_info(path):
    with zipfile.ZipFile(path) as archive:
        nuspec = next(
            name for name in archive.namelist()
            if name.endswith(".nuspec")
        )
        root = ET.fromstring(archive.read(nuspec))

        def field(name):
            element = root.find(f".//{{*}}{name}")
            if element is None:
                element = root.find(f".//{name}")
            if element is None or element.text is None:
                raise ValueError(f"Missing {name} in {path.name}")
            return element.text

        return field("id"), field("version")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--publish",
        action="store_true",
        help="Actually upload packages to NuGet.org"
    )
    args = parser.parse_args()

    if not SOLUTION.exists():
        raise FileNotFoundError(SOLUTION)

    OUTPUT.mkdir(parents=True, exist_ok=True)

    # Remove packages left from previous runs.
    for path in OUTPUT.glob("*.nupkg"):
        path.unlink()

    print(f"Packing version {VERSION}", flush=True)

    run([
        "dotnet", "pack", str(SOLUTION),
        "--configuration", "Release",
        "--output", str(OUTPUT),
        f"-p:Version={VERSION}",
    ])

    packages = sorted(
        p for p in OUTPUT.glob("*.nupkg")
        if not p.name.endswith(".symbols.nupkg")
    )

    if not packages:
        raise RuntimeError("No NuGet packages generated")

    print("\nPackages:", flush=True)

    for package in packages:
        package_id, version = package_info(package)

        if version != VERSION:
            raise RuntimeError(
                f"Unexpected version: {package_id} {version}"
            )

        print(f"  {package_id} {version}", flush=True)

    print(f"\nTotal: {len(packages)} packages", flush=True)

    if not args.publish:
        print("\nDry run completed. Use --publish to upload.")
        return

    api_key = os.environ.get("NUGET_API_KEY")

    if not api_key:
        raise RuntimeError("NUGET_API_KEY is not configured")

    for package in packages:
        print(f"\nPublishing {package.name}", flush=True)

        run([
            "dotnet", "nuget", "push",
            str(package),
            "--source", SOURCE,
            "--api-key", api_key,
            "--skip-duplicate",
        ])

    print("\nPublishing completed successfully.", flush=True)


if __name__ == "__main__":
    try:
        main()
    except (subprocess.CalledProcessError, RuntimeError,
            FileNotFoundError, ValueError) as ex:
        print(f"\nERROR: {ex}", file=sys.stderr)
        sys.exit(1)
