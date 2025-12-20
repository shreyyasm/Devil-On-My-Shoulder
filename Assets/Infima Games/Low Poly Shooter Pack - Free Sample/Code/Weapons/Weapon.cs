// Copyright 2021, Infima Games. All Rights Reserved.

using QFSW.MOP2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    public class Weapon : WeaponBehaviour
    {
        [Serializable]
        public class PlayerCharacterHands
        {
            public int characterIndex;
            public GameObject weaponHands;
            public GameObject healthHands;
        }

        // ===================== GUN TYPE =====================
        public enum GunType
        {
            Pistol,
            Automatic,
            Shotgun,
            LaserBeam,     // NEW (continuous damage beam)
            ChargedBeam   // EXISTING (kept 100% intact)
        }


        #region FIELDS SERIALIZED

        [Header("Gun Type")]
        [SerializeField] private GunType gunType;

        [Header("Firing")]
        [SerializeField] private float projectileImpulse = 400.0f;
        [SerializeField] private int roundsPerMinutes = 200;
        [SerializeField] private LayerMask mask;
        [SerializeField] private float maximumDistance = 500.0f;
        [SerializeField] private int ammunitionTotal = 10;
        [SerializeField] private Transform socket;
        [SerializeField] private AudioClip audioClipFire;

        [Header("Shotgun Settings")]
        [SerializeField] private int shotgunPellets = 8;
        [SerializeField] private float shotgunSpread = 6f;

        [Header("Laser Beam Settings")]
        [SerializeField] private float laserDamageInterval = 0.1f;
        [SerializeField] private int laserDamagePerTick = 5;
        [SerializeField] private LineRenderer beamRenderer;

        [Header("Charged Beam Settings")]
        [SerializeField] private float chargeTime = 1.5f;
        [SerializeField] private bool fireProjectileOnChargeRelease = true;
        [SerializeField] private float minChargedImpulseMultiplier = 0.5f;
        [SerializeField] private float maxChargedImpulseMultiplier = 2.0f;


        [Header("Animation")]
        [SerializeField] private Transform socketEjection;

        [Header("Resources")]
        [SerializeField] private GameObject prefabCasing;
        [SerializeField] private GameObject prefabProjectile;
        [SerializeField] public RuntimeAnimatorController controller;
        [SerializeField] private Sprite spriteBody;

        [Header("Audio Clips Holster")]
        [SerializeField] private AudioClip audioClipHolster;
        [SerializeField] private AudioClip audioClipUnholster;

        [Header("Audio Clips Reloads")]
        [SerializeField] private AudioClip audioClipReload;
        [SerializeField] private AudioClip audioClipReloadEmpty;

        [Header("Audio Clips Other")]
        [SerializeField] private AudioClip audioClipFireEmpty;

        [Header("Recoil Settings")]
        [SerializeField] private Vector2 pistolRecoil = new Vector2(1.5f, 0.6f);
        [SerializeField] private Vector2 automaticRecoil = new Vector2(1.0f, 0.35f);
        [SerializeField] private Vector2 shotgunRecoil = new Vector2(4.0f, 1.8f);
        [SerializeField] private Vector2 chargedBeamRecoil = new Vector2(0.5f, 0.25f);
        [SerializeField] private float recoilReturnSpeed = 10f;
        [SerializeField] private float recoilSnappiness = 15f;

        public List<PlayerCharacterHands> playerCharacterHands;

        #endregion

        #region FIELDS

        private Animator animator;
        private WeaponAttachmentManagerBehaviour attachmentManager;
        private int ammunitionCurrent;
        private IGameModeService gameModeService;
        private CharacterBehaviour characterBehaviour;
        private Transform playerCamera;

        public AudioSource audioSource;
        public ObjectPool BulletPool;
        public Animator flash;

        private bool isCharging;
        private bool beamActive;
        private float chargeTimer;

        public Character Character;

        // Recoil runtime
        private Vector2 currentRecoil;
        private Vector2 targetRecoil;
        private float laserTickTimer;


        #endregion

        #region UNITY

        protected override void Awake()
        {
            animator = GetComponent<Animator>();
            attachmentManager = GetComponent<WeaponAttachmentManagerBehaviour>();

            gameModeService = ServiceLocator.Current.Get<IGameModeService>();
            characterBehaviour = gameModeService.GetPlayerCharacter();
            playerCamera = characterBehaviour.GetCameraWorld().transform;
            Character = characterBehaviour.GetComponent<Character>();
        }

        protected override void Start()
        {
            SetCharacterHands(PlayerCharacterData.Instance.characterIndex);
            ammunitionCurrent = ammunitionTotal;

            if (beamRenderer != null)
                beamRenderer.enabled = false;
        }

        private void Update()
        {
            if (gunType == GunType.ChargedBeam)
            {
                if (isCharging)
                {
                    chargeTimer += Time.deltaTime;
                    if (chargeTimer >= chargeTime)
                        ActivateBeam();
                }

                if (beamActive)
                    UpdateBeam();
            }
            // NEW LaserBeam behavior
            if (gunType == GunType.LaserBeam && beamActive)
                UpdateLaserBeam();
        }

        private void LateUpdate()
        {
            // Smooth recoil return
            targetRecoil = Vector2.Lerp(
                targetRecoil,
                Vector2.zero,
                recoilReturnSpeed * Time.deltaTime
            );

            currentRecoil = Vector2.Lerp(
                currentRecoil,
                targetRecoil,
                recoilSnappiness * Time.deltaTime
            );

            playerCamera.localRotation =
                Quaternion.Euler(-currentRecoil.y, currentRecoil.x, 0f);
        }

        #endregion

        #region CHARACTER HANDS

        public void SetCharacterHands(int index)
        {
            foreach (var hands in playerCharacterHands)
            {
                bool active = hands.characterIndex == index;
                if (hands.weaponHands) hands.weaponHands.SetActive(active);
                if (hands.healthHands) hands.healthHands.SetActive(active);
            }
        }

        #endregion

        #region GETTERS

        public override Animator GetAnimator() => animator;
        public override Sprite GetSpriteBody() => spriteBody;
        public override AudioClip GetAudioClipHolster() => audioClipHolster;
        public override AudioClip GetAudioClipUnholster() => audioClipUnholster;
        public override AudioClip GetAudioClipReload() => audioClipReload;
        public override AudioClip GetAudioClipReloadEmpty() => audioClipReloadEmpty;
        public override AudioClip GetAudioClipFireEmpty() => audioClipFireEmpty;
        public override AudioClip GetAudioClipFire() => audioClipFire;
        public override int GetAmmunitionCurrent() => ammunitionCurrent;
        public override int GetAmmunitionTotal() => ammunitionTotal;

        public override bool IsAutomatic()
        {
            return gunType == GunType.Automatic || gunType == GunType.ChargedBeam;
        }

        public override float GetRateOfFire() => roundsPerMinutes;
        public override bool IsFull() => ammunitionCurrent == ammunitionTotal;
        public override bool HasAmmunition() => ammunitionCurrent > 0;
        public override RuntimeAnimatorController GetAnimatorController() => controller;

        #endregion

        #region CORE METHODS

        public override void Reload()
        {
            animator.Play(HasAmmunition() ? "Reload" : "Reload Empty", 0, 0.0f);
        }

        public override void Fire(float spreadMultiplier = 1.0f)
        {
            switch (gunType)
            {
                case GunType.Pistol:
                case GunType.Automatic:
                    FireSingle();
                    break;

                case GunType.Shotgun:
                    FireShotgun();
                    break;

                case GunType.ChargedBeam:
                    StartCharge(); // EXISTING
                    break;

                case GunType.LaserBeam:
                    StartLaserBeam(); // NEW
                    break;
            }
        }


        private void FireSingle()
        {
            if (!HasAmmunition()) return;

            audioSource.PlayOneShot(audioClipFire);
            animator.Play("Fire", 0, 0.0f);
            ammunitionCurrent--;

            Quaternion rotation = Quaternion.LookRotation(playerCamera.forward);
            GameObject projectile = BulletPool.GetObject(socket.position, rotation);
            projectile.GetComponent<Rigidbody>().velocity = projectile.transform.forward * projectileImpulse;

            flash.SetTrigger("Flash");

            ApplyRecoil(gunType == GunType.Pistol ? pistolRecoil : automaticRecoil);

            if (ammunitionCurrent == 0)
                Character.PlayReloadAnimation();
        }

        private void FireShotgun()
        {
            if (!HasAmmunition()) return;

            audioSource.PlayOneShot(audioClipFire);
            animator.Play("Fire", 0, 0.0f);
            ammunitionCurrent--;

            for (int i = 0; i < shotgunPellets; i++)
            {
                Vector3 dir = playerCamera.forward +
                              UnityEngine.Random.insideUnitSphere * shotgunSpread * 0.01f;

                GameObject pellet = BulletPool.GetObject(socket.position, Quaternion.LookRotation(dir));
                pellet.GetComponent<Rigidbody>().velocity = pellet.transform.forward * projectileImpulse;
            }

            flash.SetTrigger("Flash");
            ApplyRecoil(shotgunRecoil);
        }

        #endregion

        #region CHARGED BEAM

        private void StartCharge()
        {
            isCharging = true;
            chargeTimer = 0f;
        }
        private void FireChargedProjectile(float chargePercent)
        {
            audioSource.PlayOneShot(audioClipFire);

            Quaternion rotation = Quaternion.LookRotation(playerCamera.forward);
            GameObject projectile = BulletPool.GetObject(socket.position, rotation);

            float impulse = projectileImpulse *
                Mathf.Lerp(minChargedImpulseMultiplier, maxChargedImpulseMultiplier, chargePercent);

            projectile.GetComponent<Rigidbody>().velocity =
                projectile.transform.forward * impulse;

            flash.SetTrigger("Flash");
            ApplyRecoil(chargedBeamRecoil);
        }

        public override void ReleaseChargedShot()
        {
            if (!fireProjectileOnChargeRelease || !isCharging)
                return;

            isCharging = false;

            float chargePercent = Mathf.Clamp01(chargeTimer / chargeTime);
            chargeTimer = 0f;

            FireChargedProjectile(chargePercent);
        }
        private void ActivateBeam() 
        { 
            isCharging = false; beamActive = true; 
            if (beamRenderer) beamRenderer.enabled = true; 
            ApplyRecoil(chargedBeamRecoil); 
        }
        private void UpdateBeam() 
        { 
            beamRenderer.SetPosition(0, socket.position); 
            beamRenderer.SetPosition(1, socket.position + playerCamera.forward * maximumDistance); 
        }

        private void StartLaserBeam()
        {
            beamActive = true;
            laserTickTimer = 0f;

            if (beamRenderer)
                beamRenderer.enabled = true;
        }

        private void UpdateLaserBeam()
        {
            beamRenderer.SetPosition(0, socket.position);
            beamRenderer.SetPosition(1, socket.position + playerCamera.forward * maximumDistance);

            laserTickTimer += Time.deltaTime;
            if (laserTickTimer < laserDamageInterval)
                return;

            laserTickTimer = 0f;

            if (Physics.Raycast(
                playerCamera.position,
                playerCamera.forward,
                out RaycastHit hit,
                maximumDistance,
                mask))
            {
                hit.collider.SendMessage(
                    "TakeDamage",
                    laserDamagePerTick,
                    SendMessageOptions.DontRequireReceiver
                );
            }
        }

        public override void StopBeam()
        {
            beamActive = false;
            isCharging = false;

            if (beamRenderer)
                beamRenderer.enabled = false;
        }


        #endregion

        #region RECOIL

        private void ApplyRecoil(Vector2 recoil)
        {
            targetRecoil += new Vector2(
                UnityEngine.Random.Range(-recoil.x, recoil.x),
                recoil.y
            );
        }

        #endregion

        #region AMMO

        public override void FillAmmunition(int amount)
        {
            ammunitionCurrent = amount != 0
                ? Mathf.Clamp(ammunitionCurrent + amount, 0, ammunitionTotal)
                : ammunitionTotal;
        }

        #endregion
    }
}
