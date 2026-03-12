#!/usr/bin/env python3
import argparse
import os
import re
from pathlib import Path
from typing import List, Dict, Any

CS_EXT = '.cs'

ATTRIBUTE_RE = re.compile(r'^\s*\[(.+?)\]\s*$')
NAMESPACE_RE = re.compile(r'^\s*namespace\s+([\w\.]+)')
TYPE_RE = re.compile(
    r'^(?P<indent>\s*)'
    r'(?P<mods>(?:(?:public|internal|protected|private|static|abstract|sealed|partial|readonly|ref|unsafe)\s+)*)'
    r'(?P<kind>class|struct|interface|enum|record)\s+'
    r'(?P<name>[A-Za-z_]\w*)'
    r'(?P<tail>\s*(?:<[^\{>]*>)?\s*(?::\s*[^\{]+)?)?\s*\{?\s*$'
)
METHOD_RE = re.compile(
    r'^(?P<indent>\s*)'
    r'(?P<mods>(?:(?:public|internal|protected|private|static|virtual|override|abstract|sealed|partial|async|extern|new|unsafe)\s+)*)'
    r'(?:(?P<ret>[^=;\(\)]+?)\s+)?'
    r'(?P<name>[A-Za-z_]\w*)\s*'
    r'(?P<gen><[^>]+>)?\s*'
    r'\((?P<params>[^\)]*)\)\s*'
    r'(?P<constraints>(?:where\s+[^\{]+)?)\s*'
    r'(?P<end>\{|=>)'
)
PROPERTY_RE = re.compile(
    r'^(?P<indent>\s*)'
    r'(?P<mods>(?:(?:public|internal|protected|private|static|virtual|override|abstract|sealed|partial|new|unsafe)\s+)*)'
    r'(?P<type>[^=;\(\)]+?)\s+'
    r'(?P<name>[A-Za-z_]\w*)\s*\{\s*(?:get|set|init|private|protected|internal)'
)
FIELD_RE = re.compile(
    r'^(?P<indent>\s*)'
    r'(?P<mods>(?:(?:public|internal|protected|private|static|readonly|const|volatile|new|unsafe)\s+)*)'
    r'(?P<type>[^=;\(\)]+?)\s+'
    r'(?P<name>[A-Za-z_]\w*)\s*(?:=|;)'
)

CONTROL_KEYWORDS = {
    'if', 'for', 'foreach', 'while', 'switch', 'catch', 'using', 'lock', 'return',
    'throw', 'nameof', 'sizeof', 'default'
}
VISIBILITY_ORDER = {'public': 0, 'protected': 1, 'internal': 2, 'private': 3}


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
        nxt = text[i + 1] if i + 1 < n else ''
        if in_sl_comment:
            if ch == '\n':
                in_sl_comment = False
                result.append(ch)
            i += 1
            continue
        if in_ml_comment:
            if ch == '*' and nxt == '/':
                in_ml_comment = False
                i += 2
            else:
                if ch == '\n':
                    result.append('\n')
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
                if ch == '\\':
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
            if ch == '\\':
                if i + 1 < n:
                    result.append(text[i + 1])
                    i += 2
                    continue
            elif ch == "'":
                in_char = False
            i += 1
            continue
        if ch == '/' and nxt == '/':
            in_sl_comment = True
            i += 2
            continue
        if ch == '/' and nxt == '*':
            in_ml_comment = True
            i += 2
            continue
        if ch == '@' and nxt == '"':
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
    return ''.join(result)


def normalize_ws(s: str) -> str:
    return re.sub(r'\s+', ' ', s).strip()


def visibility_from_mods(mods: str) -> str:
    parts = mods.split()
    for token in ('public', 'protected', 'internal', 'private'):
        if token in parts:
            return token
    return 'private'


def is_publicish(mods: str) -> bool:
    return bool(set(mods.split()) & {'public', 'protected', 'internal'})


