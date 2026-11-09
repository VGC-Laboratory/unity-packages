#!/usr/bin/env python3
"""VRChat の SDK が無い素の Unity で、パッケージがコンパイルできるかを調べる。

Unity エディタは起動しない。Unity に同梱の C# コンパイラ（Data/DotNetSdkRoslyn/csc.dll）に、
Unity 本体の参照アセンブリだけを渡してコンパイルする。asmdef ごとに 1 つの DLL を作るのは Unity と同じ。

- 既定の対象は、vpmDependencies に VRChat の SDK も、このリポジトリに無いパッケージも
  （間接的にも）書いていないパッケージ全部。依存を書いていない以上、SDK の無い
  プロジェクトでもコンパイルできなければならない（VGC の VRChat に依存しないツールも使う）。
- asmdef の references・versionDefines・defineConstraints・includePlatforms・allowUnsafeCode を見る。
  versionDefines は、名前のパッケージが対象に入っているときだけ記号を定義する（expression は見ない）。
  defineConstraints を満たさない asmdef は、Unity と同じくコンパイルしない（除外）。
  見つからない参照は外してコンパイルする（その参照先の型を使っていればコンパイルで落ちる）。
- 参照は Unity 本体の DLL（Managed/UnityEngine）と .NET Framework 4.8 の API だけ。
  エディタでのコンパイル（UNITY_EDITOR あり）をまねる。プレイヤー向けのビルドは調べない。

使い方: check-compile-without-vrcsdk.py [--unity <Unity のフォルダ>] [パッケージのフォルダ ...]
  --unity を省くと、Unity Hub の導入先から package.json の "unity" に合う最新の版を探す。
  パッケージを指定したときは、その依存のうちこのリポジトリにあるものも一緒にコンパイルする。
終了コード: 全部通れば 0、通らないものがあれば 1、Unity が見つからないなどで調べられなければ 2
"""
import argparse
import io
import json
import os
import re
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PACKAGES = os.path.join(ROOT, "Packages")

# Unity 2022.3 がエディタでのコンパイルに渡す .NET Framework 4.8 の API（UnityReferenceAssemblies/unity-4.8-api）。
# 同じフォルダのほかの DLL は渡さない
NET_API = [
    "mscorlib.dll", "System.dll", "System.Core.dll", "System.Data.dll", "System.Data.DataSetExtensions.dll",
    "System.Drawing.dll", "System.IO.Compression.dll", "System.IO.Compression.FileSystem.dll",
    "System.ComponentModel.Composition.dll", "System.Net.Http.dll", "System.Numerics.dll",
    "System.Numerics.Vectors.dll", "System.Runtime.Serialization.dll", "System.Transactions.dll",
    "System.Xml.dll", "System.Xml.Linq.dll", "Microsoft.CSharp.dll",
]

# Unity がコンパイラに渡すものと同じ（Library/Bee/artifacts/*/<アセンブリ>.rsp から）
CSC_OPTIONS = [
    "-target:library", "-langversion:9.0", "-nologo", "-utf8output", "-preferreduilang:en-US",
    "-RuntimeMetadataVersion:v4.0.30319",
    "-nowarn:0169", "-nowarn:0649", "-nowarn:0282", "-nowarn:1701", "-nowarn:1702",
]

VERSION_RE = re.compile(r"(\d+)\.(\d+)\.(\d+)[abfpx]\d+")


def load_json(path):
    with io.open(path, encoding="utf-8-sig") as f:
        return json.load(f)


def rel(path):
    try:
        return os.path.relpath(path, ROOT).replace("\\", "/")
    except ValueError:
        # Windows で別ドライブのとき
        return path.replace("\\", "/")


def short_name(package):
    return package.replace("com.vgc-laboratory.", "")


# ---------------------------------------------------------------- Unity を探す

def hub_editor_dirs():
    """Unity Hub が版ごとのフォルダを置く場所"""
    dirs = []
    if os.name == "nt":
        for env in ("ProgramFiles", "ProgramW6432"):
            if os.environ.get(env):
                dirs.append(os.path.join(os.environ[env], "Unity", "Hub", "Editor"))
        settings = os.path.join(os.environ.get("APPDATA", ""), "UnityHub")
    else:
        dirs += ["/Applications/Unity/Hub/Editor", os.path.expanduser("~/Unity/Hub/Editor")]
        settings = os.path.expanduser(
            "~/Library/Application Support/UnityHub" if sys.platform == "darwin" else "~/.config/UnityHub")
    # Unity Hub で導入先を変えていると、ここに書いてある
    try:
        second = load_json(os.path.join(settings, "secondaryInstallPath.json"))
        if second:
            dirs.append(second)
    except (OSError, ValueError):
        pass
    seen, out = set(), []
    for d in dirs:
        key = os.path.normcase(os.path.abspath(d))
        if key not in seen and os.path.isdir(d):
            seen.add(key)
            out.append(d)
    return out


