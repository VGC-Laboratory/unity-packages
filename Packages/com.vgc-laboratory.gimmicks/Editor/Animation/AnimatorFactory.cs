using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace VGC.Gimmicks.Animation
{
    /// <summary>
    /// メモリ上でアニメーターを組み立てる。
    ///
    /// AnimatorController.AddLayer / AnimatorStateMachine.AddState などの便利な API は Undo を記録し、
    /// 開いているシーンに未保存の印を付けてしまう。NDMF の VirtualState の Commit と同じく、
    /// new したオブジェクトを配列に入れて代入する形で作る(Undo を記録しない)。
    /// できたコントローラーはアセットとして保存しない前提(MA Merge Animator などに渡す。
    /// MA / NDMF は複製してから結合するので、保存は要らない)。
    ///
    /// 使うのは UnityEditor と UnityEngine だけで、VRChat の SDK・MA・NDMF には依存しない。
    /// </summary>
    public sealed class AnimatorFactory
    {
        private readonly List<AnimatorControllerParameter> _parameters = new();
        private readonly List<LayerBuilder> _layers = new();

        /// <summary>パラメータを足す。同じ名前は 2 回目以降を無視する。</summary>
        public void AddParameter(string name, AnimatorControllerParameterType type, float defaultValue = 0f)
        {
            if (_parameters.Any(p => p.name == name))
                return;

            _parameters.Add(new AnimatorControllerParameter
            {
                name = name,
                type = type,
                defaultBool = defaultValue != 0f,
                defaultInt = Mathf.RoundToInt(defaultValue),
                defaultFloat = defaultValue,
            });
        }

        public LayerBuilder AddLayer(string name, AvatarMask mask = null)
        {
            var layer = new LayerBuilder(name, mask);
            _layers.Add(layer);
            return layer;
        }

        public AnimatorController Build(string name)
        {
            var controller = new AnimatorController { name = name };
            controller.parameters = _parameters.ToArray();
            controller.layers = _layers.Select(l => l.Commit()).ToArray();
            return controller;
        }

        public static AnimatorCondition If(string parameter) =>
            new() { mode = AnimatorConditionMode.If, parameter = parameter, threshold = 0f };

        public static AnimatorCondition IfNot(string parameter) =>
            new() { mode = AnimatorConditionMode.IfNot, parameter = parameter, threshold = 0f };

        public static AnimatorCondition IntEquals(string parameter, int value) =>
            new() { mode = AnimatorConditionMode.Equals, parameter = parameter, threshold = value };

        public static AnimatorCondition IntNotEqual(string parameter, int value) =>
            new() { mode = AnimatorConditionMode.NotEqual, parameter = parameter, threshold = value };

        public static AnimatorCondition Greater(string parameter, float value) =>
            new() { mode = AnimatorConditionMode.Greater, parameter = parameter, threshold = value };

        public static AnimatorCondition Less(string parameter, float value) =>
            new() { mode = AnimatorConditionMode.Less, parameter = parameter, threshold = value };

        /// <summary>条件で即座に移る遷移(固定時間 0 秒、exitTime なし)を足す。</summary>
        public static AnimatorStateTransition AddTransition(AnimatorState from, AnimatorState to,
                                                            params AnimatorCondition[] conditions)
        {
            var transition = NewTransition(to, conditions);
            from.transitions = (from.transitions ?? Array.Empty<AnimatorStateTransition>()).Append(transition).ToArray();
            return transition;
        }

        /// <summary>exitTime(正規化時間)が来たら移る遷移を足す。</summary>
        public static AnimatorStateTransition AddExitTimeTransition(AnimatorState from, AnimatorState to, float exitTime)
        {
            var transition = AddTransition(from, to);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            return transition;
        }

        public static void AddBehaviour(AnimatorState state, StateMachineBehaviour behaviour)
        {
            state.behaviours = (state.behaviours ?? Array.Empty<StateMachineBehaviour>()).Append(behaviour).ToArray();
        }

        /// <summary>1 つのプロパティを一定の値にするクリップ(キー 1 個)。</summary>
        public static AnimationClip ConstantClip(string name, string path, Type type, string property, float value)
        {
            var clip = new AnimationClip { name = name };
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, property),
                new AnimationCurve(new Keyframe(0f, value)));
            return clip;
        }

        private static AnimatorStateTransition NewTransition(AnimatorState to, AnimatorCondition[] conditions)
        {
            return new AnimatorStateTransition
            {
                destinationState = to,
                conditions = conditions ?? Array.Empty<AnimatorCondition>(),
                hasExitTime = false,
                exitTime = 0f,
                hasFixedDuration = true,
                duration = 0f,
                offset = 0f,
                canTransitionToSelf = false,
            };
        }

        /// <summary>レイヤー 1 つ分。<see cref="AnimatorFactory.AddLayer"/> で作る。</summary>
        public sealed class LayerBuilder
        {
            private readonly string _name;
            private readonly AvatarMask _mask;
            private readonly List<ChildAnimatorState> _states = new();
            private readonly List<AnimatorStateTransition> _anyStateTransitions = new();

            internal LayerBuilder(string name, AvatarMask mask)
            {
                _name = name;
                _mask = mask;
            }

            /// <summary>最初に足したステートが既定のステートになる(<see cref="DefaultState"/> で変えられる)。</summary>
            public AnimatorState DefaultState { get; set; }

            /// <summary>ステートを足す。Write Defaults は既定でオフ(MA の Match Avatar Write Defaults でアバターに合わせる)。</summary>
            public AnimatorState AddState(string name, Motion motion, bool writeDefaults = false)
            {
                var state = new AnimatorState
                {
                    name = name,
                    motion = motion,
                    writeDefaultValues = writeDefaults,
                };
                _states.Add(new ChildAnimatorState
                {
                    state = state,
                    position = new Vector3(300f, 80f * _states.Count, 0f),
                });
                DefaultState ??= state;
                return state;
            }

            /// <summary>Any State からの遷移を足す。canTransitionToSelf の既定は false。</summary>
            public AnimatorStateTransition AddAnyStateTransition(AnimatorState to, params AnimatorCondition[] conditions)
            {
                var transition = NewTransition(to, conditions);
                _anyStateTransitions.Add(transition);
                return transition;
            }

            internal AnimatorControllerLayer Commit()
            {
                var stateMachine = new AnimatorStateMachine
                {
                    name = _name,
                    hideFlags = HideFlags.HideInHierarchy,
                };
                stateMachine.states = _states.ToArray();
                stateMachine.anyStateTransitions = _anyStateTransitions.ToArray();
                // 既定のステートは最後に入れる(states を入れたときに上書きされるため)
                stateMachine.defaultState = DefaultState;

                return new AnimatorControllerLayer
                {
                    name = _name,
                    stateMachine = stateMachine,
                    avatarMask = _mask,
                    defaultWeight = 1f,
                    blendingMode = AnimatorLayerBlendingMode.Override,
                };
            }
        }
    }
}
