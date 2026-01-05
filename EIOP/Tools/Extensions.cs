using System;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;

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
    public static Dictionary<VRRig, GamePlatform> PlayerPlatforms      = new();
    public static Dictionary<VRRig, List<string>> PlayerMods           = new();
    public static List<VRRig>                     PlayersWithCosmetics = [];

    public static GamePlatform GetPlatform(this VRRig rig) =>
            PlayerPlatforms.GetValueOrDefault(rig, GamePlatform.Unknown);

    public static string ParsePlatform(this GamePlatform gamePlatform)
    {
        return gamePlatform switch
               {
                       GamePlatform.Unknown    => "<color=#000000>Unknown</color>",
                       GamePlatform.Steam      => "<color=#0091F7>Steam</color>",
                       GamePlatform.OculusPC   => "<color=#0091F7>Oculus PCVR</color>",
                       GamePlatform.PC         => "<color=#000000>PC</color>",
                       GamePlatform.Standalone => "<color=#26A6FF>Standalone</color>",
                       var _                   => throw new ArgumentOutOfRangeException(),
               };
    }

    public static bool IsOnPC(this GamePlatform gamePlatform)
    {
        return gamePlatform switch
               {
                       GamePlatform.PC       => true,
                       GamePlatform.OculusPC => true,
                       GamePlatform.Steam    => true,
                       var _                 => false,
               };
    }

    public static string[] GetPlayerMods(this VRRig rig) => PlayerMods[rig].ToArray();

    public static bool HasCosmetics(this VRRig rig) => PlayersWithCosmetics.Contains(rig);

    public static string InsertNewlinesWithRichText(this string input, int interval)
    {
        if (string.IsNullOrEmpty(input) || interval <= 0)
            return input;

        StringBuilder output                       = new();
        int           visibleCount                 = 0;
        int           lastWhitespaceIndex          = -1;
        int           outputLengthAtLastWhitespace = -1;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '<')
            {
                int tagEnd = input.IndexOf('>', i);
                if (tagEnd == -1)
                {
                    output.Append(c);

                    continue;
                }

                output.Append(input.AsSpan(i, tagEnd - i + 1));
                i = tagEnd;

                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                lastWhitespaceIndex          = i;
                outputLengthAtLastWhitespace = output.Length;
            }

            output.Append(c);
            visibleCount++;

            if (visibleCount >= interval)
            {
                if (outputLengthAtLastWhitespace != -1)
                {
                    output[outputLengthAtLastWhitespace] = '\n';
                    visibleCount                         = i - lastWhitespaceIndex;
                    lastWhitespaceIndex                  = -1;
                    outputLengthAtLastWhitespace         = -1;
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
            CircularBuffer<VRRig.VelocityTime> history = rig.velocityHistoryList;
            if (history != null && history.Count > 0)
            {
                double ping = Math.Abs((history[0].time - PhotonNetwork.Time) * 1000);

                return (int)Math.Clamp(Math.Round(ping), 0, int.MaxValue);
            }
        }
        catch
        {
            // ignored
        }

        return int.MaxValue;
    }
}