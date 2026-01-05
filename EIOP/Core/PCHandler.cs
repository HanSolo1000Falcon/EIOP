using System.Collections;
using BepInEx;
using EIOP.Tools;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EIOP.Core;

public class PCHandler : MonoBehaviour
{
    public static Transform   ThirdPersonCameraTransform;
    public static Camera      ThirdPersonCamera;
    public        MenuHandler MenuHandlerInstance;

    private void Update()
    {
        if (UnityInput.Current.GetKeyDown(KeyCode.J))
        {
            MenuHandlerInstance.IsMenuOpen = !MenuHandlerInstance.IsMenuOpen;
            if (MenuHandlerInstance.IsMenuOpen)
            {
                MenuHandlerInstance.Menu.transform.SetParent(ThirdPersonCameraTransform, false);
                MenuHandlerInstance.Menu.transform.localPosition = new Vector3(0f, 0f, 0.6f);
                MenuHandlerInstance.Menu.transform.localRotation = Quaternion.Euler(270f, 180f, 0f);
            }

            CoroutineManager.Instance.StartCoroutine(MenuHandlerInstance.IsMenuOpen
                                                             ? MenuHandlerInstance.OpenMenu()
                                                             : CloseMenu());
        }

        if (MenuHandlerInstance.IsMenuOpen && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = ThirdPersonCamera.ScreenPointToRay(UnityInput.Current.mousePosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, 2f, UnityLayerMask.GorillaInteractable.GetIndex()))
                return;

            if (hit.collider.TryGetComponent(out EIOPButton button))
                button.OnPress?.Invoke();
        }
    }

    private IEnumerator CloseMenu()
    {
        yield return MenuHandlerInstance.CloseMenu();
        MenuHandlerInstance.Menu.transform.SetParent(EIOPUtils.RealLeftController, false);
        MenuHandlerInstance.Menu.transform.localPosition = MenuHandler.BaseMenuPosition;
        MenuHandlerInstance.Menu.transform.localRotation = MenuHandler.BaseMenuRotation;
    }
}