def count_braces_outside_quotes(line: str):
    opens = closes = 0
    in_string = False
    in_char = False
    escape = False
    for ch in line:
        if in_string:
            if escape:
                escape = False
            elif ch == '\\':
                escape = True
            elif ch == '"':
                in_string = False
            continue
        if in_char:
            if escape:
                escape = False
            elif ch == '\\':
                escape = True
            elif ch == "'":
                in_char = False
            continue
        if ch == '"':
            in_string = True
        elif ch == "'":
            in_char = True
        elif ch == '{':
            opens += 1
        elif ch == '}':
            closes += 1
    return opens, closes


def append_member(type_stack, member_kind: str, signature: str, visibility: str):
    if not type_stack:
        return
    type_stack[-1]['members'].append({
        'kind': member_kind,
        'signature': normalize_ws(signature),
        'visibility': visibility,
    })


def parse_file(path: Path) -> Dict[str, Any]:
    try:
        raw = path.read_text(encoding='utf-8')
    except UnicodeDecodeError:
        raw = path.read_text(encoding='latin-1')
    text = strip_comments_keep_strings(raw)
    lines = text.splitlines()

    info: Dict[str, Any] = {
        'file': path.name,
        'path': str(path),
        'namespace': None,
        'usings': [],
        'types': [],
    }

    pending_attrs: List[str] = []
    type_stack: List[Dict[str, Any]] = []
    brace_depth = 0

    for line in lines:
        stripped = line.strip()

        if not info['namespace']:
            m = NAMESPACE_RE.match(line)
            if m:
                info['namespace'] = m.group(1)

        if stripped.startswith('using ') and stripped.endswith(';'):
            info['usings'].append(stripped)

        am = ATTRIBUTE_RE.match(line)
        if am:
            pending_attrs.append(normalize_ws(am.group(1)))
        elif stripped:
            tm = TYPE_RE.match(line)
            if tm:
                mods = normalize_ws(tm.group('mods') or '')
                kind = tm.group('kind')
                name = tm.group('name')
                tail = normalize_ws(tm.group('tail') or '')
                bases = None
                if ':' in tail:
                    bases = normalize_ws(tail.split(':', 1)[1].rstrip('{').strip())
                opens, _ = count_braces_outside_quotes(line)
                type_info = {
                    'name': name,
                    'kind': kind,
                    'mods': mods,
                    'visibility': visibility_from_mods(mods),
                    'bases': bases,
                    'attributes': pending_attrs[:],
                    'members': [],
                    'children': [],
                    'body_depth': brace_depth + 1 if opens > 0 else None,
                }
                pending_attrs.clear()
                if type_stack:
                    type_stack[-1]['children'].append(type_info)
                else:
                    info['types'].append(type_info)
                type_stack.append(type_info)
            else:
                mm = METHOD_RE.match(line)
                if mm and type_stack:
                    mods = normalize_ws(mm.group('mods') or '')
                    name = mm.group('name')
                    if name not in CONTROL_KEYWORDS and is_publicish(mods):
                        ret = normalize_ws(mm.group('ret') or '')
                        gen = mm.group('gen') or ''
                        params = normalize_ws(mm.group('params') or '')
                        sig = f"{mods + ' ' if mods else ''}{(ret + ' ') if ret else ''}{name}{gen}({params})"
                        append_member(type_stack, 'method', sig, visibility_from_mods(mods))
                        pending_attrs.clear()
                else:
                    pm = PROPERTY_RE.match(line)
                    if pm and type_stack:
                        mods = normalize_ws(pm.group('mods') or '')
                        if is_publicish(mods):
                            ptype = normalize_ws(pm.group('type'))
                            name = pm.group('name')
                            sig = f"{mods + ' ' if mods else ''}{ptype} {name}"
                            append_member(type_stack, 'property', sig, visibility_from_mods(mods))
                            pending_attrs.clear()
                    else:
                        fm = FIELD_RE.match(line)
                        if fm and type_stack:
                            mods = normalize_ws(fm.group('mods') or '')
                            if is_publicish(mods):
                                ftype = normalize_ws(fm.group('type'))
                                name = fm.group('name')
                                sig = f"{mods + ' ' if mods else ''}{ftype} {name}"
                                append_member(type_stack, 'field', sig, visibility_from_mods(mods))
                                pending_attrs.clear()
                        elif stripped:
                            pending_attrs.clear()

        opens, closes = count_braces_outside_quotes(line)

        if opens > 0:
            for t in reversed(type_stack):
                if t['body_depth'] is None:
                    t['body_depth'] = brace_depth + 1
                    break

        brace_depth += opens - closes

        while type_stack and type_stack[-1]['body_depth'] is not None and brace_depth < type_stack[-1]['body_depth']:
            type_stack.pop()

    return info


