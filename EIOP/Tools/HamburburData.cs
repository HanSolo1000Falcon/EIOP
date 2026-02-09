using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace EIOP.Tools;

public class HamburburData : MonoBehaviour
{
    public const  string  DataUrl = "https://hamburbur.org/data";
    public static JObject Data         { get; private set; }
    public static bool    IsDataLoaded { get; private set; }

    private IEnumerator Start()
    {
        while (true)
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get(DataUrl);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string maybeJson = webRequest.downloadHandler.text;

                try
                {
                    Data         = JObject.Parse(maybeJson);
                    IsDataLoaded = true;
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                            $"[EIOP] Failed to fetch a valid JSON from \"{DataUrl}\". Error message: {exception.Message}");
                }
            }
            else
            {
                Debug.LogError(
                        $"[EIOP] Failed to fetch data from \"{DataUrl}\". User may have consoless installed. Error message: {webRequest.error}");
            }

            yield return new WaitForSeconds(60);
        }
    }
}