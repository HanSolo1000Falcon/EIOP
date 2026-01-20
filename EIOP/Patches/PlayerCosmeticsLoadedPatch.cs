using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Console;
using EIOP.Core;
using EIOP.Tab_Handlers;
using EIOP.Tools;
using ExitGames.Client.Photon;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using Extensions = EIOP.Tools.Extensions;

namespace EIOP.Patches;

[HarmonyPatch(typeof(VRRig))]
[HarmonyPatch("IUserCosmeticsCallback.OnGetUserCosmetics", MethodType.Normal)]
public static class PlayerCosmeticsLoadedPatch
{
    private static readonly DateTime OculusPayDay = new(2023, 02, 06);
    private static          void     Postfix(VRRig __instance, string cosmetics) => OnLoad(__instance);

    private static async void OnLoad(VRRig rig)
    {
        if (rig.isLocal)
            return;

        Extensions.PlayersWithCosmetics.Add(rig);

        DateTime playerCreationDate;

        if (!InformationHandler.AccountCreationDates.TryGetValue(rig.OwningNetPlayer.UserId, out playerCreationDate))
        {
            TaskCompletionSource<GetAccountInfoResult> tcs = new();

            PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest { PlayFabId = rig.OwningNetPlayer.UserId, },
                    result => tcs.SetResult(result),
                    error =>
                    {
                        Debug.LogError("Failed to get account info: " + error.ErrorMessage);
                        tcs.SetException(new Exception(error.ErrorMessage));
                    });

            GetAccountInfoResult result = await tcs.Task;
            InformationHandler.AccountCreationDates[rig.OwningNetPlayer.UserId] = result.AccountInfo.Created;
            playerCreationDate                                                  = result.AccountInfo.Created;
        }

        Hashtable    properties = rig.OwningNetPlayer.GetPlayerRef().CustomProperties;
        List<string> mods       = [];
        List<string> cheats     = [];

        Dictionary<string, string> knownCheats =
                ((JObject)DataHamburburOrg.Data["Known Cheats"])
               .ToObject<Dictionary<string, string>>();

        Dictionary<string, string> knownMods =
                ((JObject)DataHamburburOrg.Data["Known Mods"])
               .ToObject<Dictionary<string, string>>();

        foreach (string key in properties.Keys)
        {
            if (knownCheats.TryGetValue(key, out string cheat))
            {
                mods.Add($"[<color=red>{cheat}</color>]");
                cheats.Add(cheat);
            }

            if (knownMods.TryGetValue(key, out string mod))
                mods.Add($"[<color=green>{mod}</color>]");
        }

        if (cheats.Count > 0)
            Notifications.SendNotification(
                    $"[<color=red>Cheater</color>] Player {rig.OwningNetPlayer.SanitizedNickName} has the following cheats: {string.Join(", ", cheats)}.");

        Extensions.PlayerMods.Add(rig, mods);

        EIOPUtils.OnPlayerCosmeticsLoaded?.Invoke(rig);

        string cosmeticsAllowed = rig.rawCosmeticString.ToLower();

        if (cosmeticsAllowed.Contains("s. first login"))
        {
            Extensions.PlayerPlatforms[rig] = GamePlatform.Steam;

            return;
        }

        if (cosmeticsAllowed.Contains("first login") || cosmeticsAllowed.Contains("game-purchase"))
        {
            Extensions.PlayerPlatforms[rig] = GamePlatform.OculusPC;

            return;
        }

        if (rig.OwningNetPlayer.GetPlayerRef().CustomProperties.Count > 1)
        {
            Extensions.PlayerPlatforms[rig] = GamePlatform.PC;

            return;
        }

        if (playerCreationDate > OculusPayDay)
        {
            Extensions.PlayerPlatforms[rig] = GamePlatform.Standalone;

            return;
        }

        Extensions.PlayerPlatforms[rig] = GamePlatform.Unknown;
    }
}