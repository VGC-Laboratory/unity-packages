# VGC Core (Worlds)

**ワールド専用のユーティリティ。** displayName に `(Worlds)` が付いているのは、
パッケージ ID が `com.vgc-laboratory.core` という汎用的な名前なのに中身がワールド専用で、
一覧で見たときに誤解されやすいため（ID は公開済みなので変えられない）。

```
Runtime/                            asmdef: VGC.Core.Runtime
├── TimeHelper.cs                   サーバー時刻ベースのカウントダウン
└── SpriteNumberFormatter.cs        int/uint -> TMP の <sprite index=N> 連結
```

`VGC.Core.Runtime` は `UdonSharpBehaviour` を含まないが、`UdonSharpBehaviour` から
`TimeHelper` / `SpriteNumberFormatter` を呼べるようにするため、asmdef と対になる
U# Assembly Definition（`VGC.Core.Runtime.asset`）を持つ。

## なぜワールド専用なのか

`vpmDependencies` は `com.vrchat.worlds`。理由は 2 つで、どちらもアバター用プロジェクトでは成立しない。

- `VGC.Core.Runtime.asmdef` が **`UdonSharp.Runtime`（`com.vrchat.worlds` の中）を条件なしで参照している。**
  参照が解決できないと Unity はこの asmdef を一切コンパイルしない。
- `TimeHelper` が `Networking.GetServerTimeInSeconds()` を使う。
  型は `com.vrchat.base` にあるのでアバター用プロジェクトでもコンパイル自体は通るが、
  サーバー時刻はワールドの同期の仕組みなので中身が意味を持たない。

ワールド用・アバター用のどちらでも使えるものは、
[`attributes`](../com.vgc-laboratory.attributes/README.md) /
[`localization`](../com.vgc-laboratory.localization/README.md) /
[`gimmicks`](../com.vgc-laboratory.gimmicks/README.md) /
[`resources`](../com.vgc-laboratory.resources/README.md) 側にある。

---

## TimeHelper

サーバー時刻ベースのカウントダウン。

- `CalculateRemain(syncedStartTime, duration, out remainTime)` … `Networking.GetServerTimeInSeconds()` と `CalculateServerDeltaTime` で残り秒を算出（0 未満は 0）。
- `ShowCountDownTime(syncedStartTime, duration, TMP_Text, out remainTime, format, showZero)` … `TimeDisplayFormat` に応じて `TMP_Text` へ書き込み。
  - `Normal`（秒・切り上げ）/ `Sprite`（sprite 秒）/ `MinuteSecond`（`mm:ss`）/ `MinuteSecondSprite` / `WithMilliseconds`（`ss.mmm`）。

`syncedStartTime` は同期変数で配る前提。カウントダウンの起点をサーバー時刻に置くことで、
各クライアントが同じ残り時間を独立に算出できる（残り時間自体を同期しなくてよい）。

## SpriteNumberFormatter

`int` / `uint` を TMP の `<sprite index=N>` 連結文字列へ変換。
マイナス記号 = `index 10`、コロン = `index 11`（`SpriteMinusIndex` / `SpriteColonIndex`）。`int.MinValue` も対応。