def data_dir(path):
    """Unity のフォルダ（版のフォルダ・Editor・Data・Unity.app/Contents のどれでも）から Data を返す"""
    for cand in (os.path.join(path, "Editor", "Data"), os.path.join(path, "Data"), path,
                 os.path.join(path, "Unity.app", "Contents"), os.path.join(path, "Contents")):
        if os.path.isdir(os.path.join(cand, "Managed", "UnityEngine")):
            return cand
    return None


def find_unity(wanted):
    """wanted（"2022.3" など）に合う、導入済みの最新の版の Data を返す"""
    found = []
    for hub in hub_editor_dirs():
        for name in os.listdir(hub):
            m = VERSION_RE.fullmatch(name)
            data = data_dir(os.path.join(hub, name))
            if m and data and "%s.%s" % m.group(1, 2) == wanted:
                found.append((tuple(int(x) for x in m.group(1, 2, 3)), data))
    return max(found)[1] if found else None


def compiler(data):
    dotnet = os.path.join(data, "NetCoreRuntime", "dotnet.exe" if os.name == "nt" else "dotnet")
    csc = os.path.join(data, "DotNetSdkRoslyn", "csc.dll")
    if os.path.isfile(dotnet) and os.path.isfile(csc):
        return [dotnet, "exec", csc, "-nostdlib", "-noconfig"]
    return None


def unity_references(data):
    managed = os.path.join(data, "Managed", "UnityEngine")
    engine = sorted(os.path.join(managed, n) for n in os.listdir(managed) if n.endswith(".dll"))
    graphs = os.path.join(data, "Managed", "UnityEditor.Graphs.dll")
    if os.path.isfile(graphs):
        engine.append(graphs)
    api = os.path.join(data, "UnityReferenceAssemblies", "unity-4.8-api")
    facades = os.path.join(api, "Facades")
    net = [os.path.join(api, n) for n in NET_API if os.path.isfile(os.path.join(api, n))]
    net += sorted(os.path.join(facades, n) for n in os.listdir(facades) if n.endswith(".dll"))
    return engine, net


def base_defines(version):
    plat = "WIN" if os.name == "nt" else ("OSX" if sys.platform == "darwin" else "LINUX")
    out = ["UNITY_EDITOR", "UNITY_EDITOR_64", "UNITY_EDITOR_" + plat, "UNITY_64", "PLATFORM_ARCH_64",
           "UNITY_STANDALONE", "UNITY_STANDALONE_" + plat, "PLATFORM_STANDALONE", "PLATFORM_STANDALONE_" + plat,
           "ENABLE_MONO", "NET_4_6", "NET_UNITY_4_8", "DEBUG", "TRACE", "CSHARP_7_OR_LATER", "CSHARP_7_3_OR_NEWER"]
    if version:
        major, minor, patch = version
        out += ["UNITY_%d_%d_%d" % version, "UNITY_%d_%d" % (major, minor), "UNITY_%d" % major]
        steps = [(5, m) for m in (3, 4, 5, 6)]
        for year in range(2017, 2024):
            steps += [(year, m) for m in range(1, 5 if year < 2020 else 4)]
        if major >= 6000:
            steps += [(major, m) for m in range(0, minor + 1)]
        out += ["UNITY_%d_%d_OR_NEWER" % s for s in steps if s <= (major, minor)]
    return out


# ---------------------------------------------------------------- パッケージと asmdef

def repo_packages():
    """パッケージ ID -> (フォルダ, package.json)"""
    out = {}
    for name in sorted(os.listdir(PACKAGES)):
        path = os.path.join(PACKAGES, name, "package.json")
        if os.path.isfile(path):
            data = load_json(path)
            out[data["name"]] = (os.path.dirname(path), data)
    return out


def dependencies(package_json):
    deps = dict(package_json.get("dependencies") or {})
    deps.update(package_json.get("vpmDependencies") or {})
    return deps


def blocker(name, packages, memo):
    """name を入れるのに要る、このリポジトリに無いパッケージ（間接も見る）。無ければ None"""
    if name not in packages:
        return name
    if name not in memo:
        memo[name] = None  # 循環よけ
        for dep in sorted(dependencies(packages[name][1])):
            found = blocker(dep, packages, memo)
            if found is not None:
                memo[name] = found
                break
    return memo[name]


