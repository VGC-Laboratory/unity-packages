# VGC UI Extension

**ワールド専用。** uGUI の `Button` に、ホバーとクリックのスケール演出を足した `UdonSharpBehaviour`。
演出は VRChat SDK の `VRCTween` で行うので、追加のアニメーターもコルーチンも要らない。

```
Runtime/                            asmdef: VGC.UIExtension.Runtime
└── ButtonExtension.cs              Button のホバー拡大・クリック縮みと色の差し替え

Editor/                             asmdef: VGC.UIExtension.Editor
└── ButtonExtensionEditor.cs        GameObject > UI > ButtonExtension(VGC)
```

`UdonSharpBehaviour` なので、asmdef と対になる U# Assembly Definition
（`VGC.UIExtension.Runtime.asset`）を持つ。

---

## 入れかた

ヒエラルキーの右クリック、または GameObject メニューから
**UI > ButtonExtension(VGC)** を選ぶ。TextMeshPro 版の Button が作られ、
`ButtonExtension` と `EventTrigger` が付いた状態で置かれる。

手で付ける場合は、`Button` のある GameObject に `ButtonExtension` を足すだけでよい。
`_button` の参照と `EventTrigger` の `PointerEnter` / `PointerExit` の配線は
[VGC Attributes](../com.vgc-laboratory.attributes/README.md) の属性が自動でやる
（`AddButtonSendCustomEventField` / `AddEventTriggerSendCustomEventField`）。
`EventTrigger` は `RequireComponent` で付く。

## 演出

| きっかけ | 動き |
|---|---|
| カーソルが乗る | 1.1 倍へ 0.3 秒（`OutQuad`） |
| カーソルが外れる | 元の大きさへ 0.3 秒（`InQuad`） |
| 押す | 0.9 倍へ 0.05 秒（`OutQuad`）→ 元へ 0.1 秒（`OutBack`） |

元の大きさは `SelfLocalScaleField` でシーン上の localScale から取る。
Inspector には出ないので、大きさはいつもどおり Transform で決めればよい。

**`interactable` が false のボタンは拡大しない。** `EventTrigger` は無効なボタンでも発火するので、
拡大するかどうかの判定は `GetTargetScale()` の 1 箇所に集めてある。
ハイライト中に `Interactable` を false にしたときも、`Interactable` の setter が
縮小を追いかけるので、拡大したまま固まらない。

Tween のハンドルは 1 本だけ持ち、張り直す前に必ず `Kill()` する。
同じ大きさへの張り直しは省くが、クリックの縮みは連打でも毎回再生し直す。

## 継承して使う

`_OnClick` / `_OnHighlight` / `_OnUnhighlight` は `virtual`。
押したときの処理は `_OnClick` を override して `base._OnClick()` を呼ぶ形で足す。

```csharp
public class MyButton : ButtonExtension
{
    public override void _OnClick()
    {
        base._OnClick();   // 縮みの演出
        // ここに自分の処理
    }
}
```

`Start` と `OnDestroy` も `virtual` なので、override したら `base` を呼ぶこと
（`OnDestroy` で Tween を Kill している）。

## 色の差し替え

- `_SetColor(Color)` … `normalColor` を指定色にし、ハイライト・押下・無効の色をそこから作る。
- `_ResetColor()` … 最初の `ColorBlock` へ戻す。

既定色は遅延して取る。`Start` より先に `_SetColor` が呼ばれても、
既定色が真っ黒で焼き付かない。

## 依存関係

```
com.vrchat.worlds                   VRCTween、VRCTMP、UdonSharp
com.vgc-laboratory.attributes       参照と EventTrigger の自動配線
```

TextMeshPro は `com.vrchat.worlds` 経由で入る。
`ButtonExtensionEditor` は VRChat SDK 内の `VRCTMP` の非公開メソッドを
リフレクションで呼んでいる（SDK 標準の Button と同じ見た目で作るため）。
SDK の更新でここが壊れることはありうる。
