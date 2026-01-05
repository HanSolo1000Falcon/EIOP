using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using EIOP.Core;
using EIOP.Tools;
using Photon.Voice.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace EIOP.Tab_Handlers;

public class SoundboardHandler : TabHandlerBase
{
    private static readonly string             SoundsFolder        = Path.Combine(Paths.BepInExRootPath, "EIOPSounds");
    private static readonly string[]           SupportedExtensions = [".wav", ".ogg", ".mp3",];
    private static readonly List<Sound>        KnownSounds         = [];
    private static readonly List<Sound>        LoadedSounds        = [];
    private readonly        SoundboardButton[] soundboardButtons   = new SoundboardButton[6];
    private                 AudioClip          cachedClip;

    private int  currentPageIndex;
    private bool isPlaying;

    private float stopPoint;
    private float stopTime;

    private void Start()
    {
        for (int i = 0; i < soundboardButtons.Length; i++)
        {
            SoundboardButton soundboardButton = new()
            {
                    AssociatedButton = transform.GetChild(i).gameObject,
            };

            soundboardButton.AssociatedButton.AddComponent<EIOPButton>();

            soundboardButtons[i] = soundboardButton;
        }

        if (!Directory.Exists(SoundsFolder))
            Directory.CreateDirectory(SoundsFolder);

        foreach (string soundFile in Directory.EnumerateFiles(SoundsFolder, "*.*", SearchOption.AllDirectories))
        {
            string extension = Path.GetExtension(soundFile);

            if (!SupportedExtensions.Contains(extension))
                continue;

            Sound sound = new()
            {
                    SoundName          = Path.GetFileNameWithoutExtension(soundFile),
                    SoundNameExtension = Path.GetFullPath(soundFile),
            };

            KnownSounds.Add(sound);
        }

        transform.GetChild(6).AddComponent<EIOPButton>().OnPress = () =>
                                                                   {
                                                                       currentPageIndex--;
                                                                       UpdateButtons();
                                                                   };

        transform.GetChild(7).AddComponent<EIOPButton>().OnPress = () =>
                                                                   {
                                                                       currentPageIndex++;
                                                                       UpdateButtons();
                                                                   };

        transform.GetChild(8).AddComponent<EIOPButton>().OnPress = () =>
                                                                   {
                                                                       if (isPlaying)
                                                                       {
                                                                           float elapsedTime =
                                                                                   Time.time -
                                                                                   (stopPoint - cachedClip.length);

                                                                           stopTime = elapsedTime;

                                                                           StopSounds();
                                                                       }
                                                                       else
                                                                       {
                                                                           if (stopTime > 0f && cachedClip != null)
                                                                           {
                                                                               int sampleRate   = cachedClip.frequency;
                                                                               int totalSamples = cachedClip.samples;
                                                                               int channels     = cachedClip.channels;

                                                                               float clipLength = cachedClip.length;

                                                                               stopTime = Mathf.Clamp(stopTime, 0f,
                                                                                       clipLength);

                                                                               int startSample = Mathf.Clamp(
                                                                                       (int)(stopTime * sampleRate),
                                                                                       0, totalSamples);

                                                                               float[] samples =
                                                                                       new float[totalSamples *
                                                                                           channels];

                                                                               cachedClip.GetData(samples, 0);

                                                                               int samplesToKeep =
                                                                                       totalSamples - startSample;

                                                                               if (samplesToKeep <= 0)
                                                                                   samplesToKeep = 1;

                                                                               float[] trimmedSamples =
                                                                                       new float[samplesToKeep *
                                                                                           channels];

                                                                               Array.Copy(samples,
                                                                                       startSample * channels,
                                                                                       trimmedSamples,
                                                                                       0,
                                                                                       samplesToKeep * channels);

                                                                               AudioClip trimmedClip = AudioClip.Create(
                                                                                       cachedClip.name + "_trimmed",
                                                                                       samplesToKeep,
                                                                                       channels,
                                                                                       sampleRate,
                                                                                       false
                                                                               );

                                                                               trimmedClip.SetData(trimmedSamples, 0);

                                                                               Plugin.PlaySound(trimmedClip);

                                                                               Recorder recorder =
                                                                                       GorillaTagger.Instance
                                                                                              .myRecorder;

                                                                               recorder.SourceType =
                                                                                       Recorder.InputSourceType
                                                                                              .AudioClip;

                                                                               recorder.AudioClip = trimmedClip;
                                                                               recorder.RestartRecording(true);
                                                                               recorder.DebugEchoMode = false;
                                                                               isPlaying              = true;

                                                                               stopPoint =
                                                                                       Time.time + trimmedClip.length;
                                                                           }
                                                                       }

                                                                       transform.GetChild(8).GetChild(0).gameObject
                                                                              .SetActive(!isPlaying);

                                                                       transform.GetChild(8).GetChild(1).gameObject
                                                                              .SetActive(isPlaying);
                                                                   };

        UpdateButtons();
    }

