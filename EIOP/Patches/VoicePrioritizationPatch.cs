using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EIOP.Patches;

[HarmonyPatch(typeof(VRRig), nameof(VRRig.PostTick))]
public static class VoicePrioritizationPatch
{
    public static List<VRRig> PrioritizedPeople = [];

    private static void Postfix(VRRig __instance)
    {
        AudioSource voice = __instance.voiceAudio;

        if (voice == null)
            return;

        if (PrioritizedPeople.Count == 0)
        {
            voice.volume = 1f;

            return;
        }

        voice.volume = PrioritizedPeople.Contains(__instance) ? 1f : 0.3f;
    }
}