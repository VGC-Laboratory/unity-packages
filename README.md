# VGC unity-packages

[VGC-Laboratory](https://github.com/VGC-Laboratory) の Unity / VRChat 向けパッケージ群。

VPM（VRChat Package Manager）で配信する。

## パッケージ

| パッケージ | 内容 | 依存 |
|---|---|---|
| [`com.vgc-laboratory.attributes`](Packages/com.vgc-laboratory.attributes) | フィールド参照の自動配線属性と、それを実行する Executor | `com.vrchat.worlds` ※ |
| [`com.vgc-laboratory.core`](Packages/com.vgc-laboratory.core) | サーバー時刻カウントダウン、TMP sprite 数値フォーマッタ | `com.vrchat.base` |
| [`com.vgc-laboratory.ui-extension`](Packages/com.vgc-laboratory.ui-extension) | uGUI Button の UdonSharp 向け拡張 | `com.vrchat.worlds`, `attributes` |
| [`com.vgc-laboratory.game-framework`](Packages/com.vgc-laboratory.game-framework) | ホスト決定 / エントリー枠管理 / 開始終了状態 | `com.vrchat.worlds`, `attributes`, `core`, `ui-extension` |

※ `attributes` の **Runtime アセンブリは依存ゼロ**で、素の Unity プロジェクトでも動く。
現時点で `com.vrchat.worlds` を要求しているのは Editor 側の `AttributeExecutor` が
`UdonSharpEditorUtility.CopyProxyToUdon` を直接呼んでいるため。
ここを外せば汎用パッケージにできる（[Issue 化候補](#todo)）。

## 依存の向き

```
attributes（汎用）
    ├── attributes/Udon（com.vrchat.worlds がある時のみコンパイル）
    ├── ui-extension
    └── game-framework
core（VRChat base）
    └── game-framework
```

循環なし。上流ほど依存が少ない。

## 開発

このリポジトリ自体が VRChat Worlds プロジェクトになっている。
クローンして Unity で開けば、`Packages/com.vgc-laboratory.*` が埋め込みパッケージとして
読み込まれ、そのままコンパイル・動作確認できる。

> `Packages/.gitignore` は VCC が生成したもので、1行目の `/*/` が `Packages/` 直下の
> 全ディレクトリを無視する。自作パッケージを `!com.vgc-laboratory.*/` で除外解除して
> いないと、**新規ファイルが git status に出ない**。編集する際は消さないこと。

## TODO

- [ ] `AttributeExecutor` から UdonSharp への直接参照を外し、`attributes` を
      `com.vrchat.worlds` 非依存にする（リフレクション経由にするか、
      プロキシ同期をフックとして切り出すか）
- [ ] VPM listing リポジトリの用意（GitHub Pages）
- [ ] リリース用 GitHub Actions

## ライセンス

MIT
