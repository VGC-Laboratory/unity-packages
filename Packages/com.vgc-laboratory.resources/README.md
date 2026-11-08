# VGC Resources

ワールド用・アバター用のどちらからも使う共通アセットの置き場。
**`vpmDependencies` は `com.vrchat.base` だけ**なので、ワールドプロジェクトにもアバタープロジェクトにも入る。

```
Animations/EmptyAnimation.anim   … 中身が空の AnimationClip
```

## EmptyAnimation

カーブ・PPtr・イベントがすべて空の `AnimationClip`。GUID は `b47482bcea2063645b7835eb0572594a`。

- アバター側: Modular Avatar の Merge Animator に渡す、ステートに何も起こさないモーションを置く、など。
- ワールド側: Animator のレイヤーを実質無効化する、Animation の差し替え先として使う、など。

**GUID は変えないこと。** 配布済みの VGC アバターギミック 1.0 系がこの GUID で参照している。

## このパッケージを分けた理由

元は `Assets/VGC/VGCResources` として各アバターギミックの unitypackage に同梱されていた。
VPM パッケージ化（[com.vgc-laboratory.avatar-gimmick-lib](../com.vgc-laboratory.avatar-gimmick-lib/README.md)）の際、
中身がアバター固有ではなくワールドでも使えるものだったため、
**avatar-gimmick-lib（`com.vrchat.avatars` に依存するのでワールドプロジェクトに入れられない）から切り離した。**

旧フォルダ `Assets\VGC\VGCResources`（GUID `f6d57f4e3e4bc244cb83ffa7f9103f0b`）の
後継はこのパッケージなので、`legacyFolders` もこちらが持つ。

## 版の付け方

このリポジトリはパッケージごとに版を持ち、**タグ `resources-v<version>`** で個別にリリースする。
初版は `0.1.0`。

avatar-gimmick-lib はこのパッケージに `>=0.1.0 <1.0.0` で依存する。
`^0.1.0`（= `>=0.1.0 <0.2.0`）のような狭い範囲にはしない。
狭くすると、このパッケージの版を上げるたびに依存側も出し直さないと解決できなくなる。
