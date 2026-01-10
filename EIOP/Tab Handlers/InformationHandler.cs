using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EIOP.Core;
using EIOP.Patches;
using EIOP.Tools;
using GorillaExtensions;
using GorillaLocomotion;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace EIOP.Tab_Handlers;

public class InformationHandler : TabHandlerBase
{
    public static readonly Dictionary<string, DateTime> AccountCreationDates = new();

    private TextMeshPro accountCreationDateInfo;
    private Transform   actionsPanel;
    private VRRig       chosenRig;
    private TextMeshPro colourCodeInfo;
    private TextMeshPro fpsInfo;
    private Transform   infoPanel;
    private TextMeshPro installedMods;
    private Vector3     lastPos;

    private float        lastUpdate;
    private LineRenderer line;
    private Transform    modsPanel;
    private TextMeshPro  pingInfo;
    private TextMeshPro  platformInfo;

    private Transform[] playerButtons;
    private GameObject  playerHighlighter;
    private TextMeshPro playerNameInfo;
    private TextMeshPro playerNameMods;

    private int         playersPageIndex;
    private Transform   playersPanel;
    private TextMeshPro velocityInfo;

    private void Start()
    {
        infoPanel    = transform.Find("InformationTab");
        modsPanel    = transform.Find("ModsTab");
        actionsPanel = transform.Find("ActionsTab");
        playersPanel = transform.Find("PlayersTab");

        infoPanel.gameObject.SetActive(true);
        modsPanel.gameObject.SetActive(false);
        actionsPanel.gameObject.SetActive(false);
        playersPanel.gameObject.SetActive(false);

        playerButtons = playersPanel.Children().Take(4).ToArray();

        playersPanel.GetChild(4).gameObject.AddComponent<EIOPButton>().OnPress = () =>
        {
            playersPageIndex++;
            UpdatePlayerButtons();
        };

        playersPanel.GetChild(5).gameObject.AddComponent<EIOPButton>().OnPress = () =>
        {
            playersPageIndex--;
            UpdatePlayerButtons();
        };

        transform.Find("InformationTabButton").AddComponent<EIOPButton>().OnPress = () =>
        {
            infoPanel.gameObject.SetActive(true);
            modsPanel.gameObject.SetActive(false);
            actionsPanel.gameObject.SetActive(false);
            playersPanel.gameObject.SetActive(false);
        };

        transform.Find("ModsTabButton").AddComponent<EIOPButton>().OnPress = () =>
                                                                             {
                                                                                 infoPanel.gameObject.SetActive(false);
                                                                                 modsPanel.gameObject.SetActive(true);
                                                                                 actionsPanel.gameObject.SetActive(
                                                                                         false);

                                                                                 playersPanel.gameObject.SetActive(
                                                                                         false);
                                                                             };

        transform.Find("ActionsTabButton").AddComponent<EIOPButton>().OnPress = () =>
        {
            if (chosenRig == null)
                return;

            infoPanel.gameObject.SetActive(false);
            modsPanel.gameObject.SetActive(false);
            actionsPanel.gameObject.SetActive(true);
            playersPanel.gameObject.SetActive(false);
            actionsPanel.GetChild(4).GetComponentInChildren<TextMeshPro>().text =
                    VoicePrioritizationPatch.PrioritizedPeople.Contains(chosenRig)
                            ? "Unprioritize"
                            : "Prioritize";
        };

        transform.Find("PlayersTabButton").AddComponent<EIOPButton>().OnPress = () =>
        {
            if (Extensions.PlayersWithCosmetics.Count == 0)
                return;

            infoPanel.gameObject.SetActive(false);
            modsPanel.gameObject.SetActive(false);
            actionsPanel.gameObject.SetActive(false);
            playersPanel.gameObject.SetActive(true);

            UpdatePlayerButtons();
        };

        playerNameInfo          = infoPanel.GetChild(0).GetComponent<TextMeshPro>();
        platformInfo            = infoPanel.GetChild(1).GetComponent<TextMeshPro>();
        fpsInfo                 = infoPanel.GetChild(2).GetComponent<TextMeshPro>();
        pingInfo                = infoPanel.GetChild(3).GetComponent<TextMeshPro>();
        colourCodeInfo          = infoPanel.GetChild(4).GetComponent<TextMeshPro>();
        velocityInfo            = infoPanel.GetChild(5).GetComponent<TextMeshPro>();
        accountCreationDateInfo = infoPanel.GetChild(6).GetComponent<TextMeshPro>();

        playerNameMods = modsPanel.GetChild(0).GetComponent<TextMeshPro>();
        installedMods  = modsPanel.GetChild(1).GetComponent<TextMeshPro>();

        NoPlayerSelected();
        SetUpActionsPanel();

        Color mainColourWithWeirdAlpha = new(Plugin.MainColour.r, Plugin.MainColour.g, Plugin.MainColour.b, 0.7f);

        line                = new GameObject("Line").AddComponent<LineRenderer>();
        line.material       = MakeMaterialTransparent(line.material);
        line.material.color = mainColourWithWeirdAlpha;
        line.startWidth     = 0.0125f;
        line.endWidth       = 0.0125f;
        line.positionCount  = 2;
        line.enabled        = true;

        playerHighlighter = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(playerHighlighter.GetComponent<Collider>());
        playerHighlighter.GetComponent<Renderer>().material =
                MakeMaterialTransparent(playerHighlighter.GetComponent<Renderer>().material);

        playerHighlighter.GetComponent<Renderer>().material.color = mainColourWithWeirdAlpha;

        playerHighlighter.transform.localScale = Vector3.one * 0.5f;
    }

