using EIOP.Core;
using EIOP.Patches;
using GorillaNetworking;
using Photon.Pun;
using TMPro;

namespace EIOP.Tab_Handlers;

public class RoomHandler : TabHandlerBase
{
    private bool displayRoomCode;

    private void Start()
    {
        transform.GetChild(0).AddComponent<EIOPButton>().OnPress = () => NetworkSystem.Instance.ReturnToSinglePlayer();
        transform.GetChild(1).AddComponent<EIOPButton>().OnPress =
                () => PhotonNetworkController.Instance.AttemptToJoinPublicRoom(
                        GorillaNetworkJoinTriggerPatch.LastGorillaNetworkJoinTrigger ??
                        FindFirstObjectByType<GorillaNetworkJoinTrigger>());

        transform.GetChild(2).AddComponent<EIOPButton>().OnPress = () =>
                                                                   {
                                                                       displayRoomCode = !displayRoomCode;
                                                                       transform.GetChild(2)
                                                                                      .GetComponentInChildren<
                                                                                               TextMeshPro>()
                                                                                      .text =
                                                                               "Show room code\n" + (displayRoomCode
                                                                                                   ? "<color=green>Enabled</color>"
                                                                                                   : "<color=red>Disabled</color>");

                                                                       UpdateRoomInfoText(NetworkSystem.Instance
                                                                              .InRoom);
                                                                   };

        UpdateRoomInfoText(NetworkSystem.Instance.InRoom);

        NetworkSystem.Instance.OnJoinedRoomEvent        += () => UpdateRoomInfoText(true);
        NetworkSystem.Instance.OnReturnedToSinglePlayer += () => UpdateRoomInfoText(false);

        NetworkSystem.Instance.OnPlayerJoined += player => UpdateRoomInfoText(true);
        NetworkSystem.Instance.OnPlayerLeft   += player => UpdateRoomInfoText(true);
    }

    private void UpdateRoomInfoText(bool inRoom)
    {
        TextMeshPro toChange = transform.GetChild(3).GetComponent<TextMeshPro>();

        toChange.text = inRoom
                                ? $"Room information\nCode: {(displayRoomCode ? PhotonNetwork.CurrentRoom.Name : "-")}\nPlayers: {PhotonNetwork.CurrentRoom.PlayerCount}\nGamemode: {GetGamemodeKey(NetworkSystem.Instance.GameModeString)}\nQueue: {GetQueueKey(NetworkSystem.Instance.GameModeString)}"
                                : "Room information\nCode: -\nPlayers: -\nGamemode: -\nQueue: -";
    }

    private string GetGamemodeKey(string gamemodeString)
    {
        gamemodeString = gamemodeString.ToUpper();

        if (gamemodeString.Contains("CASUAL")) return "Casual";
        if (gamemodeString.Contains("INFECTION")) return "Infection";
        if (gamemodeString.Contains("HUNT")) return "Hunt";
        if (gamemodeString.Contains("FREEZE")) return "Freeze";
        if (gamemodeString.Contains("PAINTBRAWL")) return "Paintbrawl";
        if (gamemodeString.Contains("AMBUSH")) return "Ambush";
        if (gamemodeString.Contains("GHOST")) return "Ghostt";
        if (gamemodeString.Contains("GUARDIAN")) return "Guardian";

        return gamemodeString.Contains("CUSTOM") ? "Custom" : gamemodeString;
    }

    private string GetQueueKey(string gamemodeString)
    {
        gamemodeString = gamemodeString.ToUpper();

        if (gamemodeString.Contains("DEFAULT")) return "Default";
        if (gamemodeString.Contains("MINIGAMES")) return "Minigames";

        return gamemodeString.Contains("COMPETITIVE") ? "Competitive" : gamemodeString;
    }
}