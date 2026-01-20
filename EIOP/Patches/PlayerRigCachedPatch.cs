using EIOP.Tools;
using HarmonyLib;

namespace EIOP.Patches;

[HarmonyPatch(typeof(VRRigCache), nameof(VRRigCache.RemoveRigFromGorillaParent))]
public class PlayerRigCachedPatch
{
    private static void Postfix(NetPlayer player, VRRig vrrig)
    {
        EIOPUtils.OnPlayerRigCached?.Invoke(vrrig);

        Extensions.PlayersWithCosmetics.Remove(vrrig);
        Extensions.PlayerPlatforms.Remove(vrrig);
        Extensions.PlayerMods.Remove(vrrig);

        if (VoicePrioritizationPatch.PrioritizedPeople.Contains(vrrig))
            VoicePrioritizationPatch.PrioritizedPeople.Remove(vrrig);
    }
}