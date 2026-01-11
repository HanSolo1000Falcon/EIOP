using UnityEngine;

namespace EIOP.Anti_Cheat;

[DisallowMultipleComponent]
public class AntiCheatHandlerBase : MonoBehaviour
{
    private static AntiCheatHandlerBase instance;

    public static AntiCheatHandlerBase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AntiCheatHandlerBase>();
                
                if (instance == null)
                {
                    var go = new GameObject("AntiCheatHandler");
                    instance = go.AddComponent<AntiCheatHandlerBase>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
