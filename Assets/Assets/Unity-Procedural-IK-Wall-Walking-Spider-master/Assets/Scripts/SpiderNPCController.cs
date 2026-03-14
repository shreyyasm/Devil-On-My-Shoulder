/* 
 * This file is part of Unity-Procedural-IK-Wall-Walking-Spider on github.com/PhilS94
 * Copyright (C) 2020 Philipp Schofield - All Rights Reserved
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This class needs a reference to the Spider class and calls the walk and turn functions depending on a random perlin generated input.
 * In essence, this class translates random input to spider movement.
 * 
 * Perlin Noise is used to generate a random yet smooth direction.
 * A random binarized speed parameter is used to mimic the "stop and go" nature of spiders.
 */

[DefaultExecutionOrder(-1)] // Make sure the input movement is applied before the spider itself will do a ground check and possibly add gravity
public class SpiderNPCController : MonoBehaviour {

    [Header("Target Movement")]
    public Transform targetTransform;
    public bool followTarget = false;
    public float stopDistance = 0.2f;

    [Header("Unstuck Jump")]
    public float stuckDistanceThreshold = 0.0001f;
    public float jumpForce = 0.5f;
    public float jumpForwardForce = 0.4f;

    private Rigidbody spiderRb;


    [Header("Debug")]
    public bool showDebug;

    [Header("Spider Reference")]
    public Spider spider;

    private float perlinDirectionStep = 0.07f;
    private float perlinSpeedStep = 0.5f;
    private float startValue;

    private Vector3 Z;
    private Vector3 X;
    private Vector3 Y;

    private void Awake() {
        Random.InitState(System.DateTime.Now.Millisecond);
        startValue = Random.value;

        //Initialize Coordinate System
        Z = transform.forward;
        X = transform.right;
        Y = transform.up;
        if (targetTransform != null)
            FollowTarget(targetTransform);
        spiderRb = spider.GetComponent<Rigidbody>();
        lastPosition = spider.transform.position;
    }

    private void FixedUpdate()
    {
        updateCoordinateSystem();

        Vector3 input;

        if (followTarget && targetTransform != null)
        {
            input = getDirectionToTarget();
        }
        else
        {
            input = getRandomDirection() * getRandomBinaryValue(0, 1, 0.4f);
        }

        spider.walk(input);
        spider.turn(input);
        CheckStuckJump();
        AssistWallExitRotation();

        if (showDebug)
            Debug.DrawLine(spider.transform.position,
            spider.transform.position + input * 0.2f * spider.getScale(),
            Color.cyan, Time.fixedDeltaTime);
    }

    private void Update() {
        if (showDebug) {
            Debug.DrawLine(spider.transform.position, spider.transform.position + X * 0.1f * spider.getScale(), Color.red);
            Debug.DrawLine(spider.transform.position, spider.transform.position + Z * 0.1f * spider.getScale(), Color.blue);
        }
    }
    public void FollowTarget(Transform target)
    {
        targetTransform = target;
        followTarget = true;
    }
    [Header("Wall Exit Assist")]
    public float wallExitAssistAngle = -20f;
    public float wallExitAssistSpeed = 6f;
    private Vector3 getDirectionToTarget()
    {
        if (targetTransform == null)
            return spider.transform.forward;

        Vector3 toTarget = targetTransform.position - spider.transform.position;

       
        if (toTarget.magnitude <= stopDistance)
        {
            return Vector3.zero;
        }

        Vector3 playerPos = targetTransform.position;
        Vector3 spiderPos = spider.transform.position;

        playerPos.y = spiderPos.y;

        float distance = Vector3.Distance(playerPos, spiderPos);
        reached = distance < stopDistance;

        if (distance < stopDistance)
        {
            reached = true;
        }
        else
        {
            reached = false;
        }
        // direction toward player projected on spider surface
        Vector3 input = Vector3.ProjectOnPlane(toTarget, spider.transform.up).normalized;

        // if input becomes weak (edge case), keep moving forward
        if (input.sqrMagnitude < 0.01f)
        {
            input = spider.transform.forward;
        }

        // make sure movement aligns with spider forward
        if (Vector3.Dot(input, spider.transform.forward) < 0.3f)
        {
            input = Vector3.Slerp(spider.transform.forward, input, 0.5f);
        }

        return input.normalized;
    }
    private void AssistWallExitRotation()
    {
        if (targetTransform == null) return;

        Vector3 toTarget = targetTransform.position - spider.transform.position;

        // check if player is below spider
        float belowCheck = Vector3.Dot(toTarget.normalized, -spider.getGroundNormal());

        if (belowCheck > 0.3f)
        {
            Vector3 euler = spider.transform.localEulerAngles;

            // convert to signed angle
            if (euler.x > 180) euler.x -= 360;

            float newX = Mathf.Lerp(euler.x, wallExitAssistAngle, Time.fixedDeltaTime * wallExitAssistSpeed);

            spider.transform.localRotation = Quaternion.Euler(newX, euler.y, euler.z);
        }
    }
    public float jumpCooldown = 1.2f;

    private float lastJumpTime;
    private Vector3 lastPosition;
    private float stuckTimer;
    public float stuckTime = 0.5f;

    public bool reached;
    private void CheckStuckJump()
    {
        if (!followTarget) return;

        if(reached) return;

        Vector3 pos = spider.transform.position;

        // measure forward progress
        float forwardProgress = Vector3.Dot(
            pos - lastPosition,
            spider.transform.forward
        );

        if (forwardProgress < 0.0001f)
        {
            stuckTimer += Time.fixedDeltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }

        // check cooldown
        if (stuckTimer > stuckTime && Time.time - lastJumpTime > jumpCooldown)
        {
            Vector3 jumpDir =
                spider.transform.forward * jumpForwardForce +
                spider.transform.up * jumpForce;

            spiderRb.AddForce(jumpDir, ForceMode.Impulse);

            lastJumpTime = Time.time;
            stuckTimer = 0f;

            Debug.Log("Spider unstuck jump");
        }

        lastPosition = pos;
    }
    private void updateCoordinateSystem() {
        Vector3 newY = spider.getGroundNormal();
        Quaternion fromTo = Quaternion.FromToRotation(Y, newY);
        X = Vector3.ProjectOnPlane(fromTo * X, newY).normalized;
        Z = Vector3.ProjectOnPlane(fromTo * Z, newY).normalized;
        Y = newY;
    }

    private Vector3 getRandomDirection() {
        //Get random values between [-1,1] using perlin noise
        float vertical = 2.0f * (Mathf.PerlinNoise(Time.time * perlinDirectionStep, startValue) - 0.5f);
        float horizontal = 2.0f * (Mathf.PerlinNoise(Time.time * perlinDirectionStep, startValue + 0.3f) - 0.5f);
        return (X * horizontal + Z * vertical).normalized;
    }

    // Threshold is between 0 and 1 and applies a threshold filter to the perlin noise. Min is the lower value and Max the higher value.
    private float getRandomBinaryValue(float min, float max, float threshold) {
        float value = Mathf.PerlinNoise(Time.time * perlinSpeedStep, startValue + 0.6f);
        if (value >= threshold) value = 1;
        else value = 0;
        return min + value * (max - min);// Range [min,max]
    }
}
