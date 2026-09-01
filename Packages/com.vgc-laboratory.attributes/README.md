# VGC.Attributes

Inspector 表示補助と、**エディタ／ビルド時にフィールド参照を自動配線する仕組み（Executor）** をまとめたモジュール。
実行時（Udon ランタイム）にはほぼ何もせず、値は事前にシリアライズ済みの状態で焼き込まれる。

---

## 構成

```
Attributes/
├── Runtime/                    asmdef: VGC.Attributes.Runtime
│   ├── ReadOnlyAttribute.cs
│   ├── ExecutorScope.cs
│   ├── ExecutorOrder.cs
│   ├── ExecuteScopeHelper.cs
│   ├── ExecutorSharedCache.cs
│   ├── FindObjectsCache.cs
│   ├── IExecutorFieldAttribute.cs
│   ├── IExecutorClassAttribute.cs
│   ├── AutoPopulateUtility.cs
│   ├── AutoAssignIndexUtility.cs
│   └── Fields/                 属性の定義本体（7個）
└── Editor/                     asmdef: VGC.Attributes.Editor
    └── AttributeExecutor.cs    属性を走査して実行するドライバ
```

`Runtime/` の実行ロジックは `#if UNITY_EDITOR && !COMPILER_UDONSHARP` で閉じているが、
**Editor asmdef には移せない**。`AutoPopulateFieldAttribute.Execute`（Runtime）が
`AutoPopulateUtility` を呼ぶため、Editor asmdef に置くと Runtime 側から参照できなくなる。
分離するには「属性はデータ宣言だけ持ち、Editor 側のハンドラが属性を読んで処理する」形への設計変更が要る。

---

## ReadOnlyAttribute

```csharp
[SerializeField, ReadOnly] private int _value;
```

`PropertyAttribute` + `ReadOnlyDrawer`。`EditorGUI.BeginDisabledGroup(true)` で囲って表示するだけ。値の書き換えはしない。

---

## Executor サブシステム

### 仕組み

1. 自動配線したい属性は `IExecutorFieldAttribute`（フィールド用）または `IExecutorClassAttribute`（クラス用）を実装する。
2. `AttributeExecutor.Execute()` がシーン内の全 `MonoBehaviour` を走査し、対象フィールド／クラスの属性を見つけて `Execute(...)` を呼ぶ。
3. `Execute(...)` が `true`（＝実際に値を書き換えた）を返した `MonoBehaviour` だけ、`UdonSharpBehaviour` なら `UdonSharpEditorUtility.CopyProxyToUdon()`、それ以外は `EditorUtility.SetDirty()` される。

> `Execute` の戻り値は「値を書き換えたか」。常に `true` を返すと、変更が無くてもシーン内の全 `UdonSharpBehaviour` に
> `CopyProxyToUdon` が走り、シーン保存と Play モード移行のたびに全件コピーになる。
> `AutoPopulateField` 系は `AutoPopulateUtility.ExecuteField` の `out bool changed` を返す。
> `Self*` / `FindFirst` / `AutoAssignIndex` 系は変更判定を持たないので `true` のまま。
> `AddEventTriggerSendCustomEventField` は変更対象が `EventTrigger` コンポーネント側（Serializer 内で `SetDirty` 済み）なので
> 成功時も `false` を返す。

### 実行タイミング（`AttributeExecutor`）

| 契機 | 実装 | トグル（`IsExecuteInUnityEditor`）の影響 |
|---|---|---|
| スクリプトリロード（`[DidReloadScripts]`） | `OnDidReloadScripts`（各種イベント購読も同時に登録） | ON のときのみ |
| シーンを開いた / 保存する直前 | `EditorSceneManager.sceneOpened` / `sceneSaving` | ON のときのみ |
| Prefab ステージを開いた / 保存した | `PrefabStage.prefabStageOpened` / `prefabSaved` | ON のときのみ |
| コンポーネント追加時 | `ObjectFactory.componentWasAdded` | ON のときのみ |
| **Play モード移行時 / プレイヤービルド時** | `AttributeExecutorBuildProcessor : IProcessSceneWithReport`（`callbackOrder = -10000`）→ `Execute(false)` | **無関係（常に実行）** |

- `IProcessSceneWithReport.OnProcessScene` は「プレイヤービルド時のシーン処理」だけでなく、**エディタで Play モードに入るときにも各シーンに対して呼ばれる**（ビルドでない場合は `BuildReport` が `null`）。
  `AttributeExecutorBuildProcessor` はこの中で `AttributeExecutor.Execute(false)` を **トグルや `EditorApplication.isPlaying` をチェックせず無条件に**呼ぶため、Play モード開始時には設定 OFF でも配線処理が走る。