    private void LateUpdate()
    {
        if (chosenRig == null)
        {
            Vector3 fakeOrigin = XRSettings.isDeviceActive
                                         ? EIOPUtils.RealRightController.position
                                         : GTPlayer.Instance.bodyCollider.transform.position;

            Vector3 origin = XRSettings.isDeviceActive
                                     ? EIOPUtils.RealRightController.position
                                     : PCHandler.ThirdPersonCamera.ScreenPointToRay(Mouse.current.position.ReadValue())
                                                .origin;

            Vector3 direction = XRSettings.isDeviceActive
                                        ? EIOPUtils.RealRightController.forward
                                        : PCHandler.ThirdPersonCamera
                                                   .ScreenPointToRay(Mouse.current.position.ReadValue()).direction;

            if (PhysicsRaycast(origin,      direction,
                        out RaycastHit hit, out VRRig rig))
            {
                line.enabled = true;
                line.SetPosition(0, fakeOrigin);
                line.SetPosition(1, hit.point);

                if (rig != null)
                {
                    HighlightPlayer(rig);
                    if (ControllerInputPoller.instance.rightControllerIndexFloat > 0.5f ||
                        Mouse.current.leftButton.wasPressedThisFrame)
                    {
                        if (!rig.HasCosmetics())
                            return;

                        OnNewRig(rig);
                        chosenRig = rig;
                    }
                }
                else
                {
                    HighlightPlayer(null);
                }
            }
            else
            {
                line.enabled = false;
                HighlightPlayer(null);
            }
        }
        else
        {
            line.enabled = false;
            int    fps    = chosenRig.fps;
            string colour = fps < 60 ? "red" : fps < 72 ? "yellow" : "green";
            fpsInfo.text        = $"<color={colour}>{fps}</color> FPS";
            colourCodeInfo.text = ParseIntoColourCode(chosenRig.playerColor);

            if (lastUpdate + 0.1f < Time.time)
            {
                string pingColour = chosenRig.GetPing() > 100 ? chosenRig.GetPing() > 250 ? "red" : "orange" : "green";
                pingInfo.text = $"<color={pingColour}>{chosenRig.GetPing()}</color> ms";

                Vector3 playerSpeed = (chosenRig.transform.position - lastPos) / (Time.time - lastUpdate);
                lastPos = chosenRig.transform.position;
                string speedColour = playerSpeed.magnitude < 10f
                                             ? playerSpeed.magnitude < 6.5f ? "green" : "orange"
                                             : "red";

                velocityInfo.text = $"<color={speedColour}>{playerSpeed.magnitude:F1}</color> m/s";

                lastUpdate = Time.time;
            }

            if (ControllerInputPoller.instance.rightControllerSecondaryButton)
            {
                chosenRig = null;
                HighlightPlayer(null);
                NoPlayerSelected();
            }
        }
    }

    private void OnEnable()
    {
        if (line != null)
            line.enabled = true;

        chosenRig = null;
        HighlightPlayer(null);
        NoPlayerSelected();
    }

    private void OnDisable()
    {
        if (line != null)
            line.enabled = false;

        chosenRig = null;
        HighlightPlayer(null);
        NoPlayerSelected();
    }

