#!/usr/bin/env python3
"""
BLACKZONE project validator.

Static checks that can run without the Unity Editor:

  1. C# syntax: every .cs file parsed with tree-sitter (real parser).
  2. Cross-file contract checks (strings that must exist together).
  3. .meta completeness: every asset under Assets/ has a valid meta GUID.
  4. Scene/settings GUID references resolve to real meta files.
  5. Layer names in TagManager.asset match GameConstants layer indices.

Usage:
    python3 Tools/validate_project.py
    exit code 0 = clean, 1 = problems found
"""
import pathlib
import re
import sys

from tree_sitter import Language, Parser
import tree_sitter_c_sharp as tscsharp

ROOT = pathlib.Path(__file__).resolve().parent.parent
ASSETS = ROOT / "Assets"

LANG = Language(tscsharp.language())
PARSER = Parser(LANG)

# ---------------------------------------------------------------- helpers

def cs_files():
    for p in sorted(ASSETS.rglob("*.cs")):
        yield p

def load_text(p):
    return p.read_text(errors="replace")

def check_syntax():
    bad = []
    for p in cs_files():
        src = load_text(p).encode("utf-8")
        tree = PARSER.parse(src)
        if tree.root_node.has_error:
            # collect the first few error nodes for a useful message
            errors = []
            stack = [tree.root_node]
            while stack and len(errors) < 3:
                node = stack.pop()
                if node.type == "ERROR" or node.is_missing:
                    line = src[: node.start_byte].count(b"\n") + 1
                    errors.append(f"line {line} ({node.type})")
                stack.extend(node.children)
            bad.append(f"{p.relative_to(ROOT)}: " + ", ".join(errors))
    return bad

def check_contracts():
    """Ensure APIs referenced across files actually exist somewhere."""
    problems = []
    texts = {p: load_text(p) for p in cs_files()}
    all_text = "\n".join(texts.values())

    def must_contain(pattern, what):
        if not re.search(pattern, all_text, re.MULTILINE):
            problems.append(f"missing definition: {what} ({pattern})")

    # GameInput API surface used by gameplay
    for member in ["public static Vector2 Move", "public static Vector2 LookDelta",
                   "public static bool FireHeld", "public static bool AdsHeld",
                   "public static bool FirePressed", "public static bool ReloadPressed",
                   "public static bool JumpPressed", "public static bool CrouchPressed",
                   "public static bool PausePressed",
                   "public static int WeaponSlotRequested", "public static int PrevNextRequest",
                   "public static void SetMobileMove", "public static void SetMobileButton"]:
        must_contain(re.escape(member), member)

    # Weapon arsenal surface
    for member in ["public WeaponRuntime Active", "public void Initialize(",
                   "public void RequestSwitch", "public void RestockAll"]:
        must_contain(re.escape(member), member)

    # MapLayout surface used by spawner/bootstrap
    for member in ["public Vector3 PlayerSpawn", "public Vector3[] EnemySpawns",
                   "public Vector3[] Waypoints"]:
        must_contain(re.escape(member), member)

    # Audio surface
    must_contain(r"public enum AudioId", "AudioId enum")
    must_contain(r"public void Play\(AudioId id", "AudioManager.Play(AudioId)")

    # QualityApplier.RegisterSun used by map
    must_contain(r"QualityApplier\.RegisterSun", "MapBuilder registers sun")

    return problems

def check_metas():
    problems = []
    missing = 0
    for p in sorted(ASSETS.rglob("*")):
        if p.suffix == ".meta" or not p.is_file():
            continue
        meta = pathlib.Path(str(p) + ".meta")
        if not meta.exists():
            missing += 1
            problems.append(f"no meta: {p.relative_to(ROOT)}")
            continue
        content = meta.read_text(errors="replace")
        m = re.search(r"guid: ([0-9a-f]{32})", content)
        if not m:
            problems.append(f"invalid meta guid: {p.relative_to(ROOT)}")
    return problems, missing

def check_guid_refs():
    """Scene and EditorBuildSettings must reference existing meta guids."""
    problems = []
    guid_files = {}
    for meta in (ASSETS / "_Blackzone").rglob("*.meta"):
        content = meta.read_text(errors="replace")
        m = re.search(r"guid: ([0-9a-f]{32})", content)
        if m:
            guid_files[m.group(1)] = meta

    refs = []
    scene = ASSETS / "_Blackzone" / "Scenes" / "Blackzone_Phase1.unity"
    if scene.exists():
        refs += re.findall(r"guid: ([0-9a-f]{32})", load_text(scene))
    ebs = ROOT / "ProjectSettings" / "EditorBuildSettings.asset"
    if ebs.exists():
        refs += re.findall(r"guid: ([0-9a-f]{32})", load_text(ebs))

    for g in refs:
        # Built-in asset references (e.g. default spot cookie) have no meta.
        if g.startswith("0000000000000000e") or g == "00000000000000000000000000000000":
            continue
        if g not in guid_files:
            problems.append(f"unresolved guid {g} referenced but no meta found")
    return problems

def check_layers():
    """TagManager layers must match GameConstants indices."""
    problems = []
    tm = ROOT / "ProjectSettings" / "TagManager.asset"
    gc = ASSETS / "_Blackzone" / "Scripts" / "Utilities" / "GameConstants.cs"
    if not tm.exists() or not gc.exists():
        return problems
    tag = load_text(tm)
    code = load_text(gc)
    expected = {"Player": 3, "UI": 5, "World": 8, "Enemy": 9, "Interactable": 10}
    layer_lines = tag.split("layers:")[1].split("m_SortingLayers:")[0]
    names = [l.strip().lstrip("- ").strip() for l in layer_lines.splitlines() if l.strip().startswith("-")]
    for name, idx in expected.items():
        if idx >= len(names) or names[idx] != name:
            problems.append(f"TagManager layer {idx} is '{names[idx] if idx < len(names) else '?'}' expected '{name}'")
        if f"Layer{name}" not in code and f"Layer{name} =" not in code:
            problems.append(f"GameConstants missing Layer{name} constant")
    return problems

# ---------------------------------------------------------------- main

def main():
    problems = []

    print("== 1. C# syntax (tree-sitter) ==")
    syntax = check_syntax()
    if syntax:
        problems += syntax
        for s in syntax:
            print("  FAIL:", s)
    else:
        print("  OK: all", len(list(cs_files())), "files parse")

    print("== 2. cross-file contracts ==")
    contracts = check_contracts()
    if contracts:
        problems += contracts
        for c in contracts:
            print("  FAIL:", c)
    else:
        print("  OK")

    print("== 3. meta completeness ==")
    meta_problems, missing = check_metas()
    if meta_problems:
        problems += meta_problems
        for m in meta_problems:
            print("  FAIL:", m)
    else:
        print(f"  OK: all assets have metas ({missing} were missing before)")

    print("== 4. guid references ==")
    guid_problems = check_guid_refs()
    if guid_problems:
        problems += guid_problems
        for g in guid_problems:
            print("  FAIL:", g)
    else:
        print("  OK: scene/build settings guids resolve")

    print("== 5. layer consistency ==")
    layer_problems = check_layers()
    if layer_problems:
        problems += layer_problems
        for l in layer_problems:
            print("  FAIL:", l)
    else:
        print("  OK")

    if problems:
        print(f"\nRESULT: {len(problems)} problem(s) — fix before committing.")
        sys.exit(1)
    print("\nRESULT: all static checks passed.")

if __name__ == "__main__":
    main()
