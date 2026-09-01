# VGC.Core

他モジュールに依存しない汎用ユーティリティ。

```
Core/
└── Runtime/                        asmdef: VGC.Core.Runtime
    ├── TimeHelper.cs               サーバー時刻ベースのカウントダウン
    └── SpriteNumberFormatter.cs    int/uint -> TMP の <sprite index=N> 連結
```

`VGC.Core.Runtime` は `UdonSharpBehaviour` を含まないが、`UdonSharpBehaviour` から
`TimeHelper` / `SpriteNumberFormatter` を呼べるようにするため、asmdef と対になる
U# Assembly Definition（`VGC.Core.Runtime.asset`）を持つ。

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

