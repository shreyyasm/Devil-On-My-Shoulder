using UnityEngine;
using DG.Tweening;

public class GateDoor : MonoBehaviour
{
    [Header("Door Leaves")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;

    [Header("Opening")]
    [SerializeField] private float openAngle = 100f;
    [SerializeField] private float overshoot = 20f;

    [Header("Animation")]
    [SerializeField] private float kickTime = 0.18f;
    [SerializeField] private float settleTime = 0.10f;
    [SerializeField] private float closeTime = 0.25f;
    [SerializeField] private float rightDoorDelay = 0.03f;

    [Header("Impact")]
    [SerializeField] private bool shakeFrame = true;
    [SerializeField] private float shakeDuration = 0.08f;
    [SerializeField] private float shakeStrength = 0.04f;
    [SerializeField] private int shakeVibrato = 20;

    [Header("Auto Close")]
    [SerializeField] private bool autoClose = true;
    [SerializeField] private float autoCloseDelay = 2f;

    public ParticleSystem doorVFX;

    private Quaternion leftClosedRotation;
    private Quaternion rightClosedRotation;

    private bool isOpen;
    public bool isOpened;
    private Tween autoCloseTween;

    AudioSource audioSource;
    private void Awake()
    {
        leftClosedRotation = leftDoor.localRotation;
        rightClosedRotation = rightDoor.localRotation;
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!isOpened)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                other.GetComponent<PlayerMovement>().TryKick();
                SFXManager.Instance.PlayNPC_SFX(audioSource, "Gate/Open", 0.7f);
                Open();
            }    
            else if (other.gameObject.layer == 16 && other.gameObject.GetComponent<EnemyManager>().kickOn)
            {
                SFXManager.Instance.PlayNPC_SFX(audioSource, "Gate/Open", 0.7f);
                Open();
            }

        }


    }
    private void OnCollisionEnter(Collision other)
    {
        if (isOpened)
            return;

        if (other.gameObject.CompareTag("Player"))
        {
            PlayerMovement player = other.gameObject.GetComponent<PlayerMovement>();

            if (player != null)
                player.TryKick();

            if (SFXManager.Instance != null && audioSource != null)
                SFXManager.Instance.PlayNPC_SFX(audioSource, "Gate/Open", 0.7f);

            Open();
        }
        else if (other.gameObject.CompareTag("Bullet") ||
                 other.gameObject.CompareTag("Blood"))
        {
            if (SFXManager.Instance != null && audioSource != null)
                SFXManager.Instance.PlayNPC_SFX(audioSource, "Gate/Open", 0.7f);

            Open();
        }
    }
    public void Open()
    {
        if (isOpen)
            return;

        isOpen = true;

        leftDoor.DOKill();
        rightDoor.DOKill();
        transform.DOKill();
        autoCloseTween?.Kill();

        if (shakeFrame)
        {
            transform.DOShakePosition(
                shakeDuration,
                shakeStrength,
                shakeVibrato,
                90,
                false,
                true);
        }

        // LEFT DOOR
        Sequence left = DOTween.Sequence();

        left.Append(
            leftDoor.DOLocalRotateQuaternion(
                leftClosedRotation * Quaternion.Euler(0, -(openAngle + overshoot), 0),
                kickTime)
            .SetEase(Ease.OutQuart));

        left.Append(
            leftDoor.DOLocalRotateQuaternion(
                leftClosedRotation * Quaternion.Euler(0, -openAngle, 0),
                settleTime)
            .SetEase(Ease.OutBounce));

        // RIGHT DOOR
        Sequence right = DOTween.Sequence();

        right.SetDelay(rightDoorDelay);

        right.Append(
            rightDoor.DOLocalRotateQuaternion(
                rightClosedRotation * Quaternion.Euler(0, openAngle + overshoot, 0),
                kickTime)
            .SetEase(Ease.OutQuart));

        right.Append(
            rightDoor.DOLocalRotateQuaternion(
                rightClosedRotation * Quaternion.Euler(0, openAngle, 0),
                settleTime)
            .SetEase(Ease.OutBounce));

        isOpened = true;
        doorVFX.Play();
        if (autoClose)
        {
            autoCloseTween = DOVirtual.DelayedCall(autoCloseDelay, Close);
        }
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        isOpened = false;

        autoCloseTween?.Kill();

        leftDoor.DOKill();
        rightDoor.DOKill();

        leftDoor.DOLocalRotateQuaternion(leftClosedRotation, closeTime)
            .SetEase(Ease.OutCubic);

        rightDoor.DOLocalRotateQuaternion(rightClosedRotation, closeTime)
            .SetEase(Ease.OutCubic);
    }

    public void Restart()
    {
        autoCloseTween?.Kill();

        leftDoor.DOKill();
        rightDoor.DOKill();
        transform.DOKill();

        isOpen = false;

        leftDoor.localRotation = leftClosedRotation;
        rightDoor.localRotation = rightClosedRotation;
    }

    public void RestartAndOpen()
    {
        Restart();
        Open();
    }
}