using System;
using UnityEngine;

namespace EIOP.Core;

public class EIOPButton : MonoBehaviour
{
    private const  float  DebounceTime = 0.2f;
    private static float  lastPressTime;
    public         Action OnPress;

    private void Awake() => gameObject.SetLayer(UnityLayer.GorillaInteractable);

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - lastPressTime < DebounceTime)
            return;

        if (other.GetComponentInParent<GorillaTriggerColliderHandIndicator>() is not null)
        {
            GorillaTriggerColliderHandIndicator gtchi =
                    other.GetComponentInParent<GorillaTriggerColliderHandIndicator>();

            if (!gtchi.isLeftHand)
            {
                lastPressTime = Time.time;
                OnPress?.Invoke();
                GorillaTagger.Instance.StartVibration(false, 0.3f, 0.15f);
                Plugin.PlaySound(Plugin.ButtonPressSound);
            }
        }
    }
}