using UnityEngine;
using System.Collections.Generic;
public class spiderman : MonoBehaviour
{
    public Animator[] animators;

    [Header("FPS Settings")]
    public float idleFPS = 8f;
    public float moveFPS = 12f;
    public float fastFPS = 20f;

    [Header("Speed Thresholds")]
    public float idleThreshold = 0.1f;
    public float moveThreshold = 3f;

    private float accumulatedTime = 0f;
    private float steppedTime = 0f;

    void Start()
    {
        foreach (Animator anim in animators)
        {
            if (anim != null)
                anim.enabled = false;
        }
    }

    void Update()
    {
        if (animators == null || animators.Length == 0) return;

        accumulatedTime += Time.deltaTime;

        foreach (Animator anim in animators)
        {
            if (anim == null) continue;

            float fps = GetFPSFromVelocity(anim);
            float step = 1f / fps;

            float newSteppedTime = Mathf.Floor(accumulatedTime / step) * step;

            if (newSteppedTime > steppedTime)
            {
                float delta = newSteppedTime - steppedTime;

                // clamp for safety
                delta = Mathf.Min(delta, step * 2f);

                anim.Update(delta);
            }
        }

        // update global stepped time AFTER all animators
        float maxFPS = Mathf.Max(idleFPS, moveFPS, fastFPS);
        float globalStep = 1f / maxFPS;
        steppedTime = Mathf.Floor(accumulatedTime / globalStep) * globalStep;
    }

    float GetFPSFromVelocity(Animator anim)
    {
        Vector3 velocity = Vector3.zero;

        // Try Rigidbody
        Rigidbody rb = anim.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            velocity = rb.velocity;
        }
        else
        {
            // fallback: approximate using root motion delta
            velocity = anim.velocity;
        }

        float speed = velocity.magnitude;

        if (speed < idleThreshold) return idleFPS;
        if (speed < moveThreshold) return moveFPS;
        return fastFPS;
    }
}