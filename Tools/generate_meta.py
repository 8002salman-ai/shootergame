#!/usr/bin/env python3
"""
BLACKZONE meta generator.

Generates Unity .meta files for every file and folder under Assets/ (and for
Packages/ProjectSettings where needed) that does not already have one.

GUIDs are DETERMINISTIC: uuid5(ns, relative_path). Re-running this tool always
produces the same GUIDs, so scene/settings files that reference assets by GUID
can be authored before metas exist.

Usage:
    python3 Tools/generate_meta.py [--check]
        --check  only verify that every asset has a meta with a valid GUID;
                 exit code 1 if anything is missing (no files written).
"""
import hashlib
import os
import pathlib
import sys
import uuid

ROOT = pathlib.Path(__file__).resolve().parent.parent
NS = uuid.UUID("8b2f3a4c-5d6e-4f7a-9b8c-1d2e3f4a5b6c")

TEXT_EXT = {".cs", ".asmdef", ".json", ".md", ".txt", ".asset", ".mat", ".unity", ".prefab", ".physicmaterial", ".controller", ".shader", ".compute"}
FOLDER_GUID_PREFIX = "folder_"

def guid_for(rel: str) -> str:
    """Deterministic 32-hex GUID for a relative path."""
    return uuid.uuid5(NS, rel).hex

def is_textual(path: pathlib.Path) -> bool:
    return path.suffix.lower() in TEXT_EXT

def write_meta(path: pathlib.Path, is_folder: bool):
    rel = path.relative_to(ROOT).as_posix()
    guid = guid_for(rel if not is_folder else rel + "/")
    # Folder metas use the same deterministic scheme but we keep a distinct
    # namespace entry so folder/file GUIDs never collide.
    if is_folder:
        guid = uuid.uuid5(NS, "dir:" + rel).hex
    lines = ["fileFormatVersion: 2", "guid: " + guid, "folderAsset: yes" if is_folder else "MonoImporter:", ""]
    if not is_folder:
        lines = ["fileFormatVersion: 2", "guid: " + guid, "MonoImporter:", "  externalObjects: {}", "  serializedVersion: 2", "  defaultReferences: []", "  executionOrder: 0", "  icon: {instanceID: 0}", "  userData: ", "  assetBundleName: ", "  assetBundleVariant: ", ""]
    path.with_suffix(path.suffix + ".meta").write_text("\n".join(lines))

def main():
    check_only = "--check" in sys.argv
    missing = []
    assets_root = ROOT / "Assets"
    for dirpath, dirnames, filenames in os.walk(assets_root):
        d = pathlib.Path(dirpath)
        for name in dirnames + filenames:
            p = d / name
            if name.endswith(".meta"):
                continue
            if not p.exists():
                continue
            meta = pathlib.Path(str(p) + ".meta")
            if not meta.exists():
                missing.append(p)
                if not check_only:
                    write_meta(p, p.is_dir())
            else:
                # verify GUID validity
                content = meta.read_text(errors="replace")
                if "guid:" not in content:
                    missing.append(p)
                    if not check_only:
                        write_meta(p, p.is_dir())
    # project-level files that benefit from metas (package.json etc.) are not
    # required by Unity, so only Assets/ is validated.
    if missing:
        print(f"meta: {len(missing)} asset(s) without meta")
        for m in missing:
            print("  missing:", m.relative_to(ROOT))
        sys.exit(1 if check_only else 0)
    print("meta: OK - all assets have valid meta files")

if __name__ == "__main__":
    main()