    private void Update()
    {
        if (UnityInput.Current.GetKeyDown(KeyCode.J))
        {
            if (!isPlaying && cachedClip == null)
            {
                PlaySound(KnownSounds[0]);

                return;
            }

            transform.GetChild(8).GetComponent<EIOPButton>().OnPress?.Invoke();
        }

        if (!isPlaying || !(stopPoint < Time.time))
            return;

        StopSounds();
    }

    private void StopSounds()
    {
        isPlaying = false;
        stopPoint = -1f;
        FixMicrophone();

        if (Plugin.PluginAudioSource.isPlaying)
            Plugin.PluginAudioSource.Stop();

        transform.GetChild(8).GetChild(0).gameObject
                 .SetActive(!isPlaying);

        transform.GetChild(8).GetChild(1).gameObject
                 .SetActive(isPlaying);
    }

    private void UpdateButtons()
    {
        if (KnownSounds.Count > 0)
            currentPageIndex %= Mathf.Max(1, Mathf.CeilToInt(KnownSounds.Count / 6f));
        else
            currentPageIndex = 0;

        int startIndex = currentPageIndex * 6;
        int endIndex   = Mathf.Min(startIndex + 6, KnownSounds.Count);

        for (int i = soundboardButtons.Length - 1; i >= 0; i--)
        {
            SoundboardButton soundboardButton = soundboardButtons[i];
            soundboardButton.AssociatedButton.SetActive(false);
            soundboardButton.AssociatedButton.GetComponent<EIOPButton>().OnPress = null;
        }

        for (int i = startIndex; i < endIndex; i++)
        {
            int              localIndex       = i - startIndex;
            SoundboardButton soundboardButton = soundboardButtons[localIndex];
            Sound            sound            = KnownSounds[i];

            soundboardButton.AssociatedButton.SetActive(true);
            soundboardButton.AssociatedButton.GetComponent<EIOPButton>().OnPress         = () => PlaySound(sound);
            soundboardButton.AssociatedButton.GetComponentInChildren<TextMeshPro>().text = sound.SoundName;
        }
    }

    private void PlaySound(Sound sound)
    {
        if (!NetworkSystem.Instance.InRoom)
            return;

        if (LoadedSounds.Contains(sound))
        {
            stopTime                                     = -1f;
            cachedClip                                   = sound.AudioClip;
            GorillaTagger.Instance.myRecorder.SourceType = Recorder.InputSourceType.AudioClip;
            GorillaTagger.Instance.myRecorder.AudioClip  = sound.AudioClip;
            GorillaTagger.Instance.myRecorder.RestartRecording(true);
            GorillaTagger.Instance.myRecorder.DebugEchoMode = false;
            if (isPlaying) Plugin.PluginAudioSource.Stop();
            Plugin.PlaySound(sound.AudioClip);
            isPlaying = true;
            stopPoint = Time.time + sound.AudioClip.length;
            transform.GetChild(8).GetChild(0).gameObject.SetActive(!isPlaying);
            transform.GetChild(8).GetChild(1).gameObject.SetActive(isPlaying);
        }
        else
        {
            CoroutineManager.Instance.StartCoroutine(LoadSound(sound.SoundNameExtension, clip =>
                {
                    if (clip == null)
                        return;

                    sound.AudioClip = clip;
                    LoadedSounds.Add(sound);
                    PlaySound(sound);
                }));
        }
    }

    private IEnumerator LoadSound(string filePath, Action<AudioClip> callback)
    {
        using UnityWebRequest getMedia = UnityWebRequestMultimedia.GetAudioClip(
                $"file://{filePath}", GetAudioType(Path.GetExtension(filePath)));

        yield return getMedia.SendWebRequest();

        if (getMedia.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to load {filePath}: {getMedia.error}");
            callback(null);
        }
        else
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(getMedia);
            callback(clip);
        }
    }

    private void FixMicrophone()
    {
        if (!NetworkSystem.Instance.InRoom)
            return;

        GorillaTagger.Instance.myRecorder.SourceType = Recorder.InputSourceType.Microphone;
        GorillaTagger.Instance.myRecorder.AudioClip  = null;
        GorillaTagger.Instance.myRecorder.RestartRecording(true);
        GorillaTagger.Instance.myRecorder.DebugEchoMode = false;
    }

    private static AudioType GetAudioType(string extension)
    {
        return extension.ToLower() switch
               {
                       ".wav" => AudioType.WAV,
                       ".ogg" => AudioType.OGGVORBIS,
                       ".mp3" => AudioType.MPEG,
                       var _  => AudioType.UNKNOWN,
               };
    }

    private struct SoundboardButton
    {
        public GameObject AssociatedButton;
    }

    private class Sound
    {
        public AudioClip AudioClip;
        public string    SoundName;
        public string    SoundNameExtension;
    }
}