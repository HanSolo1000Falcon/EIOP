using System;
using GorillaLocomotion;
using UnityEngine;

namespace EIOP.Tools;

public class EIOPUtils : MonoBehaviour
{
    public static event Action<VRRig> OnPlayerCosmeticsLoaded;
    public static event Action<VRRig> OnPlayerRigCached;

    private static EIOPUtils instance;

    public static Transform RealRightController { get; private set; }
    public static Transform RealLeftController { get; private set; }

    private Transform rightControllerCache;
    private Transform leftControllerCache;
    private GTHand rightHandCache;
    private GTHand leftHandCache;

    private bool isInitialized;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        InitializeControllers();
    }

    private void InitializeControllers()
    {
        RealRightController = CreateControllerTransform("EIOP Right Controller");
        RealLeftController = CreateControllerTransform("EIOP Left Controller");

        CachePlayerReferences();
    }

    private Transform CreateControllerTransform(string name)
    {
        var controller = new GameObject(name).transform;
        controller.SetParent(transform);
        return controller;
    }

    private void CachePlayerReferences()
    {
        if (GTPlayer.Instance == null)
            return;

        rightHandCache = GTPlayer.Instance.rightHand;
        leftHandCache = GTPlayer.Instance.leftHand;

        if (rightHandCache != null)
            rightControllerCache = rightHandCache.controllerTransform;

        if (leftHandCache != null)
            leftControllerCache = leftHandCache.controllerTransform;

        isInitialized = rightControllerCache != null && leftControllerCache != null;
    }

    private void LateUpdate()
    {
        if (!isInitialized)
        {
            CachePlayerReferences();
            if (!isInitialized)
                return;
        }

        UpdateControllerTransforms();
    }

    private void UpdateControllerTransforms()
    {
        UpdateControllerTransform(
            RealRightController,
            rightControllerCache,
            rightHandCache.handOffset,
            rightHandCache.handRotOffset
        );

        UpdateControllerTransform(
            RealLeftController,
            leftControllerCache,
            leftHandCache.handOffset,
            leftHandCache.handRotOffset
        );
    }

    private void UpdateControllerTransform(
        Transform target,
        Transform source,
        Vector3 positionOffset,
        Quaternion rotationOffset)
    {
        if (target == null || source == null)
            return;

        target.position = source.TransformPoint(positionOffset);
        target.rotation = source.rotation * rotationOffset;
    }

    public static void InvokePlayerCosmeticsLoaded(VRRig rig)
    {
        if (rig == null)
            return;

        OnPlayerCosmeticsLoaded?.Invoke(rig);
    }

    public static void InvokePlayerRigCached(VRRig rig)
    {
        if (rig == null)
            return;

        OnPlayerRigCached?.Invoke(rig);
    }

    public static bool AreControllersReady()
    {
        return RealRightController != null && RealLeftController != null;
    }

    public static Vector3 GetControllerVelocity(bool isRightHand)
    {
        if (GTPlayer.Instance == null)
            return Vector3.zero;

        var hand = isRightHand ? GTPlayer.Instance.rightHand : GTPlayer.Instance.leftHand;
        return hand?.controllerTransform != null 
            ? hand.controllerTransform.GetComponent<Rigidbody>()?.velocity ?? Vector3.zero 
            : Vector3.zero;
    }

    public static Vector3 GetControllerAngularVelocity(bool isRightHand)
    {
        if (GTPlayer.Instance == null)
            return Vector3.zero;

        var hand = isRightHand ? GTPlayer.Instance.rightHand : GTPlayer.Instance.leftHand;
        return hand?.controllerTransform != null 
            ? hand.controllerTransform.GetComponent<Rigidbody>()?.angularVelocity ?? Vector3.zero 
            : Vector3.zero;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            
            if (RealRightController != null)
                Destroy(RealRightController.gameObject);
            
            if (RealLeftController != null)
                Destroy(RealLeftController.gameObject);
        }
    }
}
