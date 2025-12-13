// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Camera Look. Handles the rotation of the camera.
    /// </summary>
    public class CameraLook : MonoBehaviour
    {
        #region FIELDS SERIALIZED

        [Header("Settings")]

        [Tooltip("Sensitivity when looking around.")]
        [SerializeField]
        private Vector2 sensitivity = new Vector2(1, 1);

        [Tooltip("Minimum and maximum up/down rotation angle the camera can have.")]
        [SerializeField]
        private Vector2 yClamp = new Vector2(-60, 60);

        [Tooltip("Should the look rotation be interpolated?")]
        [SerializeField]
        private bool smooth;

        [Tooltip("The speed at which the look rotation is interpolated.")]
        [SerializeField]
        private float interpolationSpeed = 25.0f;

        // ----- Aim assist tuning -----
        [Header("Aim Assist (Gamepad Only)")]
        [Tooltip("Enable/disable aim assist (still only applied when a gamepad is connected).")]
        [SerializeField]
        private bool aimAssistEnabled = true;

        [Tooltip("Maximum world-space distance to consider enemies for aim assist.")]
        [SerializeField]
        private float assistRange = 40f;

        [Tooltip("Maximum angle (degrees) from the camera forward to consider for assist.")]
        [SerializeField, Range(1f, 45f)]
        private float assistMaxAngle = 12f;

        [Tooltip("Maximum strength of the assist (0 = off, 1 = full lock). The code scales this down so it never overpowers player input.")]
        [SerializeField, Range(0f, 1f)]
        private float assistStrength = 0.25f;

        [Tooltip("Layer name used for enemies. Must match your Project settings.")]
        [SerializeField]
        private string enemyLayerName = "Enemy";

        #endregion

        #region FIELDS

        private CharacterBehaviour playerCharacter;
        private Rigidbody playerCharacterRigidbody;

        private Quaternion rotationCharacter;
        private Quaternion rotationCamera;

        [Header("Debug Info")]
        [Tooltip("True if any gamepad/joystick is connected.")]
        public bool gamepadConnected;

        private int enemyLayerMask;

        #endregion

        #region UNITY

        private void Awake()
        {
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
            playerCharacterRigidbody = playerCharacter.GetComponent<Rigidbody>();
            sensitivity.x = SettingsManager.MouseSensitivity;
            sensitivity.y = SettingsManager.MouseSensitivity;

            enemyLayerMask = LayerMask.GetMask(enemyLayerName);
        }

        private void Start()
        {
            rotationCharacter = playerCharacter.transform.localRotation;
            rotationCamera = transform.localRotation;
        }

        public PlayerMovement playerMovement;

        private void LateUpdate()
        {
            gamepadConnected = IsGamepadConnected();

            sensitivity.x = SettingsManager.MouseSensitivity;
            sensitivity.y = SettingsManager.MouseSensitivity;

            if (gamepadConnected)
            {
                sensitivity.y *= 1.5f;
                sensitivity.x *= 2f;
            }
            if (playerMovement.mainMenu) return;

            Vector2 rawLookInput = playerCharacter.IsCursorLocked() ? playerCharacter.GetInputLook() : default;
            Vector2 frameInput = rawLookInput * sensitivity;

            Quaternion rotationYaw = Quaternion.Euler(0.0f, frameInput.x, 0.0f);
            Quaternion rotationPitch = Quaternion.Euler(-frameInput.y, 0.0f, 0.0f);

            rotationCamera *= rotationPitch;
            rotationCharacter *= rotationYaw;

            rotationCamera = Clamp(rotationCamera);

            // ---- AIM ASSIST ----
            if (gamepadConnected && aimAssistEnabled)
            {
                float rawInputMag = rawLookInput.magnitude;
                const float minInputToAssist = 0.05f;
                if (rawInputMag > minInputToAssist)
                {
                    ApplyAimAssist(ref rotationCamera, rawLookInput);
                    rotationCamera = Clamp(rotationCamera);
                }
            }

            Quaternion localRotation = transform.localRotation;

            if (smooth)
            {
                localRotation = Quaternion.Slerp(localRotation, rotationCamera, Time.deltaTime * interpolationSpeed);
                localRotation = Clamp(localRotation);

                playerCharacterRigidbody.MoveRotation(Quaternion.Slerp(
                    playerCharacterRigidbody.rotation, rotationCharacter, Time.deltaTime * interpolationSpeed));
            }
            else
            {
                localRotation *= rotationPitch;
                localRotation = Clamp(localRotation);
                playerCharacterRigidbody.MoveRotation(playerCharacterRigidbody.rotation * rotationYaw);
            }

            transform.localRotation = localRotation;
        }

        #endregion

        #region FUNCTIONS

        private Quaternion Clamp(Quaternion rotation)
        {
            rotation.x /= rotation.w;
            rotation.y /= rotation.w;
            rotation.z /= rotation.w;
            rotation.w = 1.0f;

            float pitch = 2.0f * Mathf.Rad2Deg * Mathf.Atan(rotation.x);
            pitch = Mathf.Clamp(pitch, yClamp.x, yClamp.y);
            rotation.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * pitch);
            return rotation;
        }

        private bool IsGamepadConnected()
        {
            foreach (string name in Input.GetJoystickNames())
            {
                if (!string.IsNullOrEmpty(name))
                    return true;
            }
            return false;
        }

        private void ApplyAimAssist(ref Quaternion rotationCameraLocal, Vector2 rawLookInput)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 camPos = cam.transform.position;
            Vector3 camForward = cam.transform.forward;

            // Search only within assistRange
            Collider[] hits = Physics.OverlapSphere(camPos, assistRange, enemyLayerMask);
            if (hits == null || hits.Length == 0) return;

            Transform best = null;
            float bestAngle = assistMaxAngle;
            float bestDistSqr = float.MaxValue;

            foreach (var c in hits)
            {
                if (c == null) continue;

                Vector3 toEnemy = c.transform.position - camPos;
                float dist = toEnemy.magnitude;

                // 🔹 Skip if enemy is out of range
                if (dist > assistRange)
                    continue;

                float angle = Vector3.Angle(camForward, toEnemy);
                if (angle <= assistMaxAngle)
                {
                    // Prefer smaller angle, then closer distance
                    if (angle < bestAngle || (Mathf.Approximately(angle, bestAngle) && dist * dist < bestDistSqr))
                    {
                        best = c.transform;
                        bestAngle = angle;
                        bestDistSqr = dist * dist;
                    }
                }
            }

            if (best == null) return;

            Vector3 dirToEnemy = (best.position - camPos).normalized;
            Quaternion desiredWorldRot = Quaternion.LookRotation(dirToEnemy, Vector3.up);
            Quaternion desiredLocal = Quaternion.Inverse(playerCharacter.transform.rotation) * desiredWorldRot;

            float inputMag = Mathf.Clamp01(rawLookInput.magnitude);
            float assistFactor = assistStrength * (1f - inputMag);
            if (assistFactor <= Mathf.Epsilon) return;

            float frameAssist = Mathf.Clamp01(assistFactor) * 0.5f;
            rotationCameraLocal = Quaternion.Slerp(rotationCameraLocal, desiredLocal, frameAssist * Time.deltaTime * 60f);
        }

        #endregion
    }
}
