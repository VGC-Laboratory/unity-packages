#!/usr/bin/env python3
"""listing に載せる version entry を作る。

package.json に url と zipSHA256 を足したものを書き出す。
使い方: make-version-entry.py <package.json> <zip の URL> <zip の SHA256> <出力先>
"""
import io
import json
import sys


def main(argv):
    if len(argv) != 5:
        sys.stderr.write(__doc__)
        return 2
    src, url, sha, dst = argv[1:]
    with io.open(src, encoding="utf-8") as f:
        entry = json.load(f)
    entry["url"] = url
    entry["zipSHA256"] = sha
    with io.open(dst, "w", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(entry, ensure_ascii=False, indent=2) + "\n")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