def with_repo_deps(names, packages):
    out, todo = set(), list(names)
    while todo:
        name = todo.pop()
        if name in out or name not in packages:
            continue
        out.add(name)
        todo += dependencies(packages[name][1])
    return out


def ignored_dir(name):
    """Unity が取り込まないフォルダ"""
    return name.startswith(".") or name.endswith("~") or name.lower() == "cvs"


def read_guid(meta):
    try:
        with io.open(meta, encoding="utf-8", errors="replace") as f:
            m = re.search(r"^guid: ([0-9a-f]{32})", f.read(), re.MULTILINE)
        return m.group(1) if m else None
    except OSError:
        return None


class Assembly:
    def __init__(self, path, package):
        self.path = path
        self.dir = os.path.dirname(path)
        self.package = package
        self.data = load_json(path)
        self.name = self.data["name"]
        self.guid = read_guid(path + ".meta")
        self.status = None  # OK / NG / 除外 / 未確認
        self.notes = []
        self.errors = []
        self.output = None

    @property
    def editor_only(self):
        return self.data.get("includePlatforms") == ["Editor"]

    @property
    def in_editor(self):
        include = self.data.get("includePlatforms") or []
        exclude = self.data.get("excludePlatforms") or []
        return "Editor" in include if include else "Editor" not in exclude

    def sources(self):
        """asmdef のフォルダの .cs。別の asmdef / asmref があるフォルダから下は含めない"""
        out = []
        for dirpath, dirnames, filenames in os.walk(self.dir):
            if dirpath != self.dir and any(f.endswith((".asmdef", ".asmref")) for f in filenames):
                dirnames[:] = []
                continue
            dirnames[:] = sorted(d for d in dirnames if not ignored_dir(d))
            out += [os.path.join(dirpath, f) for f in sorted(filenames) if f.endswith(".cs")]
        return out


def find_assemblies(package_dir, package):
    out = []
    for dirpath, dirnames, filenames in os.walk(package_dir):
        dirnames[:] = sorted(d for d in dirnames if not ignored_dir(d))
        out += [Assembly(os.path.join(dirpath, f), package) for f in sorted(filenames) if f.endswith(".asmdef")]
    return out


def constraint_ok(expr, defines):
    """defineConstraints の 1 項目（"A"、"!A"、"A || B"）"""
    for term in expr.split("||"):
        term = term.strip()
        if term.startswith("!") and term[1:].strip() not in defines:
            return True
        if not term.startswith("!") and term in defines:
            return True
    return False


# ---------------------------------------------------------------- コンパイル

def quote(path):
    return '"%s"' % path.replace("\\", "/")


