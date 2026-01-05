using System.Collections.Generic;
using System.Linq;
using EIOP.Core;
using EIOP.Tools;
using GorillaNetworking;

namespace EIOP.Anti_Cheat;

public class CosmetXChecker : AntiCheatHandlerBase
{
    public static readonly Dictionary<VRRig, bool> LastCosmetXState = new();

    private void Update()
    {
        if (GorillaParent.instance == null || GorillaParent.instance.vrrigs == null)
            return;

        foreach (VRRig rig in GorillaParent.instance.vrrigs.Where(rig => !rig.isLocal && rig.HasCosmetics()))
        {
            LastCosmetXState.TryAdd(rig, false);

            bool hasCosmetx = false;

            CosmeticsController.CosmeticSet cosmeticSet = rig.cosmeticSet;
            if (cosmeticSet.items.Any(cosmetic => !cosmetic.isNullItem &&
                                                  !rig.concatStringOfCosmeticsAllowed.Contains(cosmetic.itemName)))
                hasCosmetx = true;

            switch (hasCosmetx)
            {
                case true when LastCosmetXState.ContainsKey(rig) && !LastCosmetXState[rig]:
                {
                    Notifications.SendNotification(
                            $"[<color=red>Cheater</color>] Player {rig.OwningNetPlayer.SanitizedNickName} has CosmetX installed.");

                    if (Extensions.PlayerMods[rig] != null &&
                        !Extensions.PlayerMods[rig].Contains("[<color=red>CosmetX</color>]"))
                        Extensions.PlayerMods[rig].Add("[<color=red>CosmetX</color>]");

                    break;
                }

                case false when LastCosmetXState.ContainsKey(rig)  && LastCosmetXState[rig] &&
                                Extensions.PlayerMods[rig] != null &&
                                Extensions.PlayerMods[rig].Contains("[<color=red>CosmetX</color>]"):
                    Extensions.PlayerMods[rig].Remove("[<color=red>CosmetX</color>]");

                    break;
            }

            LastCosmetXState[rig] = hasCosmetx;
        }
    }
}