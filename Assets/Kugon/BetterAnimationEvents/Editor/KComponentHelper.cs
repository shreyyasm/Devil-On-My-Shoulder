using System;
using Kugon.BetterAnimationEvents.Editor;
using UnityEngine;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    public class KComponentHelper
    {
        public enum ComponentType
        {
            None,
            AnimatorController,
            Animation,
            AnimationClip,
        }

        private ComponentType selectedComponent = ComponentType.None;

        public ComponentType SelectedComponent => selectedComponent;


        private GameObject currentGameObject;
        private Animator currentAnimator;
        private Animation currentAnimation;
        
        public Transform SelectedTransform => currentGameObject.transform;

        public bool TrySelectComponent(KAnimationEventEditor editor, GameObject selection)
        {
            Animator foundAnimator = selection.GetComponentInParent<Animator>();

            if (foundAnimator != null)
            {
                currentAnimator = foundAnimator;
                selectedComponent = ComponentType.AnimatorController;
            }

            currentGameObject = selection.gameObject;

            return true;
        }

        public bool CanUseFunctions()
        {
            switch (selectedComponent)
            {
                case ComponentType.None:
                    return false;
                case ComponentType.AnimatorController:
                    return true;
                case ComponentType.Animation:
                    return true;
                case ComponentType.AnimationClip:
                    return false;
            }

            return false;
        }

        public AnimationClip[] GetAnimationClips()
        {
            switch (selectedComponent)
            {
                case ComponentType.None:
                    return new AnimationClip[0];
                case ComponentType.AnimatorController:
                    return currentAnimator.runtimeAnimatorController.animationClips;
                case ComponentType.Animation:
                    return null;
                case ComponentType.AnimationClip:
                    return null;
            }
            
            return null;
        }

    }
    
}