def compile_assembly(asm, by_ref, cmd, engine_refs, net_refs, defines, installed, outdir):
    data = asm.data
    own = [v["define"] for v in data.get("versionDefines") or [] if v.get("name") in installed and v.get("define")]
    symbols = set(defines) | set(own)
    if asm.editor_only:
        symbols.add("UNITY_EDITOR_ONLY_COMPILATION")

    unmet = [c for c in data.get("defineConstraints") or [] if not constraint_ok(c, symbols)]
    if unmet:
        asm.status = "除外"
        asm.notes.append("defineConstraints: " + ", ".join(unmet))
        return

    refs = []
    for ref in data.get("references") or []:
        target = by_ref.get(ref)
        if target is None:
            asm.notes.append("参照先が無いので外した: " + ref)
        elif target.status == "OK":
            refs.append(target.output)
        elif target.status == "除外":
            asm.notes.append("参照先が除外されたので外した: " + target.name)
        else:
            asm.status = "未確認"
            asm.notes.append("参照先 %s が通らない" % target.name)
            return
    if data.get("overrideReferences"):
        for dll in data.get("precompiledReferences") or []:
            asm.notes.append("precompiledReferences は渡していない: " + dll)

    sources = asm.sources()
    if not sources:
        asm.status = "OK"
        asm.notes.append(".cs が無い")
        return

    asm.output = os.path.join(outdir, asm.name + ".dll")
    lines = list(CSC_OPTIONS) + ["-out:" + quote(asm.output)]
    if data.get("allowUnsafeCode"):
        lines.append("-unsafe")
    lines += ["-define:" + d for d in sorted(symbols)]
    unity = [] if data.get("noEngineReferences") else engine_refs
    lines += ["-r:" + quote(r) for r in unity + net_refs + refs]
    lines += [quote(s) for s in sources]
    rsp = os.path.join(outdir, asm.name + ".rsp")
    with io.open(rsp, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines) + "\n")

    result = subprocess.run(cmd + ["@" + rsp], stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    text = result.stdout.decode("utf-8", errors="replace")
    asm.errors = [line.strip() for line in text.splitlines() if ": error " in line]
    if result.returncode == 0:
        asm.status = "OK"
    else:
        asm.status = "NG"
        if not asm.errors:
            asm.errors = [line.strip() for line in text.splitlines() if line.strip()]
        asm.output = None


def order(assemblies, by_ref):
    """参照先が先に来る順"""
    done, out = set(), []

    def visit(asm, stack):
        if asm.name in done or asm.name in stack:
            return
        for ref in asm.data.get("references") or []:
            target = by_ref.get(ref)
            if target is not None:
                visit(target, stack | {asm.name})
        done.add(asm.name)
        out.append(asm)

    for asm in assemblies:
        visit(asm, frozenset())
    return out


def main(argv):
    parser = argparse.ArgumentParser(
        description="VRChat の SDK が無い素の Unity で、パッケージがコンパイルできるかを調べる（Unity エディタは起動しない）")
    parser.add_argument("packages", nargs="*",
                        help="調べるパッケージのフォルダ（既定: VRChat の SDK などに依存しないパッケージ全部）")
    parser.add_argument("--unity", help="Unity のフォルダ（例: C:/Program Files/Unity/Hub/Editor/2022.3.22f1）")
    args = parser.parse_args(argv[1:])
    out = io.open(sys.stdout.fileno(), "w", encoding="utf-8", closefd=False)

    packages = repo_packages()
    memo = {}
    if args.packages:
        chosen = []
        for path in args.packages:
            pj = os.path.join(path, "package.json")
            if not os.path.isfile(pj):
                sys.stderr.write("package.json がありません: %s\n" % path)
                return 2
            chosen.append(load_json(pj)["name"])
        targets = sorted(with_repo_deps(chosen, packages))
        skipped = []
    else:
        targets = sorted(n for n in packages if blocker(n, packages, memo) is None)
        skipped = sorted((n, blocker(n, packages, memo)) for n in packages if n not in targets)

    wanted = max(packages[n][1].get("unity", "") for n in targets) if targets else ""
    data = data_dir(args.unity) if args.unity else find_unity(wanted)
    if data is None:
        sys.stderr.write("Unity %s が見つかりません。--unity で Unity のフォルダを指定してください\n" % wanted)
        return 2
    cmd = compiler(data)
    if cmd is None:
        sys.stderr.write("Unity に同梱のコンパイラがありません: %s\n" % data)
        return 2
    m = VERSION_RE.search(data.replace("\\", "/"))
    version = tuple(int(x) for x in m.group(1, 2, 3)) if m else None

    out.write("Unity: %s\n" % data.replace("\\", "/"))
    out.write("対象: %s\n" % ", ".join(short_name(n) for n in targets))
    if skipped:
        out.write("対象外: %s\n" % ", ".join("%s（%s が要る）" % (short_name(n), b) for n, b in skipped))
    out.write("\n")

    assemblies = []
    for name in targets:
        assemblies += find_assemblies(packages[name][0], name)
    by_ref = {}
    for asm in assemblies:
        by_ref[asm.name] = asm
        if asm.guid:
            by_ref["GUID:" + asm.guid] = asm

    engine_refs, net_refs = unity_references(data)
    defines = base_defines(version)
    with tempfile.TemporaryDirectory(prefix="vgc-compile-") as outdir:
        for asm in order(assemblies, by_ref):
            if not asm.in_editor:
                asm.status = "除外"
                asm.notes.append("エディタでコンパイルしない（includePlatforms / excludePlatforms）")
                continue
            compile_assembly(asm, by_ref, cmd, engine_refs, net_refs, defines, set(targets), outdir)

    root = ROOT.replace("\\", "/") + "/"
    for asm in assemblies:
        out.write("[%s] %s（%s）\n" % (asm.status, asm.name, rel(asm.path)))
        for note in asm.notes:
            out.write("    %s\n" % note)
        for err in asm.errors:
            out.write("    %s\n" % err.replace("\\", "/").replace(root, ""))

    count = {s: sum(1 for a in assemblies if a.status == s) for s in ("OK", "NG", "未確認", "除外")}
    out.write("\n通った: %d、通らない: %d、未確認: %d、除外: %d\n" % (count["OK"], count["NG"], count["未確認"], count["除外"]))
    out.flush()
    return 1 if count["NG"] or count["未確認"] else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
