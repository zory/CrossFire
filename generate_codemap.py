#!/usr/bin/env python3
import os
import re
from pathlib import Path
from typing import List, Dict, Any

# =========================
# CONFIG - edit these only
# =========================

# Folder to scan, relative to the location of this script.
SOURCE_RELATIVE_PATH = "CrossFire/Assets/_Game/Scripts"

# Output file path, relative to the location of this script.
# Examples:
#   "CODEMAP.txt"
#   "Docs/CODEMAP.txt"
OUTPUT_RELATIVE_PATH = "CODEMAP.txt"

CS_EXT = ".cs"

TYPE_RE = re.compile(
    r"^(?P<indent>\s*)"
    r"(?P<mods>(?:(?:public|internal|protected|private|static|abstract|sealed|partial|readonly|ref|unsafe)\s+)*)"
    r"(?P<kind>class|struct|interface|enum|record)\s+"
    r"(?P<name>[A-Za-z_]\w*)"
    r"(?P<tail>\s*(?:<[^>{}]*>)?\s*(?::\s*[^{]+)?)?\s*\{?\s*$"
)

METHOD_RE = re.compile(
    r"^(?P<indent>\s*)"
    r"(?P<mods>(?:(?:public|internal|protected|private|static|virtual|override|abstract|sealed|partial|async|extern|new|unsafe)\s+)*)"
    r"(?:(?P<ret>[^=;\(\)]+?)\s+)?"
    r"(?P<name>[A-Za-z_]\w*)\s*"
    r"(?P<gen><[^>]+>)?\s*"
    r"\((?P<params>[^\)]*)\)\s*"
    r"(?P<constraints>(?:where\s+[^\{]+)?)\s*"
    r"(?P<end>\{|=>)"
)

PROPERTY_RE = re.compile(
    r"^(?P<indent>\s*)"
    r"(?P<mods>(?:(?:public|internal|protected|private|static|virtual|override|abstract|sealed|partial|new|unsafe)\s+)*)"
    r"(?P<type>[^=;\(\)]+?)\s+"
    r"(?P<name>[A-Za-z_]\w*)\s*\{\s*(?:get|set|init|private|protected|internal)"
)

FIELD_RE = re.compile(
    r"^(?P<indent>\s*)"
    r"(?P<mods>(?:(?:public|internal|protected|private|static|readonly|const|volatile|new|unsafe)\s+)*)"
    r"(?P<type>[^=;\(\)]+?)\s+"
    r"(?P<name>[A-Za-z_]\w*)\s*(?:=|;)"
)

NAMESPACE_RE = re.compile(r"^\s*namespace\s+([\w\.]+)")

CONTROL_KEYWORDS = {
    "if", "for", "foreach", "while", "switch", "catch", "using", "lock",
    "return", "throw", "nameof", "sizeof", "default"
}

VISIBILITY_ORDER = {
    "public": 0,
    "protected": 1,
    "internal": 2,
    "private": 3,
}

KIND_ORDER = {
    "field": 0,
    "property": 1,
    "method": 2,
}


def strip_comments_keep_strings(text: str) -> str:
    result = []
    i = 0
    n = len(text)
    in_sl_comment = False
    in_ml_comment = False
    in_str = False
    in_char = False
    verbatim = False

    while i < n:
        ch = text[i]
        nxt = text[i + 1] if i + 1 < n else ""

        if in_sl_comment:
            if ch == "\n":
                in_sl_comment = False
                result.append(ch)
            i += 1
            continue

        if in_ml_comment:
            if ch == "*" and nxt == "/":
                in_ml_comment = False
                i += 2
            else:
                if ch == "\n":
                    result.append("\n")
                i += 1
            continue

        if in_str:
            result.append(ch)
            if verbatim:
                if ch == '"' and nxt == '"':
                    result.append(nxt)
                    i += 2
                    continue
                elif ch == '"':
                    in_str = False
                    verbatim = False
            else:
                if ch == "\\":
                    if i + 1 < n:
                        result.append(text[i + 1])
                        i += 2
                        continue
                elif ch == '"':
                    in_str = False
            i += 1
            continue

        if in_char:
            result.append(ch)
            if ch == "\\":
                if i + 1 < n:
                    result.append(text[i + 1])
                    i += 2
                    continue
            elif ch == "'":
                in_char = False
            i += 1
            continue

        if ch == "/" and nxt == "/":
            in_sl_comment = True
            i += 2
            continue

        if ch == "/" and nxt == "*":
            in_ml_comment = True
            i += 2
            continue

        if ch == "@" and nxt == '"':
            in_str = True
            verbatim = True
            result.append(ch)
            result.append(nxt)
            i += 2
            continue

        if ch == '"':
            in_str = True
            verbatim = False
            result.append(ch)
            i += 1
            continue

        if ch == "'":
            in_char = True
            result.append(ch)
            i += 1
            continue

        result.append(ch)
        i += 1

    return "".join(result)


