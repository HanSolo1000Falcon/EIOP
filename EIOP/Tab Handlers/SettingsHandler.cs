using EIOP.Core;
using EIOP.Patches;
using GorillaGameModes;
using GorillaNetworking;
using TMPro;
using UnityEngine;

namespace EIOP.Tab_Handlers;

public class SettingsHandler : TabHandlerBase
{
    private Transform gamemodePanel;
    private Transform miscPanel;
    private Transform queuePanel;

    private void Start()
    {
        miscPanel     = transform.Find("MiscTab");
        queuePanel    = transform.Find("QueueTab");
        gamemodePanel = transform.Find("GamemodeTab");

        SetUpMiscTab();
        SetUpQueueTab();
        SetUpGamemodeTab();

        transform.Find("MiscTabButton").AddComponent<EIOPButton>().OnPress = () =>
                                                                             {
                                                                                 miscPanel.gameObject.SetActive(true);
                                                                                 queuePanel.gameObject.SetActive(false);
                                                                                 gamemodePanel.gameObject.SetActive(
                                                                                         false);

                                                                                 OnMiscTabEntered();
                                                                             };

        transform.Find("QueueTabButton").AddComponent<EIOPButton>().OnPress = () =>
                                                                              {
                                                                                  miscPanel.gameObject.SetActive(false);
                                                                                  queuePanel.gameObject.SetActive(true);
                                                                                  gamemodePanel.gameObject.SetActive(
                                                                                          false);

                                                                                  OnQueueTabEntered();
                                                                              };

        transform.Find("GamemodeTabButton").AddComponent<EIOPButton>().OnPress = () =>
            {
                miscPanel.gameObject.SetActive(false);
                queuePanel.gameObject.SetActive(false);
                gamemodePanel.gameObject.SetActive(true);
                OnGamemodeTabEntered();
            };
    }

#region Misc Tab

    private enum MicState
    {
        PushToTalk,
        OpenMic,
        PushToMute,
    }

    private GameObject monkeVoices;
    private GameObject pushToTalk;
    private GameObject openMic;
    private GameObject pushToMute;

    private MicState micState;

    private void OnMiscTabEntered()
    {
        FetchCurrentMicState();
        UpdateMiscTexts();
    }

    private void SetUpMiscTab()
    {
        monkeVoices = miscPanel.Find("MonkeVoices").gameObject;
        pushToTalk  = miscPanel.Find("PushToTalk").gameObject;
        openMic     = miscPanel.Find("OpenMic").gameObject;
        pushToMute  = miscPanel.Find("PushToMute").gameObject;

        monkeVoices.AddComponent<EIOPButton>().OnPress = () =>
                                                         {
                                                             GorillaComputer.instance.voiceChatOn =
                                                                     GorillaComputer.instance.voiceChatOn == "TRUE"
                                                                             ? "FALSE"
                                                                             : "TRUE";

                                                             PlayerPrefs.SetString("voiceChatOn",
                                                                     GorillaComputer.instance.voiceChatOn);

                                                             PlayerPrefs.Save();
                                                             RigContainer.RefreshAllRigVoices();
                                                             UpdateMiscTexts();
                                                         };

        pushToTalk.AddComponent<EIOPButton>().OnPress = () =>
                                                        {
                                                            micState = MicState.PushToTalk;
                                                            UpdateMicState();
                                                            UpdateMiscTexts();
                                                        };

        openMic.AddComponent<EIOPButton>().OnPress = () =>
                                                     {
                                                         micState = MicState.OpenMic;
                                                         UpdateMicState();
                                                         UpdateMiscTexts();
                                                     };

        pushToMute.AddComponent<EIOPButton>().OnPress = () =>
                                                        {
                                                            micState = MicState.PushToMute;
                                                            UpdateMicState();
                                                            UpdateMiscTexts();
                                                        };

        miscPanel.Find("UnprioritizeAll").AddComponent<EIOPButton>().OnPress =
                () => VoicePrioritizationPatch.PrioritizedPeople.Clear();

        FetchCurrentMicState();
        UpdateMiscTexts();
    }

    private void UpdateMicState()
    {
        string modeString = micState switch
                            {
                                    MicState.PushToTalk => "PUSH TO TALK",
                                    MicState.OpenMic    => "OPEN MIC",
                                    MicState.PushToMute => "PUSH TO MUTE",
                                    var _               => "OPEN MIC",
                            };

        GorillaComputer.instance.pttType = modeString;
        PlayerPrefs.SetString("pttType", modeString);
        PlayerPrefs.Save();
    }

    private void FetchCurrentMicState()
    {
        micState = PlayerPrefs.GetString("pttType", "OPEN MIC") switch
                   {
                           "OPEN MIC"     => MicState.OpenMic,
                           "PUSH TO TALK" => MicState.PushToTalk,
                           "PUSH TO MUTE" => MicState.PushToMute,
                           var _          => MicState.OpenMic,
                   };
    }

    private void UpdateMiscTexts()
    {
        monkeVoices.GetComponentInChildren<TextMeshPro>().text =
                "Monke voices\n" + (GorillaComputer.instance.voiceChatOn == "FALSE"
                                            ? "<color=green>Enabled</color>"
                                            : "<color=red>Disabled</color>");

        pushToTalk.GetComponentInChildren<TextMeshPro>().text =
                "Push to talk\n" + (micState == MicState.PushToTalk
                                            ? "<color=green>Enabled</color>"
                                            : "<color=red>Disabled</color>");

        openMic.GetComponentInChildren<TextMeshPro>().text =
                "Open mic\n" + (micState == MicState.OpenMic
                                        ? "<color=green>Enabled</color>"
                                        : "<color=red>Disabled</color>");

        pushToMute.GetComponentInChildren<TextMeshPro>().text =
                "Push to mute\n" + (micState == MicState.PushToMute
                                            ? "<color=green>Enabled</color>"
                                            : "<color=red>Disabled</color>");
    }

#endregion

#region Queue Tab