    private void UpdatePlayerButtons()
    {
        int amountPages = Extensions.PlayersWithCosmetics.Count == 0
                                  ? 0
                                  : Mathf.CeilToInt(Extensions.PlayersWithCosmetics.Count / 4f);

        playersPageIndex = (playersPageIndex + amountPages) % amountPages;
        VRRig[] currentRigs = Extensions.PlayersWithCosmetics.Skip(playersPageIndex * 4).Take(4).ToArray();

        foreach (Transform playerButton in playerButtons)
            playerButton.gameObject.SetActive(false);

        for (int i = 0; i < currentRigs.Length; i++)
        {
            VRRig     rig          = currentRigs[i];
            Transform playerButton = playerButtons[i];
            playerButton.gameObject.SetActive(true);
            playerButton.GetComponentInChildren<TextMeshPro>().text = rig.OwningNetPlayer.SanitizedNickName;
            playerButton.gameObject.GetOrAddComponent<EIOPButton>().OnPress = () =>
                                                                              {
                                                                                  OnNewRig(rig);
                                                                                  chosenRig = rig;
                                                                                  HighlightPlayer(rig);
                                                                              };
        }
    }

    private void SetUpActionsPanel()
    {
        actionsPanel.GetChild(0).AddComponent<EIOPButton>().OnPress = () =>
                                                                      {
                                                                          GorillaPlayerScoreboardLine scoreboardLine =
                                                                                  GorillaScoreboardTotalUpdater
                                                                                         .allScoreboardLines
                                                                                         .FirstOrDefault(line =>
                                                                                                  line.linePlayer ==
                                                                                                  chosenRig
                                                                                                         .OwningNetPlayer);

                                                                          if (scoreboardLine == null)
                                                                              return;

                                                                          scoreboardLine.PressButton(true,
                                                                                  GorillaPlayerLineButton.ButtonType
                                                                                         .Cheating);
                                                                      };

        actionsPanel.GetChild(1).AddComponent<EIOPButton>().OnPress = () =>
                                                                      {
                                                                          GorillaPlayerScoreboardLine scoreboardLine =
                                                                                  GorillaScoreboardTotalUpdater
                                                                                         .allScoreboardLines
                                                                                         .FirstOrDefault(line =>
                                                                                                  line.linePlayer ==
                                                                                                  chosenRig
                                                                                                         .OwningNetPlayer);

                                                                          if (scoreboardLine == null)
                                                                              return;

                                                                          scoreboardLine.PressButton(true,
                                                                                  GorillaPlayerLineButton.ButtonType
                                                                                         .Toxicity);
                                                                      };

        actionsPanel.GetChild(2).AddComponent<EIOPButton>().OnPress = () =>
                                                                      {
                                                                          GorillaPlayerScoreboardLine scoreboardLine =
                                                                                  GorillaScoreboardTotalUpdater
                                                                                         .allScoreboardLines
                                                                                         .FirstOrDefault(line =>
                                                                                                  line.linePlayer ==
                                                                                                  chosenRig
                                                                                                         .OwningNetPlayer);

                                                                          if (scoreboardLine == null)
                                                                              return;

                                                                          scoreboardLine.PressButton(true,
                                                                                  GorillaPlayerLineButton.ButtonType
                                                                                         .HateSpeech);
                                                                      };

        actionsPanel.GetChild(3).AddComponent<EIOPButton>().OnPress = () =>
                                                                      {
                                                                          GorillaPlayerScoreboardLine scoreboardLine =
                                                                                  GorillaScoreboardTotalUpdater
                                                                                         .allScoreboardLines
                                                                                         .FirstOrDefault(line =>
                                                                                                  line.linePlayer ==
                                                                                                  chosenRig
                                                                                                         .OwningNetPlayer);

                                                                          if (scoreboardLine == null)
                                                                              return;

                                                                          scoreboardLine.muteButton.isOn =
                                                                                  !scoreboardLine.muteButton.isOn;

                                                                          scoreboardLine.PressButton(
                                                                                  scoreboardLine.muteButton.isOn,
                                                                                  GorillaPlayerLineButton.ButtonType
                                                                                         .Mute);

                                                                          actionsPanel.GetChild(3)
                                                                                 .GetComponentInChildren<TextMeshPro>()
                                                                                 .text = scoreboardLine.muteButton.isOn
                                                                                  ? "Unmute"
                                                                                  : "Mute";
                                                                      };

        actionsPanel.GetChild(4).AddComponent<EIOPButton>().OnPress = () =>
                                                                      {
                                                                          if (VoicePrioritizationPatch.PrioritizedPeople
                                                                             .Contains(chosenRig))
                                                                              VoicePrioritizationPatch.PrioritizedPeople
                                                                                     .Remove(chosenRig);
                                                                          else
                                                                              VoicePrioritizationPatch.PrioritizedPeople
                                                                                     .Add(chosenRig);

                                                                          actionsPanel.GetChild(4)
                                                                                 .GetComponentInChildren<TextMeshPro>()
                                                                                 .text = VoicePrioritizationPatch
                                                                                 .PrioritizedPeople
                                                                                 .Contains(chosenRig)
                                                                                  ? "Unprioritize"
                                                                                  : "Prioritize";
                                                                      };
    }