def normalize_ws(s: str) -> str:
    return re.sub(r"\s+", " ", s).strip()


def visibility_from_mods(mods: str) -> str:
    parts = mods.split()
    for token in ("public", "protected", "internal", "private"):
        if token in parts:
            return token
    return "private"


def is_publicish(mods: str) -> bool:
    return bool(set(mods.split()) & {"public", "protected", "internal"})


def count_braces_outside_quotes(line: str):
    opens = 0
    closes = 0
    in_string = False
    in_char = False
    escape = False

    for ch in line:
        if in_string:
            if escape:
                escape = False
            elif ch == "\\":
                escape = True
            elif ch == '"':
                in_string = False
            continue

        if in_char:
            if escape:
                escape = False
            elif ch == "\\":
                escape = True
            elif ch == "'":
                in_char = False
            continue

        if ch == '"':
            in_string = True
        elif ch == "'":
            in_char = True
        elif ch == "{":
            opens += 1
        elif ch == "}":
            closes += 1

    return opens, closes


def append_member(type_stack, member_kind: str, signature: str, visibility: str):
    if not type_stack:
        return

    type_stack[-1]["members"].append({
        "kind": member_kind,
        "signature": normalize_ws(signature),
        "visibility": visibility,
    })


def parse_file(path: Path) -> Dict[str, Any]:
    try:
        raw = path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        raw = path.read_text(encoding="latin-1")

    text = strip_comments_keep_strings(raw)
    lines = text.splitlines()

    info: Dict[str, Any] = {
        "path": str(path),
        "namespace": None,
        "types": [],
    }

    type_stack: List[Dict[str, Any]] = []
    brace_depth = 0

    for line in lines:
        stripped = line.strip()

        if not info["namespace"]:
            m = NAMESPACE_RE.match(line)
            if m:
                info["namespace"] = m.group(1)

        if stripped:
            tm = TYPE_RE.match(line)
            if tm:
                mods = normalize_ws(tm.group("mods") or "")
                kind = tm.group("kind")
                name = tm.group("name")
                tail = normalize_ws(tm.group("tail") or "")

                bases = None
                if ":" in tail:
                    bases = normalize_ws(tail.split(":", 1)[1].rstrip("{").strip())

                opens, _ = count_braces_outside_quotes(line)

                type_info = {
                    "name": name,
                    "kind": kind,
                    "mods": mods,
                    "visibility": visibility_from_mods(mods),
                    "bases": bases,
                    "namespace": info["namespace"],
                    "members": [],
                    "children": [],
                    "body_depth": brace_depth + 1 if opens > 0 else None,
                }

                if type_stack:
                    type_stack[-1]["children"].append(type_info)
                else:
                    info["types"].append(type_info)

                type_stack.append(type_info)
            else:
                mm = METHOD_RE.match(line)
                if mm and type_stack:
                    mods = normalize_ws(mm.group("mods") or "")
                    name = mm.group("name")

                    if name not in CONTROL_KEYWORDS and is_publicish(mods):
                        ret = normalize_ws(mm.group("ret") or "")
                        gen = mm.group("gen") or ""
                        params = normalize_ws(mm.group("params") or "")
                        sig = f"{mods + ' ' if mods else ''}{(ret + ' ') if ret else ''}{name}{gen}({params})"
                        append_member(type_stack, "method", sig, visibility_from_mods(mods))
                else:
                    pm = PROPERTY_RE.match(line)
                    if pm and type_stack:
                        mods = normalize_ws(pm.group("mods") or "")
                        if is_publicish(mods):
                            ptype = normalize_ws(pm.group("type"))
                            name = pm.group("name")
                            sig = f"{mods + ' ' if mods else ''}{ptype} {name}"
                            append_member(type_stack, "property", sig, visibility_from_mods(mods))
                    else:
                        fm = FIELD_RE.match(line)
                        if fm and type_stack:
                            mods = normalize_ws(fm.group("mods") or "")
                            if is_publicish(mods):
                                ftype = normalize_ws(fm.group("type"))
                                name = fm.group("name")
                                sig = f"{mods + ' ' if mods else ''}{ftype} {name}"
                                append_member(type_stack, "field", sig, visibility_from_mods(mods))

        opens, closes = count_braces_outside_quotes(line)

        if opens > 0:
            for t in reversed(type_stack):
                if t["body_depth"] is None:
                    t["body_depth"] = brace_depth + 1
                    break

        brace_depth += opens - closes

        while type_stack and type_stack[-1]["body_depth"] is not None and brace_depth < type_stack[-1]["body_depth"]:
            type_stack.pop()

    return info


