﻿using System.Collections.Generic;
using Hai.ComboGesture.Scripts.Components;
using Hai.ComboGesture.Scripts.Editor.Internal.Reused;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;


namespace Hai.ComboGesture.Scripts.Editor.Internal
{
    internal class LayerForExpressionsView
    {
        private readonly FeatureToggles _featuresToggles;
        private readonly AnimatorGenerator _animatorGenerator;
        private readonly AvatarMask _expressionsAvatarMask;
        private readonly AnimationClip _emptyClip;
        private readonly string _activityStageName;
        private readonly ConflictPrevention _conflictPrevention;
        private readonly AssetContainer _assetContainer;
        private readonly ConflictFxLayerMode _compilerConflictFxLayerMode;
        private readonly AnimationClip _compilerIgnoreParamList;
        private readonly AnimationClip _compilerFallbackParamList;
        private readonly List<CurveKey> _blinkBlendshapes;
        private readonly AnimatorController _animatorController;
        private readonly List<GestureComboStageMapper> _comboLayers;
        private readonly bool _useGestureWeightCorrection;
        private readonly List<ManifestBinding> _manifestBindings;

        public LayerForExpressionsView(FeatureToggles featuresToggles,
            AnimatorGenerator animatorGenerator,
            AvatarMask expressionsAvatarMask,
            AnimationClip emptyClip,
            string activityStageName,
            ConflictPrevention conflictPrevention,
            AssetContainer assetContainer,
            ConflictFxLayerMode compilerConflictFxLayerMode,
            AnimationClip compilerIgnoreParamList,
            AnimationClip compilerFallbackParamList,
            List<CurveKey> blinkBlendshapes,
            AnimatorController animatorController,
            List<GestureComboStageMapper> comboLayers,
            bool useGestureWeightCorrection,
            List<ManifestBinding> manifestBindings)
        {
            _featuresToggles = featuresToggles;
            _animatorGenerator = animatorGenerator;
            _expressionsAvatarMask = expressionsAvatarMask;
            _emptyClip = emptyClip;
            _activityStageName = activityStageName;
            _conflictPrevention = conflictPrevention;
            _assetContainer = assetContainer;
            _compilerConflictFxLayerMode = compilerConflictFxLayerMode;
            _compilerIgnoreParamList = compilerIgnoreParamList;
            _compilerFallbackParamList = compilerFallbackParamList;
            _blinkBlendshapes = blinkBlendshapes;
            _animatorController = animatorController;
            _comboLayers = comboLayers;
            _useGestureWeightCorrection = useGestureWeightCorrection;
            _manifestBindings = manifestBindings;
        }

        public void Create()
        {
            EditorUtility.DisplayProgressBar("GestureCombo", "Clearing expressions layer", 0f);
            var machine = ReinitializeLayer();

            var defaultState = machine.AddState("Default", SharedLayerUtils.GridPosition(-1, -1));
            defaultState.motion = _emptyClip;
            if (Feature(FeatureToggles.ExposeDisableExpressions))
            {
                CreateTransitionWhenExpressionsAreDisabled(machine, defaultState);
            }

            if (_activityStageName != null)
            {
                CreateTransitionWhenActivityIsOutOfBounds(machine, defaultState);
            }

            var activityManifests = _manifestBindings;
            if (_conflictPrevention.ShouldGenerateAnimations)
            {
                EditorUtility.DisplayProgressBar("GestureCombo", "Generating animations", 0f);
                _assetContainer.RemoveAssetsStartingWith("zAutogeneratedExp_", typeof(AnimationClip));
                _assetContainer.RemoveAssetsStartingWith("zAutogeneratedPup_", typeof(BlendTree));
                activityManifests = new AnimationNeutralizer(
                    activityManifests,
                    _compilerConflictFxLayerMode,
                    _compilerIgnoreParamList,
                    _compilerFallbackParamList,
                    _blinkBlendshapes,
                    _assetContainer
                ).NeutralizeManifestAnimations();
            }

            var combinator = new IntermediateCombinator(activityManifests);

            new GestureCExpressionCombiner(
                _animatorController,
                machine,
                combinator.IntermediateToTransition,
                _activityStageName,
                _conflictPrevention.ShouldWriteDefaults,
                _useGestureWeightCorrection
            ).Populate();
        }

        private static void CreateTransitionWhenExpressionsAreDisabled(AnimatorStateMachine machine, AnimatorState defaultState)
        {
            var transition = machine.AddAnyStateTransition(defaultState);
            SharedLayerUtils.SetupDefaultTransition(transition);
            transition.AddCondition(AnimatorConditionMode.NotEqual, 0, SharedLayerUtils.HaiGestureComboDisableExpressionsParamName);
        }

        private void CreateTransitionWhenActivityIsOutOfBounds(AnimatorStateMachine machine, AnimatorState defaultState)
        {
            var transition = machine.AddAnyStateTransition(defaultState);
            SharedLayerUtils.SetupDefaultTransition(transition);

            foreach (var layer in _comboLayers)
            {
                transition.AddCondition(AnimatorConditionMode.NotEqual, layer.stageValue, _activityStageName);
            }
        }

        private AnimatorStateMachine ReinitializeLayer()
        {
            return _animatorGenerator.CreateOrRemakeLayerAtSameIndex("Hai_GestureExp", 1f, _expressionsAvatarMask).ExposeMachine();
        }

        private bool Feature(FeatureToggles feature)
        {
            return (_featuresToggles & feature) == feature;
        }
    }
}
