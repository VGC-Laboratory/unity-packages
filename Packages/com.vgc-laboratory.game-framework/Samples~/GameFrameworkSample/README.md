# Game Framework Sample

[`com.vgc-laboratory.game-framework`](../../README.md) の最小リファレンス実装。

> ゲーム開始前に 3 秒のカウントダウンをして、10 秒カウントし、ゲームを終了するサンプル

---

## 構成

asmdef: `VGC.GameFrameworkSample.Runtime`（参照: `UdonSharp.Runtime`, `Unity.TextMeshPro`,
`VGC.Attributes.Runtime`, `VGC.GameFramework.Runtime`, `VGC.Core.Runtime`）。
UdonSharpBehaviour を含むため、asmdef と対になる U# Assembly Definition
（`VGC.GameFrameworkSample.Runtime.asset`）の Source Assembly も設定すること。


| ファイル | 内容 |
|---|---|
| `Runtime/GameMain.cs` | フェーズ状態機械の本体（partial） |
| `Runtime/Phase/GameMain.*Phase.cs` | 各フェーズの `Initialize` / `Update`（partial 分割） |
| `Runtime/GamePlayerSample.cs` | `GamePlayerBase` を継承しただけの枠マーカー |

`GameMain` は `partial class GameMain : UdonSharpBehaviour`、`BehaviourSyncMode.Manual`。
`IGameHostChanged` / `IGameStateChanged` / `IGamePlayerChanged` を実装。
`[AddComponentMenu("GameMainSample")]`。

---

## フェーズ状態機械

`enum GamePhaseSample { IdlePhase, StartPhase, MainPhase, EndPhase }`

- `[UdonSynced] GamePhaseSample us_phase` … 公開は読み取り専用の `US_Phase`。
- **`FieldChangeCallback` は使わない。** Udon は synced 変数を1個 heap に書くたびに
  change callback を即発火するため、`us_phase` のコールバック時点で `us_syncedStartTime` が
  まだ古い可能性がある。スナップショットが揃うのは `OnDeserialization()`。
- `ApplyPhase()`（冪等）… `_showPhaseText` を更新し `Initialize<Phase>()` を呼ぶ。
  リモートは `OnDeserialization()` から、ホストは `SetPhase()` 内から呼ぶ。
- `SetPhase(phase, resetCountDown)`（ホストのみ）… 「時刻を確定 → `us_phase` 更新 →
  `ApplyPhase()` → `RequestSerialization()`」の順序を1箇所に固定する。
  `Initialize<Phase>()` が時刻を読む場合に備え、フェーズより先に時刻を確定させる。
- `Update()` は**カウントダウン表示が要る StartPhase / MainPhase のときだけ**分岐する。
- `[UdonSynced] double us_syncedStartTime` … `SetPhase(..., resetCountDown: true)` で
  `Networking.GetServerTimeInSeconds()` に更新。カウントダウンは `TimeHelper.ShowCountDownTime` で `_countDownText` に表示。

### 進行フロー（フェーズ遷移はすべて `_isHost` のクライアントのみ実行。他は synced `us_phase` で追従）

| 契機 | 処理 |
|---|---|
| `_OnGameStart()`（framework から） | host: `SetPhase(StartPhase, resetCountDown: true)` |
| `UpdateStartPhase()` | 残り 0 で host: `SetPhase(MainPhase, resetCountDown: true)`（`StartPhaseCountDownTime = 3`） |
| `UpdateMainPhase()` | 残り 0 で host: `SetPhase(EndPhase, resetCountDown: false)`（`MainPhaseCountDownTime = 10`） |
| `InitializeEndPhase()` | host: `SendCustomEventDelayedFrames(_RequestEndGameDelayed, 1)`。`ApplyPhase()` から同期的に呼ぶと `_RequestEndGame` → `_OnGameEnd` → `_RequestExitAll` → `_OnExitAll` → `SetPhase(IdlePhase)` が同じコールスタックでネストし、外側の `SetPhase(EndPhase)` の `RequestSerialization()` が IdlePhase を送ってしまうため1フレーム遅らせる |
| `_OnGameEnd()` | エントリー中のローカルプレイヤーを登録地点へテレポートさせるフック（`Networking.LocalPlayer.TeleportTo()` はコメントアウト）。host: `_gameSystem._RequestExitAll()` |
| `_OnExitAll()` | host: `SetPhase(IdlePhase, resetCountDown: false)` |
| `OnPlayerRespawn(player)` | `player.isLocal` のとき `_gameSystem._RequestExitLocalPlayer()`。`OnPlayerRespawn` はリスポーンした本人のクライアントでのみ発火するため、host 判定で弾くと機能しない |

`IdlePhase` は待機状態（`_countDownText` を空に）。

### プレイヤーのテレポートについて

`_OnGameEnd()` にエントリー中プレイヤーを登録地点へ戻すためのフック（コメントアウト済み）がある。
同様に **ゲーム開始時（`_OnGameStart()` / `StartPhase` 遷移時）にもプレイフィールドへテレポートさせたいケースがある**ため、
開始側にもテレポート処理を差し込めるようコメントで明示している。開始・終了の双方が想定される拡張ポイント。

---

## 自動参照（Executor 属性）

- `_gameSystem` = `AutoPopulateField(typeof(GameSystemMain), ExecutorScope.Parents)`
- `_gamePlayers` = `AutoPopulateField(typeof(GamePlayerSample), ExecutorScope.ParentHierarchy)`

`IGamePlayerChanged` の引数受け口として `_entryArgs` / `_exitArgs`（`int[]`）を宣言している。
名前は `GameSystemMain.EntryArgsVariableName` / `ExitArgsVariableName` と一致させること。

`_showPhaseText` / `_countDownText`（`TextMeshProUGUI`）は手動でアサイン。

---

## 使い方

`GameSystemMain` 配下（`EntryPanel` を含む階層）に `GameMain` を置き、`GamePlayerSample` を枠数だけ配置。
プレイヤーがエントリーして Start ボタンを押すと `StartPhase → MainPhase → EndPhase → IdlePhase` と進む。


