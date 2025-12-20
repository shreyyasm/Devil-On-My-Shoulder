// Copyright 2021, Infima Games. All Rights Reserved.

using QFSW.MOP2;
using System;
using System.Collections.Generic;
using UnityEngine;
using static CartoonFX.CFXR_Effect;

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
            LaserBeam,     
            ChargedBeam   
        }

        public enum SpecialGuns
        {
            Deafult,
            Threesome,
            Demo,
            Kinematic,
            Heatbloom,
            Riftsaw,
            RayGun,
            Piercer
        }
        #region FIELDS SERIALIZED

        [Header("Gun Type")]
        [SerializeField] private GunType gunType;

        [Header("SpecialGun")]
        [SerializeField] private SpecialGuns specialGun;

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
        [SerializeField] private float laserOverheatTime = 5f;
        [SerializeField] private float laserCooldownTime = 3f;
        [SerializeField] private LayerMask enemyLayer;
        private float laserUseTimer;
        private float laserCooldownTimer;
        private bool laserOverheated;



        [Header("Charged Beam Settings")]
        [SerializeField] private float minChargedImpulseMultiplier = 0.5f; 
        [SerializeField] private float maxChargedImpulseMultiplier = 2.0f;
        [SerializeField] private float maxChargeValue = 100f;
        [SerializeField] private float timeToReachFullCharge = 5f; // seconds (0 → 100)
        [SerializeField] private float autoFireDelayAtFullCharge = 1.0f;
        private float currentChargeValue;
        private float fullChargeTimer;
        private bool waitingForAutoFire;


        [Header("Weapon Vibration (Charge / Beam)")]
        [SerializeField] private float vibrationFrequency = 25f;
        [SerializeField] private float maxVibrationAmplitude = 0.015f;
        [SerializeField] private float vibrationRandomness = 1.0f;

        private Vector3 weaponInitialLocalPos;
        private float vibrationTime;



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

            weaponInitialLocalPos = transform.localPosition;

            if (beamRenderer != null)
                beamRenderer.enabled = false;
        }


        private void Update()
        {
            if (gunType == GunType.ChargedBeam)
            {
                if (isCharging)
                {
                    // Increase charge value based on charge speed
                    currentChargeValue += (maxChargeValue / timeToReachFullCharge) * Time.deltaTime;
                    currentChargeValue = Mathf.Clamp(currentChargeValue, 0f, maxChargeValue);
                    //Debug.Log(currentChargeValue);
                    // Start auto-fire wait when fully charged

                    // Weapon vibration scales with charge
                    float charge01 = currentChargeValue / maxChargeValue;
                    ApplyWeaponVibration(charge01);

                    if (currentChargeValue >= maxChargeValue && !waitingForAutoFire)
                    {
                        waitingForAutoFire = true;
                        fullChargeTimer = 0f;
                    }
                }

                // Auto-fire after delay at full charge
                if (waitingForAutoFire)
                {
                    fullChargeTimer += Time.deltaTime;
                    if (fullChargeTimer >= autoFireDelayAtFullCharge)
                    {
                       
                        FireChargedShotWithValue(currentChargeValue);
                        ResetChargedBeamState();
                    }
                }
            }

            if (gunType == GunType.LaserBeam && beamActive)
            {
                UpdateLaserBeam();

                // Weapon vibration scales with laser usage
                float laser01 = laserUseTimer / laserOverheatTime;
                laser01 = Mathf.Clamp01(laser01);
                ApplyWeaponVibration(laser01);
            }



            // Laser cooldown handling
            if (laserOverheated)
            {
                laserCooldownTimer += Time.deltaTime;
                if (laserCooldownTimer >= laserCooldownTime)
                {
                    laserOverheated = false;
                    laserCooldownTimer = 0f;
                    laserUseTimer = 0f;
                }
            }
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
            return gunType == GunType.Automatic;
        }

        public override float GetRateOfFire() => roundsPerMinutes;
        public override bool IsFull() => ammunitionCurrent == ammunitionTotal;
        public override bool isChargedBeam() => gunType == GunType.ChargedBeam;
        public override bool IsLaserBeam() => gunType == GunType.LaserBeam;
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

            Character.lastShotTime = Time.time;
           
            //Play firing animation.
            const string stateName = "Fire";
            Character.crosshairAnim.SetTrigger("Fire");
            Character.characterAnimator.CrossFade(stateName, 0.05f, Character.layerOverlay, 0);
            Character.cameraShake.shakeDuration += 0.15f;
            if (Character.playerMovement.mainMenu) return;
            ScoreManager.Instance.RegisterShotFired();

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

            Character.lastShotTime = Time.time;

            //Play firing animation.
            Character.crosshairAnim.SetTrigger("Fire");
            const string stateName = "Fire";
            Character.characterAnimator.CrossFade(stateName, 0.05f, Character.layerOverlay, 0);
            Character.cameraShake.shakeDuration += 0.15f;
            if (Character.playerMovement.mainMenu) return;
            ScoreManager.Instance.RegisterShotFired();

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
            // 🔹 ADD THIS GUARD
            if (isCharging)
                return;

            isCharging = true;
            currentChargeValue = 0f;
            waitingForAutoFire = false;
            fullChargeTimer = 0f;
        }



        private void FireChargedProjectile(float chargePercent)
        {
            audioSource.PlayOneShot(audioClipFire);

            Character.lastShotTime = Time.time;
            
            //Play firing animation.
            const string stateName = "Fire";
            Character.crosshairAnim.SetTrigger("Fire");
            Character.characterAnimator.CrossFade(stateName, 0.05f, Character.layerOverlay, 0);
            Character.cameraShake.shakeDuration += 0.15f;
            if (Character.playerMovement.mainMenu) return;
            ScoreManager.Instance.RegisterShotFired();

            Quaternion rotation = Quaternion.LookRotation(playerCamera.forward);
            GameObject projectile = BulletPool.GetObject(socket.position, rotation);

            // 🔹 Constant speed, no charge scaling
            projectile.GetComponent<Rigidbody>().velocity =
                projectile.transform.forward * projectileImpulse;

            flash.SetTrigger("Flash");
            ApplyRecoil(chargedBeamRecoil);
        }

        public override void ReleaseChargedShot()
        {
            if (!isCharging)
                return;

            FireChargedShotWithValue(currentChargeValue);
            ResetChargedBeamState();
        }
        private void ResetChargedBeamState()
        {
            isCharging = false;
            waitingForAutoFire = false;
            currentChargeValue = 0f;
            fullChargeTimer = 0f;
            ResetWeaponVibration();

        }

        // This is how you use that charged damage
        //projectile.GetComponent<Bullet>().SetDamage(chargeValue);
        private void FireChargedShotWithValue(float chargeValue)
        {
            Debug.Log($"Charged Beam Fired with Value: {chargeValue}");

            // Convert value → percent if you want impulse scaling
            float chargePercent = chargeValue / maxChargeValue;

            FireChargedProjectile(chargePercent);
        }

        private void StartLaserBeam()
        {
            if (laserOverheated)
                return;

            beamActive = true;
            laserTickTimer = 0f;

            if (beamRenderer)
                beamRenderer.enabled = true;
        }


        private void UpdateLaserBeam()
        {
            // Track continuous usage
            laserUseTimer += Time.deltaTime;
            if (laserUseTimer >= laserOverheatTime)
            {
                OverheatLaser();
                return;
            }

            Vector3 origin = socket.position;
            Vector3 direction = playerCamera.forward;

            beamRenderer.SetPosition(0, origin);

            RaycastHit hit;
            bool hasHit = Physics.Raycast(
                origin,
                direction,
                out hit,
                maximumDistance
            );

            Vector3 endPoint = hasHit
                ? hit.point
                : origin + direction * maximumDistance;

            beamRenderer.SetPosition(1, endPoint);

            // Damage ticking
            laserTickTimer += Time.deltaTime;
            if (laserTickTimer < laserDamageInterval)
                return;

            laserTickTimer = 0f;

            if (hasHit && ((1 << hit.collider.gameObject.layer) & enemyLayer) != 0)
            {
               
                Debug.Log("EnemyHit");
                hit.collider.gameObject.GetComponent<Enemy>().EnemyHit(10);
                hit.collider.SendMessage(
                    "TakeDamage",
                    laserDamagePerTick,
                    SendMessageOptions.DontRequireReceiver
                );
            }
        }
        private void OverheatLaser()
        {
            laserOverheated = true;
            beamActive = false;
            laserUseTimer = 0f;
            ResetWeaponVibration();

            if (beamRenderer)
                beamRenderer.enabled = false;

            //Play firing animation.
            const string stateName = "Fire";
            Character.crosshairAnim.SetTrigger("Fire");
            Character.characterAnimator.CrossFade(stateName, 0.05f, Character.layerOverlay, 0);
            Character.cameraShake.shakeDuration += 0.15f;
        }

        public override void StopBeam()
        {
            if(!beamActive) { return; }

            beamActive = false;
            isCharging = false;
            ResetWeaponVibration();

            // Reset laser usage if player stops firing manually
            if (gunType == GunType.LaserBeam && !laserOverheated)
                laserUseTimer = 0f;

            if (beamRenderer)
                beamRenderer.enabled = false;

            //Play firing animation.
            const string stateName = "Fire";
            Character.crosshairAnim.SetTrigger("Fire");
            Character.characterAnimator.CrossFade(stateName, 0.05f, Character.layerOverlay, 0);
            Character.cameraShake.shakeDuration += 0.15f;

        }

        private void ApplyWeaponVibration(float intensity01)
        {
            vibrationTime += Time.deltaTime;

            float amplitude = maxVibrationAmplitude * intensity01;

            // Smooth random offsets using Perlin noise
            float noiseX = Mathf.PerlinNoise(vibrationTime * vibrationFrequency, 0f) - 0.5f;
            float noiseY = Mathf.PerlinNoise(0f, vibrationTime * vibrationFrequency) - 0.5f;

            Vector3 offset = new Vector3(
                noiseX * amplitude * vibrationRandomness,
                noiseY * amplitude,
                0f
            );

            transform.localPosition = weaponInitialLocalPos + offset;
        }

        private void ResetWeaponVibration()
        {
            vibrationTime = 0f;
            transform.localPosition = weaponInitialLocalPos;
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
