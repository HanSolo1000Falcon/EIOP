using UnityEngine;

namespace EIOP.Tools;

public class CoroutineManager : MonoBehaviour
{
    public static CoroutineManager Instance { get; private set; }

    private void Awake() => Instance = this;
}