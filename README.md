# VGC unity-packages

[VGC-Laboratory](https://github.com/VGC-Laboratory) の Unity / VRChat 向けパッケージ群。

VPM（VRChat Package Manager）で配信する。

## パッケージ

二種類ある。**ワールド制作に使えるライブラリ**と、**翻訳・版の確認・ギミックの導入の基盤**。
どちらも外部のクリエイターが使える。

### 外部のクリエイター向け

ワールド制作に使えるライブラリ。これらを目的に入れる。

| パッケージ | 内容 | 依存 |
|---|---|---|
| [`attributes`](Packages/com.vgc-laboratory.attributes) | フィールド参照の自動配線属性と、それを実行する Executor。**Runtime は依存ゼロ**で素の Unity でも動く | なし |
| [`core`](Packages/com.vgc-laboratory.core) | サーバー時刻カウントダウン、TMP sprite 数値フォーマッタ。**ワールド専用**（VCC では「VGC Core (Worlds)」） | `com.vrchat.worlds` |
| [`ui-extension`](Packages/com.vgc-laboratory.ui-extension) | uGUI Button の UdonSharp 向け拡張 | `com.vrchat.worlds`, `attributes` |
| [`game-framework`](Packages/com.vgc-laboratory.game-framework) | ホスト決定 / エントリー枠管理 / 開始終了状態 | `com.vrchat.worlds`, `attributes`, `core`, `ui-extension` |

各パッケージの README に属性の一覧や設計の意図を書いてある。

### 翻訳・版の確認・ギミックの導入の基盤

**スプレッドシートの ID を設定すれば、誰のツールでも使える。**
[VRC 合法チート研究会](https://goho-cheat-vrc.booth.pm/)で配布している VGC のギミックや
ワールド拡張メニュー（VGCWExM）もこれを使っている。

| パッケージ | 内容 |
|---|---|
| [`localization`](Packages/com.vgc-laboratory.localization) | スプレッドシートから取り込む翻訳表、エディタの表示言語、使用言語の一覧（Project Settings） |
| [`localization-world`](Packages/com.vgc-laboratory.localization-world) | ワールドの実行時（Udon）の翻訳。VRChat 本体の言語に追従、ビルド時に焼き込み |
| [`gimmicks`](Packages/com.vgc-laboratory.gimmicks) | スプレッドシートで管理する版の確認と更新のお知らせ。VGC のギミックの定義・導入 |
| [`gimmicks-avatar`](Packages/com.vgc-laboratory.gimmicks-avatar) | 上のアバター側（導入ウィンドウ、NDMF / Modular Avatar 連携） |
| [`resources`](Packages/com.vgc-laboratory.resources) | 共通アセット（中身が空の AnimationClip） |

翻訳表はプロジェクト全体をシート名で探さず、GUID・パス・フォルダで場所を絞って読む。
別の作者の表と同じシート名でも衝突しない。

ワールド用かアバター用かは `vpmDependencies` で決まる（`com.vrchat.worlds` ならワールド用、
`com.vrchat.avatars` ならアバター用、どちらも無ければ両方で使える）。
[配信ページ](https://vgc-laboratory.github.io/unity-packages/)もこの規則で区分を出し分けている。

## 依存の向き

```
外部のクリエイター向け
  attributes
      ├── attributes/Udon（com.vrchat.worlds がある時のみコンパイル）
      ├── ui-extension
      └── game-framework
  core
      └── game-framework

翻訳・版の確認・ギミックの導入の基盤
  localization
      ├── localization-world
      ├── gimmicks
      │     └── gimmicks-avatar
      └── gimmicks-avatar（VGC.Localization.Avatar）
  resources
      └── gimmicks-avatar
```

循環なし。二つの群の間に依存は無い。

基盤側は、共通の家族に `.Avatar` / `.World` を足した名前でプラットフォーム固有の部分を分ける。

| 家族 | 共通 | アバター | ワールド |
|---|---|---|---|
| 翻訳 | `VGC.Localization` | `VGC.Localization.Avatar` | `VGC.Localization.World` |
| ギミック | `VGC.Gimmicks` | `VGC.Gimmicks.Avatar` | （将来） |
## 開発

リポジトリ直下は Unity プロジェクトではない。**配布するパッケージ本体は `Packages/` に置き、
開発用の Unity プロジェクトを `Projects/` に並べている。**

```
Packages/          配布するパッケージ本体
Projects/World/    ワールド用（com.vrchat.worlds）
Projects/Avatar/   アバター用（com.vrchat.avatars + Modular Avatar）
```

VRChat SDK のワールド用とアバター用は 1 つのプロジェクトに同時に入れられないので、
プロジェクトを 2 つに分けている。どちらのプロジェクトも `Packages/manifest.json` の
`file:../../../Packages/com.vgc-laboratory.*` で同じパッケージ本体を読み込むので、
**どちらで編集しても同じファイルが変わる。**

Unity で開くのは `Projects/World` か `Projects/Avatar`。リポジトリ直下を開いても何も起きない。

> **開発用プロジェクトの `vpm-manifest.json` に `com.vgc-laboratory.*` を書かないこと。**
> VCC の Manage Project や `vrc-get install` で自作パッケージを足すと、
> `file:` 参照と二重になって `.meta` の GUID が重複する。

`Assets/` 配下は VCC / VRChat SDK が自動生成するものなので追跡していない。
無視設定は 3 枚に分かれている。

| ファイル | 役割 |
|---|---|
| `.gitignore` | Unity の生成物（`Library/` `Temp/` `Logs/` `*.csproj` など）。`Projects/<name>/` 配下でも効く |
| `Projects/<name>/.gitignore` | そのプロジェクト固有の `Assets/` 配下の無視設定 |
| `Projects/<name>/Packages/.gitignore` | VCC が入れる VPM パッケージ |

## インストール

**[→ VCC に追加](https://vgc-laboratory.github.io/unity-packages/)**

上のページの「VCC に追加」ボタンを押すと `vcc://` スキームで VCC が起動し、
リポジトリ追加の確認が出る。

手動で追加する場合は、VCC の Settings > Packages > Add Repository に次の URL を入れる。

```
https://vgc-laboratory.github.io/unity-packages/index.json
```

## AI コーディング支援（Claude Code スキル）

`AutoPopulateField` などの属性は、存在を知らないと使われない。
`.claude/skills/vgc-attributes/` に「いつどれを選ぶか」をまとめた
Claude Code 用スキルを同梱している。

導入するとエージェントが `GetComponentInParent` を `Start()` に書く代わりに
属性で解決するようになり、Inspector での手作業も減る。

```bash
npx skills add VGC-Laboratory/unity-packages
```

スキルは「選択の指針」だけを持ち、実装詳細は
[`Packages/com.vgc-laboratory.attributes/README.md`](Packages/com.vgc-laboratory.attributes/README.md)
に委ねている。内容が二重管理にならないようにするため。

## リリース手順

**パッケージごとに個別の版で出す。** 変えたものだけ出せる。

1. 対象の `package.json` の `version` を上げる
2. 依存側の `vpmDependencies` のバージョン範囲も必要なら更新する
3. commit / push
4. タグ `<短縮名>-v<version>` を打つ（短縮名 = パッケージ ID から `com.vgc-laboratory.` を取ったもの）

```bash
git tag gimmicks-v0.2.0 && git push origin gimmicks-v0.2.0
```

`release.yml` がそのパッケージの `version` とタグの一致を確認したうえで ZIP と
listing 用の version entry を作り、Release に添付する。
そのあと `listing.yml` が全 Release から `index.json` を作り直して Pages へ配信する。
手で `index.json` を管理する必要はない。

- パッケージごとの版なので、**パッケージ間の依存は `^0.4.0` のような狭い範囲にしない。**
  `^0.4.0` は `>=0.4.0 <0.5.0` の意味で、片方だけ版を上げると解決できなくなる。
  `>=0.4.0 <1.0.0` のように書く。
- 旧方式のタグ `v*`（全パッケージを同じ版で出す）では起動しない。
  公開済みの `v0.1.0`〜`v0.4.0` の Release はそのまま残している。

#### 複数パッケージをまとめて出すとき

**タグは 1 度に 3 本までしか push しない。** GitHub は
[4 本以上のタグを一度に push するとイベントを作らない](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows)。
9 本まとめて打って 1 本も動かなかったことがある。エラーも出ず、ただ無視される。
打ち直すにはリモートのタグを消してから入れ直すしかない。

```bash
git push origin attributes-v0.5.0 core-v0.5.0 game-framework-v0.5.0
```

**`listing.yml` は `concurrency: group: pages` で待ち行列が 1 本しかない。**
Release が続けて走ると、先に並んでいた listing が押し出されて cancelled になる。
`release` ジョブ自体は成功しているので Release と ZIP は出来ているが、
`index.json` が古いままになる。出し終わってから Actions の
**Build VPM Listing** を手で 1 回実行するのが確実。
listing は毎回すべての Release から作り直すので、1 回走れば全部そろう。
### 検査

`guid-check.yml` が `Packages/` 配下の `.meta` の GUID 重複を調べる。
push / PR で走り、リリースの前にも走る（`release.yml` が待つ）。手元でも同じコマンドで確認できる。

```bash
python3 tools/check-guid-duplicates.py Packages
```

## ライセンス

MIT