def sort_members(members: List[Dict[str, str]]) -> List[Dict[str, str]]:
    def key(m):
        return (VISIBILITY_ORDER.get(m['visibility'], 9), m['kind'], m['signature'])
    return sorted(members, key=key)


def render_type(t: Dict[str, Any], depth: int, out: List[str]):
    prefix = '  ' * depth
    mods = t['mods']
    decl = f"{mods + ' ' if mods else ''}{t['kind']} {t['name']}"
    if t.get('bases'):
        decl += f" : {t['bases']}"
    out.append(f"{prefix}TYPE: {decl}")
    if t['attributes']:
        out.append(f"{prefix}ATTRIBUTES:")
        for attr in t['attributes']:
            out.append(f"{prefix}- [{attr}]")
    members = sort_members(t['members'])
    if members:
        out.append(f"{prefix}MEMBERS:")
        for m in members:
            out.append(f"{prefix}- {m['kind']}: {m['signature']}")
    else:
        out.append(f"{prefix}MEMBERS: none detected")
    if t['children']:
        out.append(f"{prefix}NESTED TYPES:")
        for child in t['children']:
            render_type(child, depth + 1, out)


def build_output(files: List[Dict[str, Any]], root: Path) -> str:
    out: List[str] = []
    out.append('CODEMAP')
    out.append(f'ROOT: {root.resolve()}')
    out.append(f'TOTAL_FILES: {len(files)}')
    out.append('')
    out.append('FORMAT')
    out.append('- FILE = source file')
    out.append('- TYPE = class/struct/interface/enum/record')
    out.append('- MEMBERS = public/protected/internal methods, properties, fields detected by lightweight parser')
    out.append('- ATTRIBUTES = C# attributes attached directly above type declarations')
    out.append('')

    for info in files:
        rel = os.path.relpath(info['path'], root)
        out.append('=' * 80)
        out.append(f"FILE: {rel}")
        out.append(f"NAMESPACE: {info['namespace'] or '(none found)'}")
        if info['usings']:
            out.append(f"USINGS: {', '.join(info['usings'])}")
        if not info['types']:
            out.append('TYPES: none detected')
        else:
            out.append('TYPES:')
            for t in info['types']:
                render_type(t, 1, out)
        out.append('')
    return '\n'.join(out).rstrip() + '\n'


def collect_files(root: Path) -> List[Path]:
    files = [p for p in root.rglob(f'*{CS_EXT}') if p.is_file()]
    return sorted(files, key=lambda p: str(p).lower())


def main():
    parser = argparse.ArgumentParser(description='Generate a single-file CODEMAP.txt from C# source files.')
    parser.add_argument('root', nargs='?', default='.', help='Root folder to scan. Default: current directory')
    parser.add_argument('-o', '--output', default='CODEMAP.txt', help='Output file path. Default: CODEMAP.txt')
    args = parser.parse_args()

    root = Path(args.root).resolve()
    if not root.exists():
        raise SystemExit(f'Root path does not exist: {root}')

    files = collect_files(root)
    parsed = [parse_file(p) for p in files]
    content = build_output(parsed, root)

    out_path = Path(args.output)
    if not out_path.is_absolute():
        out_path = root / out_path
    out_path.write_text(content, encoding='utf-8')

    print(f'Generated {out_path}')
    print(f'Files scanned: {len(files)}')


if __name__ == '__main__':
    main()