    private void OnNewRig(VRRig rig)
    {
        playerNameInfo.text = rig.OwningNetPlayer.SanitizedNickName;
        playerNameMods.text = rig.OwningNetPlayer.SanitizedNickName;
        GetAccountCreationDate(rig);
        platformInfo.text = rig.GetPlatform().ParsePlatform();
        DoCheatChecking(rig);
    }

    private async void GetAccountCreationDate(VRRig rig)
    {
        if (AccountCreationDates.ContainsKey(rig.OwningNetPlayer.UserId))
        {
            accountCreationDateInfo.text = AccountCreationDates[rig.OwningNetPlayer.UserId].ToString("dd/MM/yyyy");

            return;
        }

        accountCreationDateInfo.text = "LOADING...";
        GetAccountInfoResult result = await GetAccountCreationDateAsync(rig);
        AccountCreationDates[rig.OwningNetPlayer.UserId] = result.AccountInfo.Created;
        accountCreationDateInfo.text                     = result.AccountInfo.Created.ToString("dd/MM/yyyy");
        platformInfo.text                                = rig.GetPlatform().ParsePlatform();
    }

    private void DoCheatChecking(VRRig rig)
    {
        string mods = "Installed mods:\n" + rig.GetPlayerMods().Join("\n");
        installedMods.text = mods.Trim();
    }

    private async Task<GetAccountInfoResult> GetAccountCreationDateAsync(VRRig rig)
    {
        TaskCompletionSource<GetAccountInfoResult> tcs = new();

        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest { PlayFabId = rig.OwningNetPlayer.UserId, },
                result => tcs.SetResult(result),
                error =>
                {
                    Debug.LogError("Failed to get account info: " + error.ErrorMessage);
                    tcs.SetException(new Exception(error.ErrorMessage));
                });

        return await tcs.Task;
    }

    private string ParseIntoColourCode(Color colour)
    {
        int r = Mathf.RoundToInt(colour.r * 9);
        int g = Mathf.RoundToInt(colour.g * 9);
        int b = Mathf.RoundToInt(colour.b * 9);

        return $"<color=red>{r}</color> <color=green>{g}</color> <color=blue>{b}</color>";
    }

    private void NoPlayerSelected()
    {
        infoPanel.gameObject.SetActive(true);
        modsPanel.gameObject.SetActive(false);
        actionsPanel.gameObject.SetActive(false);
        playersPanel.gameObject.SetActive(false);

        playerNameInfo.text          = "No player selected";
        platformInfo.text            = "-";
        fpsInfo.text                 = "-";
        pingInfo.text                = "-";
        colourCodeInfo.text          = "-";
        velocityInfo.text            = "-";
        accountCreationDateInfo.text = "-";
        playerNameMods.text          = "No player selected";
        installedMods.text           = "-";
    }

    private void HighlightPlayer(VRRig rig)
    {
        if (rig == null)
        {
            playerHighlighter?.transform.SetParent(null);
            playerHighlighter?.SetActive(false);
        }
        else
        {
            playerHighlighter.transform.SetParent(rig.transform);
            playerHighlighter.transform.localPosition = Vector3.zero;
            playerHighlighter.transform.localRotation = Quaternion.identity;
            playerHighlighter.SetActive(true);
        }
    }

    private Material MakeMaterialTransparent(Material material)
    {
        material.shader = Plugin.UberShader;

        material.SetInt("_SrcBlend",      (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend",      (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
        material.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite",        0);
        material.SetInt("_AlphaToMask",   0);
        material.renderQueue = (int)RenderQueue.Transparent;

        return material;
    }

    private bool PhysicsRaycast(Vector3 origin, Vector3 direction, out RaycastHit hit, out VRRig rig)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, 1000f);

        rig = null;
        hit = default(RaycastHit);
        float minDistance = float.MaxValue;

        foreach (RaycastHit hit2 in hits)
            if ((1 << hit2.collider.gameObject.layer & GTPlayer.Instance.locomotionEnabledLayers) != 0
             || hit2.collider.GetComponentInParent<VRRig>() != null &&
                !hit2.collider.GetComponentInParent<VRRig>().isLocal)
                if (hit2.distance < minDistance)
                {
                    minDistance = hit2.distance;
                    hit         = hit2;
                    rig         = hit2.collider.GetComponentInParent<VRRig>();
                }

        return hit.collider != null;
    }
}