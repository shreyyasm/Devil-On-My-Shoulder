using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace InfimaGames.LowPolyShooterPack
{
    public class CrosshairManager : MonoBehaviour
    {
        public static CrosshairManager instance;
        // ============================================================
        // CROSSHAIR / WEAPON ENTRY
        // ============================================================

        [Serializable]
        public class CrosshairWeapon
        {
            [Header("Weapon")]
            public Weapon weapon;

            [Header("Crosshair")]
            public Sprite crosshair;

            [Header("Scale")]
            [Min(0f)]
            public float scale = 1f;
        }


        // ============================================================
        // REFERENCES
        // ============================================================

        [Header("References")]
        [SerializeField] private Image crosshairImage;
        [SerializeField] private RectTransform crosshairTransform;


        // ============================================================
        // WEAPON CROSSHAIRS
        // ============================================================

        [Header("Weapon Crosshairs")]
        [SerializeField]
        private List<CrosshairWeapon> weapons =
            new List<CrosshairWeapon>();


        // ============================================================
        // HIT FEEDBACK
        // ============================================================

        [Header("Hit Feedback")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hitColor = Color.red;

        [Tooltip("Fixed amount added to the current crosshair scale during a hit.")]
        [SerializeField] private float popAmount = 0.2f;

        [Tooltip("Duration of each half of the hit pop.")]
        [SerializeField] private float popDuration = 0.08f;


        // ============================================================
        // RELOAD FEEDBACK
        // ============================================================

        [Header("Reload Feedback")]
        [Tooltip("How long the 360 degree reload rotation takes.")]
        [SerializeField] private float reloadRotationDuration = 0.25f;


        // ============================================================
        // RUNTIME
        // ============================================================

        private CrosshairWeapon currentEntry;

        private Tween popTween;
        private Tween reloadTween;

        /// <summary>Whether the crosshair image is currently drawn.</summary>
        public bool IsCrosshairVisible => crosshairImage != null && crosshairImage.enabled;

        /// <summary>Shows or hides only the crosshair image, leaving its manager active.</summary>
        public void SetCrosshairVisible(bool visible)
        {
            if (crosshairImage == null)
                return;

            if (!visible)
            {
                popTween?.Kill();
                reloadTween?.Kill();
            }

            crosshairImage.enabled = visible;
        }


        // ============================================================
        // UNITY
        // ============================================================

        private void Awake()
        {
            instance = this;
            if (crosshairImage == null)
            {
                Debug.LogError(
                    "CrosshairManager: Crosshair Image is not assigned."
                );

                return;
            }

            if (crosshairTransform == null)
                crosshairTransform = crosshairImage.rectTransform;

            crosshairImage.color = normalColor;
        }


        private void Update()
        {
            CheckActiveWeapon();
        }


        // ============================================================
        // ACTIVE WEAPON DETECTION
        // ============================================================

        private void CheckActiveWeapon()
        {
            foreach (CrosshairWeapon entry in weapons)
            {
                if (entry == null || entry.weapon == null)
                    continue;

                if (entry.weapon.gameObject.activeInHierarchy)
                {
                    if (currentEntry != entry)
                    {
                        currentEntry = entry;
                        SetCrosshair(entry);
                    }

                    return;
                }
            }
        }


        // ============================================================
        // SET CROSSHAIR
        // ============================================================

        private void SetCrosshair(CrosshairWeapon entry)
        {
            if (entry.crosshair == null)
            {
                Debug.LogWarning(
                    $"CrosshairManager: No crosshair assigned for {entry.weapon.name}."
                );

                return;
            }

            // Stop any active animations when switching weapons.
            popTween?.Kill();
            reloadTween?.Kill();

            // Set sprite.
            crosshairImage.sprite = entry.crosshair;

            // Reset color.
            crosshairImage.color = normalColor;

            // Apply weapon-specific scale.
            crosshairTransform.localScale =
                Vector3.one * entry.scale;

            // Reset rotation.
            crosshairTransform.localRotation =
                Quaternion.identity;
        }


        // ============================================================
        // CROSSHAIR HIT
        // ============================================================

        public void CrosshairHit()
        {
            if (crosshairImage == null)
                return;

            if (crosshairTransform == null)
                return;

            if (currentEntry == null)
                return;

            // Stop previous hit animation.
            popTween?.Kill();

            // --------------------------------------------------------
            // BASE SCALE
            // --------------------------------------------------------

            Vector3 baseScale =
                Vector3.one * currentEntry.scale;

            // Always start from the weapon's assigned scale.
            crosshairTransform.localScale = baseScale;

            // --------------------------------------------------------
            // HIT COLOR
            // --------------------------------------------------------

            crosshairImage.color = hitColor;

            // --------------------------------------------------------
            // POP SCALE
            // --------------------------------------------------------

            // FIXED additive amount.
            //
            // 0.8 -> 1.0 -> 0.8
            // 1.0 -> 1.2 -> 1.0
            // 1.5 -> 1.7 -> 1.5

            Vector3 popScale =
                baseScale + Vector3.one * popAmount;

            // --------------------------------------------------------
            // DOTWEEN POP
            // --------------------------------------------------------

            popTween = DOTween.Sequence()

                .Append(
                    crosshairTransform
                        .DOScale(
                            popScale,
                            popDuration
                        )
                        .SetEase(Ease.OutQuad)
                )

                .Append(
                    crosshairTransform
                        .DOScale(
                            baseScale,
                            popDuration
                        )
                        .SetEase(Ease.InQuad)
                )

                .OnComplete(() =>
                {
                    crosshairImage.color = normalColor;
                });
        }


        // ============================================================
        // RELOAD CROSSHAIR
        // ============================================================

        public void ReloadCrosshair()
        {
            if (crosshairTransform == null)
                return;

            // Stop any previous reload animation.
            reloadTween?.Kill();

            // --------------------------------------------------------
            // STORE EXACT STARTING ROTATION
            // --------------------------------------------------------

            Vector3 startingRotation =
                crosshairTransform.localEulerAngles;

            // --------------------------------------------------------
            // 360 DEGREE ROTATION
            // --------------------------------------------------------

            Vector3 targetRotation =
                startingRotation + new Vector3(0f, 0f, 360f);

            reloadTween = crosshairTransform
                .DORotate(
                    targetRotation,
                    reloadRotationDuration,
                    RotateMode.FastBeyond360
                )
                .SetEase(Ease.OutCubic)

                // Automatically reset to the exact starting
                // rotation when the 360 degree animation finishes.
                .OnComplete(() =>
                {
                    crosshairTransform.localEulerAngles =
                        startingRotation;
                });
        }
    }
}
