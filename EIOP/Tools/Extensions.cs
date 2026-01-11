using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Photon.Pun;
using UnityEngine;

namespace EIOP.Tools;

public enum GamePlatform
{
    Steam,
    OculusPC,
    PC,
    Standalone,
    Unknown,
}

public static class Extensions
{
    private static readonly Dictionary<VRRig, GamePlatform> PlayerPlatforms = new();
    private static readonly Dictionary<VRRig, List<string>> PlayerMods = new();
    private static readonly HashSet<VRRig> PlayersWithCosmetics = new();

    private static readonly Dictionary<GamePlatform, string> PlatformColors = new()
    {
        { GamePlatform.Unknown, "<color=#000000>Unknown</color>" },
        { GamePlatform.Steam, "<color=#0091F7>Steam</color>" },
        { GamePlatform.OculusPC, "<color=#0091F7>Oculus PCVR</color>" },
        { GamePlatform.PC, "<color=#000000>PC</color>" },
        { GamePlatform.Standalone, "<color=#26A6FF>Standalone</color>" }
    };

    private static readonly HashSet<GamePlatform> PCPlatforms = new()
    {
        GamePlatform.PC,
        GamePlatform.OculusPC,
        GamePlatform.Steam
    };

    public static GamePlatform GetPlatform(this VRRig rig) =>
        PlayerPlatforms.TryGetValue(rig, out var platform) ? platform : GamePlatform.Unknown;

    public static string ParsePlatform(this GamePlatform gamePlatform) =>
        PlatformColors.TryGetValue(gamePlatform, out var color) 
            ? color 
            : throw new ArgumentOutOfRangeException(nameof(gamePlatform), gamePlatform, null);

    public static bool IsOnPC(this GamePlatform gamePlatform) => 
        PCPlatforms.Contains(gamePlatform);

    public static Transform[] Children(this Transform transform)
    {
        var children = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child != transform && child.parent == transform)
                children.Add(child);
        }
        return children.ToArray();
    }

    public static string[] GetPlayerMods(this VRRig rig) =>
        PlayerMods.TryGetValue(rig, out var mods) ? mods.ToArray() : Array.Empty<string>();

    public static bool HasCosmetics(this VRRig rig) => 
        PlayersWithCosmetics.Contains(rig);

    public static string InsertNewlinesWithRichText(this string input, int interval)
    {
        if (string.IsNullOrEmpty(input) || interval <= 0)
            return input;

        var output = new StringBuilder(input.Length + input.Length / interval);
        var visibleCount = 0;
        var lastWhitespaceIndex = -1;
        var outputLengthAtLastWhitespace = -1;

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];

            if (c == '<')
            {
                var tagEnd = input.IndexOf('>', i);
                if (tagEnd == -1)
                {
                    output.Append(c);
                    continue;
                }

                output.Append(input, i, tagEnd - i + 1);
                i = tagEnd;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                lastWhitespaceIndex = i;
                outputLengthAtLastWhitespace = output.Length;
            }

            output.Append(c);
            visibleCount++;

            if (visibleCount >= interval)
            {
                if (outputLengthAtLastWhitespace != -1)
                {
                    output[outputLengthAtLastWhitespace] = '\n';
                    visibleCount = i - lastWhitespaceIndex;
                    lastWhitespaceIndex = -1;
                    outputLengthAtLastWhitespace = -1;
                }
                else
                {
                    output.Append('\n');
                    visibleCount = 0;
                }
            }
        }

        return output.ToString();
    }

    public static int GetPing(this VRRig rig)
    {
        try
        {
            var history = rig.velocityHistoryList;
            if (history == null || history.Count == 0)
                return int.MaxValue;

            var ping = Math.Abs((history[0].time - PhotonNetwork.Time) * 1000);
            return (int)Math.Clamp(Math.Round(ping), 0, int.MaxValue);
        }
        catch
        {
            return int.MaxValue;
        }
    }
}
