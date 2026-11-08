# VGC Localization (World)

VRChat ワールドで、スプレッドシート由来の翻訳表（`LocalizationTable`、com.vgc-laboratory.localization）を使うためのパッケージ。

## 使い方

1. **言語を決める**: Project Settings > VGC > Localization で、使う言語の一覧と既定言語を設定する。一覧の並び順が言語の番号になる。
2. **翻訳表を作る**: Create > VGC > Localization > Spreadsheet Source でスプレッドシートを登録し（ID か URL を貼る）、Create > VGC > Localization > Localization Table でシート名を指定して「Import」。
3. **シーンに LocalizationSystem を置く**: Hierarchy の右クリック > VGC > Localization System。
4. **翻訳を使う UdonSharpBehaviour を `LocalizedUdonBehaviour` から派生させる**。同じ GameObject に `LocalizationBindings` を付け、翻訳表と「文字欄とキー」を並べる。
5. 保存・ビルド・再生のときに自動で焼き込まれる。言語を変えると文字欄は自動で書き換わる。

```csharp
public class MyWindow : LocalizedUdonBehaviour
{
    [SerializeField] private TextMeshProUGUI _status;

    public override void _OnLanguageChanged()
    {
        base._OnLanguageChanged();            // Bindings の文字欄を書き換える
        _status.text = _Get("Status.Ready");  // コードから引く
    }
}
```

## 仕組み

| 部品 | 役割 |
|---|---|
| `LocalizationSystem`（Udon） | 表示言語の管理。起動時は VRChat 本体の表示言語（`VRCPlayerApi.GetCurrentLanguage`）に合わせ、`OnLanguageChanged` にも追従する。利用者が `_SetLanguageIndex` で選んだ後はそちらを優先する |
| `LocalizedUdonBehaviour`（Udon） | 翻訳を受け取る基底。`_Get(key)` / `_GetByIndex(i)`、言語変更時の `_OnLanguageChanged` |
| `LanguageButton`（Udon） | 押すと決まった言語（-1 なら次の言語）に切り替える |
| `LocalizationBindings`（エディタ専用） | 焼き込む翻訳表と、文字欄（Text / TextMeshProUGUI）とキーの対応 |
| `LocalizationBaker`（エディタ） | ビルド・再生開始・シーン保存・エディタの言語変更のときに、`[言語][キー]` の配列を焼き込む。訳が無いところは表の既定言語、それも無ければキーで埋める |

- エディタの言語（Project Settings の言語選択や各インスペクタの Language）を変えると、シーンの文字欄がその言語で書き換わる（プレビュー）。
- 実行中に生成したオブジェクトは `LocalizationSystem._AddListener` で通知先に加える。
