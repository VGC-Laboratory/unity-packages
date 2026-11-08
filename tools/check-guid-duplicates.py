#!/usr/bin/env python3
"""Packages/ 配下の .meta の GUID が重複していないか調べる。

パッケージをまたいで同じ GUID があると、利用者のプロジェクトで
どちらか一方しか解決されず、参照が静かに壊れる。

使い方: check-guid-duplicates.py [探索するフォルダ（既定: Packages）]
終了コード: 重複が無ければ 0、あれば 1
"""
import io
import os
import re
import sys
from collections import defaultdict

GUID_RE = re.compile(r"^guid: ([0-9a-f]{32})\s*$", re.MULTILINE)


def collect(root):
    """.meta の GUID -> それを持つファイルのリスト"""
    found = defaultdict(list)
    no_guid = []
    for dirpath, _dirnames, filenames in os.walk(root):
        for name in filenames:
            if not name.endswith(".meta"):
                continue
            path = os.path.join(dirpath, name)
            with io.open(path, encoding="utf-8", errors="replace") as f:
                text = f.read()
            match = GUID_RE.search(text)
            try:
                rel = os.path.relpath(path, ".")
            except ValueError:
                # Windows で別ドライブを渡されたとき
                rel = path
            rel = rel.replace("\\", "/")
            if match is None:
                no_guid.append(rel)
            else:
                found[match.group(1)].append(rel)
    return found, no_guid


def main(argv):
    root = argv[1] if len(argv) > 1 else "Packages"
    if not os.path.isdir(root):
        sys.stderr.write("フォルダがありません: %s\n" % root)
        return 2

    found, no_guid = collect(root)
    out = io.open(sys.stdout.fileno(), "w", encoding="utf-8", closefd=False)
    out.write("調べた .meta: %d 件（%s 配下）\n" % (sum(len(v) for v in found.values()) + len(no_guid), root))

    if no_guid:
        out.write("\nguid 行が読めなかったファイル:\n")
        for path in sorted(no_guid):
            out.write("  %s\n" % path)

    duplicates = {g: paths for g, paths in found.items() if len(paths) > 1}
    if not duplicates:
        out.write("GUID の重複なし\n")
        return 0

    out.write("\nGUID が重複しています:\n")
    for guid in sorted(duplicates):
        out.write("  %s\n" % guid)
        for path in sorted(duplicates[guid]):
            out.write("      %s\n" % path)
    # GitHub Actions のアノテーションとして出す
    for guid in sorted(duplicates):
        out.write("::error::GUID %s が %s で重複しています\n"
                  % (guid, ", ".join(sorted(duplicates[guid]))))
    out.flush()
    return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
