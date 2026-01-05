using System.Collections.Generic;
using System.Linq;
using EIOP.Tools;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace EIOP.Core;

public class NetworkingHandler : MonoBehaviour
{
    private const byte NetworkingByte = 135;

    private static readonly Vector3    NetworkedMenuLocalPosition = new(-0.2487f, 0.0197f, 0f);
    private static readonly Quaternion NetworkedMenuLocalRotation = Quaternion.Euler(331.9757f, 348.8297f, 22.7959f);

    private readonly Dictionary<VRRig, GameObject> playerMenus = new();

    private GameObject emptyMenuPrefab;

    private void Start()
    {
        emptyMenuPrefab                      = Plugin.EIOPBundle.LoadAsset<GameObject>("EmptyMenu");
        emptyMenuPrefab.transform.localScale = MenuHandler.TargetMenuScale;
        MenuHandler.PerformShaderManagement(emptyMenuPrefab);

        EIOPUtils.OnPlayerCosmeticsLoaded += rig =>
                                             {
                                                 playerMenus[rig] = Instantiate(emptyMenuPrefab, rig.leftHandTransform);
                                                 playerMenus[rig].SetActive(false);
                                                 playerMenus[rig].transform.localPosition = NetworkedMenuLocalPosition;
                                                 playerMenus[rig].transform.localRotation = NetworkedMenuLocalRotation;
                                             };

        EIOPUtils.OnPlayerRigCached += rig =>
                                       {
                                           if (!playerMenus.TryGetValue(rig, out GameObject menu))
                                               return;

                                           Destroy(menu);
                                           playerMenus.Remove(rig);
                                       };

        PhotonNetwork.NetworkingClient.EventReceived += eventData =>
                                                        {
                                                            if (eventData.Code != NetworkingByte)
                                                                return;

                                                            VRRig sender = GorillaParent.instance.vrrigs
                                                                   .FirstOrDefault(rig => rig.OwningNetPlayer
                                                                                   .ActorNumber ==
                                                                            eventData.Sender);

                                                            if (sender == null)
                                                                return;

                                                            if (!playerMenus.ContainsKey(sender))
                                                                return;

                                                            if (!eventData.Parameters.TryGetValue(ParameterCode.Data,
                                                                        out object data))
                                                                return;

                                                            playerMenus[sender].SetActive((bool)data);
                                                        };
    }
}