def sort_members(members: List[Dict[str, str]]) -> List[Dict[str, str]]:
    def key(m):
        return (
            VISIBILITY_ORDER.get(m["visibility"], 9),
            KIND_ORDER.get(m["kind"], 9),
            m["signature"].lower(),
        )
    return sorted(members, key=key)


def collect_all_types(type_list: List[Dict[str, Any]], out: List[Dict[str, Any]]):
    for t in type_list:
        out.append(t)
        if t["children"]:
            collect_all_types(t["children"], out)


def render_type(t: Dict[str, Any], out: List[str]):
    ns = t["namespace"] or "(global)"
    mods = t["mods"]
    decl = f"{mods + ' ' if mods else ''}{t['kind']} {t['name']}"
    if t.get("bases"):
        decl += f" : {t['bases']}"

    out.append(f"## {t['name']}")
    out.append(f"- Decl: `{decl}`")
    out.append(f"- Namespace: `{ns}`")

    members = sort_members(t["members"])
    if not members:
        out.append("- Public API: none detected")
        out.append("")
        return

    out.append("- Public API:")
    current_kind = None

    for m in members:
        if m["kind"] != current_kind:
            current_kind = m["kind"]
            out.append(f"  - {current_kind.title()}s:")
        out.append(f"    - `{m['signature']}`")

    out.append("")


def build_output(files: List[Dict[str, Any]], source_root: Path) -> str:
    all_types: List[Dict[str, Any]] = []
    for info in files:
        collect_all_types(info["types"], all_types)

    all_types.sort(key=lambda t: ((t["namespace"] or "").lower(), t["name"].lower()))

    out: List[str] = []
    out.append("# CODEMAP")
    out.append("")
    out.append(f"- Source root: `{source_root}`")
    out.append(f"- Total files scanned: **{len(files)}**")
    out.append(f"- Total types found: **{len(all_types)}**")
    out.append("")
    out.append("---")
    out.append("")

    current_namespace = None
    for t in all_types:
        ns = t["namespace"] or "(global)"
        if ns != current_namespace:
            if current_namespace is not None:
                out.append("---")
                out.append("")
            current_namespace = ns
            out.append(f"# Namespace: `{ns}`")
            out.append("")
        render_type(t, out)

    return "\n".join(out).rstrip() + "\n"


def collect_files(root: Path) -> List[Path]:
    files = [p for p in root.rglob(f"*{CS_EXT}") if p.is_file()]
    return sorted(files, key=lambda p: str(p).lower())


def main():
    script_dir = Path(__file__).resolve().parent
    source_root = (script_dir / SOURCE_RELATIVE_PATH).resolve()
    output_path = (script_dir / OUTPUT_RELATIVE_PATH).resolve()

    if not source_root.exists():
        raise SystemExit(f"Source path does not exist: {source_root}")

    files = collect_files(source_root)
    parsed = [parse_file(p) for p in files]
    content = build_output(parsed, source_root)

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(content, encoding="utf-8")

    print(f"Generated: {output_path}")
    print(f"Source root: {source_root}")
    print(f"Files scanned: {len(files)}")


if __name__ == "__main__":
    main()