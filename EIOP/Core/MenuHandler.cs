using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EIOP.Tab_Handlers;
using EIOP.Tools;
using TMPro;
using UnityEngine;

namespace EIOP.Core;

public class MenuHandler : MonoBehaviour
{
    public static readonly Vector3 TargetMenuScale = Vector3.one * 15f;

    public static readonly Vector3    BaseMenuPosition = new(0.25f, 0f, 0.05f);
    public static readonly Quaternion BaseMenuRotation = Quaternion.Euler(300f, 0f, 180f);

    public bool IsMenuOpen;

    public GameObject Menu;

    private bool wasPressed;

    private void Start()
    {
        GameObject menuPrefab = Plugin.EIOPBundle.LoadAsset<GameObject>("Menu");
        Menu = Instantiate(menuPrefab, EIOPUtils.RealLeftController);
        Destroy(menuPrefab);
        Menu.name = "Menu";

        Menu.transform.localPosition = BaseMenuPosition;
        Menu.transform.localRotation = BaseMenuRotation;
        Menu.transform.localScale    = Vector3.zero;

        Plugin.MainColour      = Menu.GetComponent<Renderer>().material.color;
        Plugin.SecondaryColour = Menu.transform.Find("ModePanel").GetComponent<Renderer>().material.color;
        Menu.SetActive(false);

        PerformShaderManagement(Menu);
        SetUpTabs();

        gameObject.AddComponent<PCHandler>().MenuHandlerInstance = this;
    }

    private void Update()
    {
        bool isPressed = ControllerInputPoller.instance.leftControllerSecondaryButton;

        if (isPressed && !wasPressed)
        {
            IsMenuOpen = !IsMenuOpen;
            CoroutineManager.Instance.StartCoroutine(IsMenuOpen ? OpenMenu() : CloseMenu());
        }

        wasPressed = isPressed;
    }

    private void SetUpTabs()
    {
        List<Type> tabHandlerTypes = Assembly.GetExecutingAssembly().GetTypes()
                                             .Where(t => !t.IsAbstract && t.IsClass &&
                                                         typeof(TabHandlerBase).IsAssignableFrom(t)).ToList();

        List<Transform> tabViews = Menu.GetComponentsInChildren<Transform>(true)
                                       .Where(t => t.gameObject.name.EndsWith("View",
                                                      StringComparison.OrdinalIgnoreCase)).ToList();

        Transform modePanel = Menu.transform.Find("ModePanel");
        List<Transform> tabButtons = modePanel.GetComponentsInChildren<Transform>(true)
                                              .Where(t => t.gameObject.name.EndsWith("Button",
                                                             StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (Type tabHandlerType in tabHandlerTypes)
        {
            string tabName = tabHandlerType.Name.Replace("Handler", "");
            Transform tabView =
                    tabViews.FirstOrDefault(t => t.gameObject.name.Equals(tabName + "View",
                                                    StringComparison.OrdinalIgnoreCase));

            Transform tabButton =
                    tabButtons.FirstOrDefault(t => t.gameObject.name.Equals(tabName + "Button",
                                                      StringComparison.OrdinalIgnoreCase));

            if (tabView == null || tabButton == null)
            {
                Debug.LogWarning($"[MenuHandler] Could not find View or Button for tab: {tabName}");

                continue;
            }

            EIOPButton button = tabButton.gameObject.AddComponent<EIOPButton>();
            button.OnPress = () =>
                             {
                                 foreach (Transform tab in tabViews)
                                     tab.gameObject.SetActive(false);

                                 tabView.gameObject.SetActive(true);
                             };

            tabView.gameObject.AddComponent(tabHandlerType);
            tabView.gameObject.SetActive(false);
        }
    }

    public static void PerformShaderManagement(GameObject obj)
    {
        foreach (Transform child in obj.transform)
            PerformShaderManagement(child.gameObject);

        if (obj.TryGetComponent(out Renderer renderer))
            if (renderer.material.shader.name.Contains("Universal"))
            {
                renderer.material.shader = Plugin.UberShader;
                if (renderer.material.mainTexture != null) renderer.material.EnableKeyword("_USE_TEXTURE");
            }

        if (obj.TryGetComponent(out TextMeshPro tmp))
            tmp.fontMaterial = new Material(tmp.fontMaterial)
            {
                    shader = Shader.Find("TextMeshPro/Mobile/Distance Field"),
            };

        if (obj.TryGetComponent(out TextMeshProUGUI tmpUGUI))
            tmpUGUI.fontMaterial = new Material(tmpUGUI.fontMaterial)
            {
                    shader = Shader.Find("TextMeshPro/Mobile/Distance Field"),
            };
    }

    public IEnumerator OpenMenu()
    {
        Menu.SetActive(true);
        Menu.transform.localScale = Vector3.zero;
        float startTime = Time.time;

        while (Time.time - startTime < 0.1f)
        {
            float t = (Time.time - startTime) / 0.1f;
            Menu.transform.localScale = Vector3.Lerp(Vector3.zero, TargetMenuScale, t);

            yield return null;
        }

        Menu.transform.localScale = TargetMenuScale;
    }

    public IEnumerator CloseMenu()
    {
        Menu.transform.localScale = TargetMenuScale;
        float startTime = Time.time;

        while (Time.time - startTime < 0.1f)
        {
            float t = (Time.time - startTime) / 0.1f;
            Menu.transform.localScale = Vector3.Lerp(TargetMenuScale, Vector3.zero, t);

            yield return null;
        }

        Menu.transform.localScale = Vector3.zero;
        Menu.SetActive(false);
    }
}