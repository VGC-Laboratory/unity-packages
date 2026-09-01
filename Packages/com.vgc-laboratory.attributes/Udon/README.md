# VGC.Attributes.Udon

[`VGC.Attributes`](../Attributes/README.md) の Udon / uGUI 固有の拡張。

汎用の `VGC.Attributes` は UdonSharp にも uGUI にも依存しない。
「uGUI の `Button` / `EventTrigger` に `SendCustomEvent` を配線する」という
VRChat 固有の処理だけをこちらに分離してある。

```
AttributesUdon/
└── Runtime/                     asmdef: VGC.Attributes.Udon.Runtime
    ├── Fields/
    │   ├── AddButtonSendCustomEventFieldAttribute.cs
    │   └── AddEventTriggerSendCustomEventFieldAttribute.cs
    └── Serializer/
        ├── ButtonSerializer.cs
        └── EventTriggerSerializer.cs
```

参照: `VGC.Attributes.Runtime`, `UdonSharp.Runtime`, `UdonSharp.Editor`, `VRC.Udon`

---

## 属性

| 属性 | 対象 | 内容 |
|---|---|---|
| `AddButtonSendCustomEventField(eventName, targetUdonType, targetUdonScope, addIndex, …)` | Field | `AutoPopulateField` で uGUI `Button` を集め、`onClick` → 対象 `UdonSharpBehaviour` への `SendCustomEvent` を配線。配列の場合、`addIndex: true` なら `{eventName}_{i}` を配線する |
| `AddEventTriggerSendCustomEventField(eventTriggerType, eventName)` | Class | 同一 GameObject の `EventTrigger` に、指定タイプの `SendCustomEvent` エントリを配線 |

どちらも `#if UDONSHARP` でガードされており、UdonSharp が無い環境ではコンパイルされない。

`AddButtonSendCustomEventFieldAttribute` は `AutoPopulateFieldAttribute` を継承し、
`Execute` を override して「フィールドへの代入（基底の処理）＋ Button への配線」を行う。

## Serializer

`Button.onClick` / `EventTrigger.m_Delegates` の永続リスナーを `SerializedProperty` 経由で
直接編集する。どちらも全体が `#if UNITY_EDITOR && !COMPILER_UDONSHARP` でガードされた
エディタ専用コード。

> `m_Target` が ObjectReference を持つ要素に `DeleteArrayElementAtIndex` を呼ぶと、
> Unity は1回目を「参照を null にするだけ」として扱い要素が残る。
> 削除前に `objectReferenceValue = null` を明示すること。

---

## なぜ分離したか

`VGC.Attributes` を単体パッケージとして配布する場合、この2属性のためだけに
UdonSharp / uGUI / VRChat SDK への依存が汎用パッケージ側に入ってしまう。
分離することで `VGC.Attributes.Runtime` の参照はゼロになる。
