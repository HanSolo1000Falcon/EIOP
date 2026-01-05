using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using BepInEx;
using EIOP.Anti_Cheat;
using EIOP.Core;
using EIOP.Tools;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace EIOP;

[BepInPlugin(Constants.PluginGuid, Constants.PluginName, Constants.PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    private const string GorillaInfoEndPointURL =
            "https://raw.githubusercontent.com/HanSolo1000Falcon/GorillaInfo/main/";

    public static Dictionary<string, string> KnownCheats;
    public static Dictionary<string, string> KnownMods;

    public static AssetBundle EIOPBundle;
    public static Shader      UberShader;
    public static AudioClip   ButtonPressSound;

    public static Color MainColour;
    public static Color SecondaryColour;

    public static AudioSource PluginAudioSource;

    private void Start()
    {
        new Harmony(Constants.PluginGuid).PatchAll();
        GorillaTagger.OnPlayerSpawned(OnGameInitialized);
    }

    public static void PlaySound(AudioClip audioClip)
    {
        if (audioClip != null && PluginAudioSource != null)
            PluginAudioSource.PlayOneShot(audioClip);
    }

    private void OnGameInitialized()
    {
        PCHandler.ThirdPersonCameraTransform = GorillaTagger.Instance.thirdPersonCamera.transform.GetChild(0);
        PCHandler.ThirdPersonCamera          = PCHandler.ThirdPersonCameraTransform.GetComponent<Camera>();

        Stream bundleStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EIOP.Resources.eiopbundle");
        EIOPBundle = AssetBundle.LoadFromStream(bundleStream);
        bundleStream.Close();

        UberShader = Shader.Find("GorillaTag/UberShader");

        ButtonPressSound = LoadWavFromResource("EIOP.Resources.ButtonPressWood.wav");

        PluginAudioSource              = new GameObject("LocalAudioSource").AddComponent<AudioSource>();
        PluginAudioSource.spatialBlend = 0f;
        PluginAudioSource.playOnAwake  = false;

        Type[] antiCheatHandlers = Assembly.GetExecutingAssembly().GetTypes()
                                           .Where(t => t.IsClass && !t.IsAbstract &&
                                                       typeof(AntiCheatHandlerBase).IsAssignableFrom(t)).ToArray();

        foreach (Type antiCheatHandlerType in antiCheatHandlers)
            gameObject.AddComponent(antiCheatHandlerType);

        gameObject.AddComponent<CoroutineManager>();
        gameObject.AddComponent<EIOPUtils>();
        gameObject.AddComponent<Notifications>();
        gameObject.AddComponent<NetworkingHandler>();
        gameObject.AddComponent<MenuHandler>();

        FetchModsAndCheats();
    }

    private void FetchModsAndCheats()
    {
        using HttpClient httpClient = new();
        HttpResponseMessage gorillaInfoEndPointResponse =
                httpClient.GetAsync(GorillaInfoEndPointURL + "KnownCheats.txt").Result;

        using (Stream stream = gorillaInfoEndPointResponse.Content.ReadAsStreamAsync().Result)
        {
            using (StreamReader reader = new(stream))
            {
                KnownCheats = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
            }
        }

        HttpResponseMessage knownModsEndPointResponse =
                httpClient.GetAsync(GorillaInfoEndPointURL + "KnownMods.txt").Result;

        using (Stream stream = knownModsEndPointResponse.Content.ReadAsStreamAsync().Result)
        {
            using (StreamReader reader = new(stream))
            {
                KnownMods = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
            }
        }
    }

    private AudioClip LoadWavFromResource(string resourcePath)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath);

        if (stream == null)
            return null;

        byte[] buffer = new byte[stream.Length];
        int    read   = stream.Read(buffer, 0, buffer.Length);

        WAV     wav = new(buffer);
        float[] samples;

        if (wav.ChannelCount == 2)
        {
            samples = new float[wav.SampleCount];
            for (int i = 0; i < wav.SampleCount; i++)
                samples[i] = (wav.LeftChannel[i] + wav.RightChannel[i]) * 0.5f;
        }
        else
        {
            samples = wav.LeftChannel;
        }

        AudioClip audioClip = AudioClip.Create(resourcePath, wav.SampleCount, 1, wav.Frequency, false);
        audioClip.SetData(samples, 0);

        return audioClip;
    }
}