    private enum QueueState
    {
        Default,
        Competitive,
        MiniGames,
    }

    private GameObject defaultQueue;
    private GameObject competitiveQueue;
    private GameObject minigamesQueue;

    private QueueState queueState;

    private void OnQueueTabEntered()
    {
        UpdateQueueState();
        UpdateQueueTexts();
    }

    private void SetUpQueueTab()
    {
        defaultQueue     = queuePanel.Find("Default").gameObject;
        competitiveQueue = queuePanel.Find("Competitive").gameObject;
        minigamesQueue   = queuePanel.Find("MiniGames").gameObject;

        defaultQueue.AddComponent<EIOPButton>().OnPress = () =>
                                                          {
                                                              queueState = QueueState.Default;
                                                              UpdateQueueState();
                                                              UpdateQueueTexts();
                                                          };

        competitiveQueue.AddComponent<EIOPButton>().OnPress = () =>
                                                              {
                                                                  queueState = QueueState.Competitive;
                                                                  UpdateQueueState();
                                                                  UpdateQueueTexts();
                                                              };

        minigamesQueue.AddComponent<EIOPButton>().OnPress = () =>
                                                            {
                                                                queueState = QueueState.MiniGames;
                                                                UpdateQueueState();
                                                                UpdateQueueTexts();
                                                            };

        FetchCurrentQueueState();
        UpdateQueueTexts();
    }

    private void UpdateQueueState()
    {
        string queueName = queueState switch
                           {
                                   QueueState.Default     => "DEFAULT",
                                   QueueState.Competitive => "COMPETITIVE",
                                   QueueState.MiniGames   => "MINIGAMES",
                                   var _                  => "DEFAULT",
                           };

        GorillaComputer.instance.currentQueue = queueName;
        PlayerPrefs.SetString("currentQueue", queueName);
        PlayerPrefs.Save();
    }

    private void FetchCurrentQueueState()
    {
        queueState = GorillaComputer.instance.currentQueue switch
                     {
                             "DEFAULT"     => QueueState.Default,
                             "COMPETITIVE" => QueueState.Competitive,
                             "MINIGAMES"   => QueueState.MiniGames,
                             var _         => QueueState.Default,
                     };
    }

    private void UpdateQueueTexts()
    {
        defaultQueue.GetComponentInChildren<TextMeshPro>().text =
                "Default\n" + (queueState == QueueState.Default
                                       ? "<color=green>Enabled</color>"
                                       : "<color=red>Disabled</color>");

        competitiveQueue.GetComponentInChildren<TextMeshPro>().text =
                "Competitive\n" + (queueState == QueueState.Competitive
                                           ? "<color=green>Enabled</color>"
                                           : "<color=red>Disabled</color>");

        minigamesQueue.GetComponentInChildren<TextMeshPro>().text =
                "Mini games\n" + (queueState == QueueState.MiniGames
                                          ? "<color=green>Enabled</color>"
                                          : "<color=red>Disabled</color>");
    }

#endregion

#region Gamemode Tab

    private enum GamemodeState
    {
        Infection,
        Casual,
        Unknown,
    }

    private GameObject infection;
    private GameObject casual;

    private GamemodeState gamemodeState;

    private void OnGamemodeTabEntered() { }

    private void SetUpGamemodeTab()
    {
        infection = gamemodePanel.Find("Infection").gameObject;
        casual    = gamemodePanel.Find("Casual").gameObject;

        infection.AddComponent<EIOPButton>().OnPress = () =>
                                                       {
                                                           gamemodeState = GamemodeState.Infection;
                                                           UpdateGamemodeState();
                                                           UpdateGamemodeTexts();
                                                       };

        casual.AddComponent<EIOPButton>().OnPress = () =>
                                                    {
                                                        gamemodeState = GamemodeState.Casual;
                                                        UpdateGamemodeState();
                                                        UpdateGamemodeTexts();
                                                    };

        FetchCurrentGamemodeState();
        UpdateGamemodeTexts();
    }

    private void UpdateGamemodeState()
    {
        string currentGameModeName = gamemodeState switch
                                     {
                                             GamemodeState.Infection => nameof(GameModeType.Infection),
                                             GamemodeState.Casual    => nameof(GameModeType.Casual),
                                             var _                   => nameof(GameModeType.Infection),
                                     };

        GorillaComputer.instance.OnModeSelectButtonPress(currentGameModeName, false);
    }

    private void FetchCurrentGamemodeState()
    {
        string currentGamemode = PlayerPrefs.GetString("currentGameMode", nameof(GameModeType.Infection));
        gamemodeState = currentGamemode switch
                        {
                                nameof(GameModeType.Infection) => GamemodeState.Infection,
                                nameof(GameModeType.Casual)    => GamemodeState.Casual,
                                var _                          => GamemodeState.Unknown,
                        };
    }

    private void UpdateGamemodeTexts()
    {
        bool isInfection = gamemodeState == GamemodeState.Infection;
        bool isCasual    = gamemodeState == GamemodeState.Casual;

        infection.GetComponentInChildren<TextMeshPro>().text =
                "Infection\n" + (isInfection ? "<color=green>Enabled</color>" : "<color=red>Disabled</color>");

        casual.GetComponentInChildren<TextMeshPro>().text =
                "Casual\n" + (isCasual ? "<color=green>Enabled</color>" : "<color=red>Disabled</color>");
    }

#endregion
}