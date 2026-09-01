using UdonSharp;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VGC.Attributes.Runtime;
using VGC.Attributes.Udon.Runtime;
using VRC.SDK3.Components;

namespace VGC.UIExtension.Runtime
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(EventTrigger)),
     AddEventTriggerSendCustomEventField(EventTriggerType.PointerEnter, nameof(_OnHighlight)),
     AddEventTriggerSendCustomEventField(EventTriggerType.PointerExit, nameof(_OnUnhighlight))]
    public class ButtonExtension : UdonSharpBehaviour
    {
        private const float HighlightScaleRate = 1.1f;
        private const float PunchScaleRate = 0.9f;

        [SerializeField, AddButtonSendCustomEventField(nameof(_OnClick))] protected Button _button;
        [SerializeField, SelfLocalScaleField, HideInInspector] protected Vector3 _defaultScale;

        private ColorBlock _defaultButtonColor;
        private bool _isDefaultButtonColorCached;
        protected bool _isHighLight;
        protected bool _isPunching;
        // scaleのTweenは全てこのハンドルで管理する(貼り直す前に必ずKillする)
        protected VRCTweenHandle _scaleTweenHandle;
        // 現在Tween中のターゲット。同じ値への張り直しを省くために保持する
        private Vector3 _scaleTweenTarget;
        private bool _hasScaleTweenTarget;

        public bool Interactable
        {
            get => _button.interactable;
            set
            {
                if (_button.interactable == value)
                    return;

                _button.interactable = value;

                // カーソルを乗せたまま有効/無効が切り替わることがあるので拡大状態を追従させる。
                // これが無いと、ハイライト中に無効化されたボタンが拡大したまま残る
                if (_isHighLight && !_isPunching)
                    PlayScaleTween(GetTargetScale(), 0.3f, VRCTweenEase.InQuad);
            }
        }

        protected virtual void Start()
        {
            CacheDefaultButtonColor();
        }

        protected virtual void OnDestroy()
        {
            _scaleTweenHandle.Kill();
        }

        public virtual void _OnClick()
        {
            PunchScale();
        }

        public virtual void _OnHighlight()
        {
            _isHighLight = true;

            // パンチ中はハイライトのTweenで上書きしない
            if(!_isPunching)
                PlayScaleTween(GetTargetScale(), 0.3f, VRCTweenEase.OutQuad);
        }

        public virtual void _OnUnhighlight()
        {
            _isHighLight = false;

            // interactable で早期returnしてはいけない。
            // ハイライト中に無効化されたボタンはPointerExitが来ても縮小されず、
            // 拡大したまま固まる（interactable=false でも EventTrigger は発火する）
            if(!_isPunching)
                PlayScaleTween(GetTargetScale(), 0.3f, VRCTweenEase.InQuad);
        }

        public void _SetColor(Color color)
        {
            if (_button == null) return;

            CacheDefaultButtonColor();
            var colors = _button.colors;

            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            colors.selectedColor = color;
            colors.disabledColor = Color.Lerp(color, _defaultButtonColor.disabledColor, 0.2f);

            _button.colors = colors;
        }

        public void _ResetColor()
        {
            if (_button == null) return;

            CacheDefaultButtonColor();
            _button.colors = _defaultButtonColor;
        }

        private void CacheDefaultButtonColor()
        {
            // _SetColor がStartより先に呼ばれると既定色が黒(default)で焼き付くため遅延初期化する
            if (_isDefaultButtonColorCached) return;

            _defaultButtonColor = _button.colors;
            _isDefaultButtonColorCached = true;
        }

        private Vector3 GetTargetScale()
        {
            // 無効化されたボタンはカーソルが乗っていても拡大しない。
            // 拡大するかどうかの判定をここ1箇所に集約し、
            // 各イベント側で interactable を見て早期returnしないようにする
            return _defaultScale * (_isHighLight && _button.interactable ? HighlightScaleRate : 1f);
        }

        private void PunchScale()
        {
            _isPunching = true;

            // 連打でも毎回再生し直したいので PlayScaleTween（同値スキップ）は通さない。
            // ハンドルを使い回すとTweenのターゲット値が生成時のまま固定されるため貼り直す。
            // OnComplete を代入前に繋ぐ必要があるので SetScaleTween ではなく直接組み立てる
            _scaleTweenTarget = GetTargetScale() * PunchScaleRate;
            _hasScaleTweenTarget = true;

            _scaleTweenHandle.Kill();
            _scaleTweenHandle = transform.TweenScale(
                                             _scaleTweenTarget,
                                             0.05f,
                                             VRCTweenEase.OutQuad
                                         )
                                         .OnComplete(this, nameof(_OnPunchScaleComplete));
        }

        public void _OnPunchScaleComplete()
        {
            _isPunching = false;

            // 完了コールバック内なのでKillせずそのまま貼り直す。
            // ここでターゲットを記録しないと、パンチ前と同じ値への復帰が
            // PlayScaleTween で「変化なし」と判定されて縮んだまま残る
            _scaleTweenTarget = GetTargetScale();
            _hasScaleTweenTarget = true;
            _scaleTweenHandle = transform.TweenScale(_scaleTweenTarget, 0.1f, VRCTweenEase.OutBack);
        }

        private void PlayScaleTween(Vector3 targetScale, float duration, VRCTweenEase ease)
        {
            // 同じターゲットへ張り直しても見た目は変わらないので、Tweenの生成ごと省く。
            // 無効なボタンにカーソルを乗せた場合など、既定サイズ -> 既定サイズの
            // 無効果なTweenが毎回走るのを防ぐ
            if (_hasScaleTweenTarget && _scaleTweenTarget == targetScale)
                return;

            SetScaleTween(targetScale, duration, ease);
        }

        /// <summary>
        /// ターゲットが同じでも必ず張り直す。パンチのように再生し直したい場合に使う
        /// </summary>
        private void SetScaleTween(Vector3 targetScale, float duration, VRCTweenEase ease)
        {
            _scaleTweenTarget = targetScale;
            _hasScaleTweenTarget = true;

            _scaleTweenHandle.Kill();
            _scaleTweenHandle = transform.TweenScale(targetScale, duration, ease);
        }
    }
}