- 上表の `[DidReloadScripts]` / シーン / Prefab / コンポーネント追加の各コールバックは、`IsExecuteInUnityEditor` が OFF、または `EditorApplication.isPlaying` のときは早期 return する。
- フラグの切り替えはメニュー `VGC/Attribute/Executor/TurnON/TurnOFF ExecuteUnityEditor`。状態は `ScriptableSingleton`（`ScriptableSingleton/AttributeExecutorSetting.dat`）に永続化。
- `IProcessSceneWithReport` 経由の実行は `Execute(false)`（`registerUndo = false`）。

> **シーンに配線を焼き付けたくない場合**はトグルを OFF にしてからシーンを保存する。
> Play モード移行時とビルド時は上記のとおりトグル無視で必ず実行されるので、動作には影響しない。

### 検索スコープ（`ExecutorScope`）

`Self` / `Children` / `ChildrenExcludeSelf` / `Parents` / `Parent` / `ParentHierarchy` / `NearestParent` / `NearestParentHierarchy` / `Root` / `RootHierarchy` / `Scene`。
`NearestParent*` は「`AnchorType` を持つ最も近い親」を基準に検索する。並び順は `ExecutorOrder`（`None` / `Hierarchy`）。

### 属性一覧

| 属性 | 対象 | 内容 |
|---|---|---|
| `AutoPopulateField(targetType, scope, order, anchorType, includeInactive, onlyEnabled, required)` | Field | 指定型／インターフェースのコンポーネントをスコープ検索して代入。配列・`List<T>` 対応。`targetType` 省略時はフィールド型（要素型）を使用。`required` で未発見時に警告。Inspector には `AutoPopulate(...)` タグ付きの読み取り専用表示 |
| `SelfField` | Field | `AutoPopulateField(scope: Self)` のショートハンド |
| `FindFirstField` | Field | シーンからフィールド型の最初の 1 個を代入（`GameObject` 型ならアクティブシーンの最初のルート） |
| `SelfPositionField(SelfPositionState)` | Field | `transform.position` を焼き込み。`X/Y/Z` は `float`、`All` は `Vector3` |
| `SelfRotationField` | Field | `transform.rotation`（`Quaternion`）を焼き込み |
| `SelfLocalScaleField(SelfLocalScaleState)` | Field | `transform.localScale` を焼き込み。`X/Y/Z` は `float`、`All` は `Vector3` |
| `AutoAssignIndexField(anchorType, scope, order)` | Field | 同じ型のコンポーネント群に連番（0,1,2,…）を割り振る。`anchorType` でグループ分けし、既定は `NearestParent` + `Hierarchy` 順 |


Udon / uGUI 固有の `AddButtonSendCustomEventField` / `AddEventTriggerSendCustomEventField` は
別アセンブリ [`VGC.Attributes.Udon`](../AttributesUdon/README.md) にある。

### 補助クラス

- `ExecuteScopeHelper.FindTarget(transform, type, scope)` — スコープ指定の単体検索。
- `ExecutorSharedCache` — `FindObjectsByType` のキャッシュと、階層インデックス文字列（例 `"0001.0003.0002"`）の生成・キャッシュ。`Execute()` の先頭で `Clear()`。
- `AutoAssignIndexCache` — アンカー解決結果のキャッシュ（`AnchorCache`）と、処理済みの `(コンポーネント型, フィールド名)`（`ProcessedFields`）。
  `AutoAssignIndexUtility.ExecuteField` は1回で同型の全インスタンスに Index を割り振るため、
  `ProcessedFields` が無いと N 個のインスタンスに対して N 回同じ処理が走る（O(N²)）。

---

## 依存関係

**`VGC.Attributes.Runtime` の参照はゼロ**（UnityEngine のみ）。UdonSharp にも uGUI にも依存しない。

- **VGC.Attributes.Editor** … `VGC.Attributes.Runtime` を参照。`AttributeExecutor` が `#if UDONSHARP` で UdonSharp を使う。
- **[VGC.Attributes.Udon](../AttributesUdon/README.md)** … Udon / uGUI 固有の属性（`AddButton*` / `AddEventTrigger*`）と、その配線を行う Serializer。
  こちら側が `VGC.Attributes.Runtime` を参照する（依存の向きは Udon → 汎用）。

## メモ

- 実行時コードは属性型の定義のみ。処理は `#if UNITY_EDITOR && !COMPILER_UDONSHARP` に閉じている。
- 値はエディタ／ビルド時にシリアライズされるため、ランタイムでの検索コストは発生しない。
- namespace は `VGC.Attributes`（複数形）。`System.Attribute` と衝突しやすいため。
