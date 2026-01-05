using System;
using GorillaLocomotion;
using UnityEngine;

namespace EIOP.Tools;

public class EIOPUtils : MonoBehaviour
{
    public static Action<VRRig> OnPlayerCosmeticsLoaded;
    public static Action<VRRig> OnPlayerRigCached;

    public static Transform RealRightController;
    public static Transform RealLeftController;

    private void Start()
    {
        RealRightController = new GameObject("RealRightController").transform;
        RealLeftController  = new GameObject("RealLeftController").transform;
    }

    private void LateUpdate()
    {
        RealRightController.position =
                GTPlayer.Instance.rightHand.controllerTransform.TransformPoint(GTPlayer.Instance.rightHand.handOffset);

        RealLeftController.position =
                GTPlayer.Instance.leftHand.controllerTransform.TransformPoint(GTPlayer.Instance.leftHand.handOffset);

        RealRightController.rotation =
                GTPlayer.Instance.rightHand.controllerTransform.rotation * GTPlayer.Instance.rightHand.handRotOffset;

        RealLeftController.rotation =
                GTPlayer.Instance.leftHand.controllerTransform.rotation * GTPlayer.Instance.leftHand.handRotOffset;
    }
}