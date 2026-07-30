using System;
using HarmonyLib;
using UnityEngine;

namespace CustomSpeaker
{
    /// <summary>
    /// Перехват ванильного проигрывания для наших звуковых групп.
    ///
    /// Сеть остаётся ванильной: блок зовёт Audio.Manager.BroadcastPlay/BroadcastStop,
    /// сервер рассылает команду клиентам, а у клиента вместо ванильного пайплайна
    /// (которому нужен sounds.xml и бандл) отрабатывает наш SpeakerAudio.
    /// </summary>
    public static class Patches
    {
        /// <summary>Группы вида customSpeakerTrack7 — номер это индекс трека в ClipLibrary.</summary>
        public const string GroupPrefix = "customSpeakerTrack";

        public static string GroupName(int trackIndex) => GroupPrefix + trackIndex;

        public static bool TryParseGroup(string soundGroupName, out int trackIndex)
        {
            trackIndex = -1;
            if (string.IsNullOrEmpty(soundGroupName) || !soundGroupName.StartsWith(GroupPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return int.TryParse(soundGroupName.Substring(GroupPrefix.Length), out trackIndex);
        }
    }

    [HarmonyPatch(typeof(Audio.Manager), nameof(Audio.Manager.Play),
        new[] { typeof(Vector3), typeof(string), typeof(int), typeof(bool), typeof(float) })]
    public static class Patch_Manager_Play
    {
        public static bool Prefix(Vector3 position, string soundGroupName, ref Audio.Handle __result)
        {
            if (!Patches.TryParseGroup(soundGroupName, out int trackIndex))
            {
                return true;
            }

            try
            {
                SpeakerAudio.Play(position, trackIndex);
            }
            catch (Exception e)
            {
                Log.Error("[CustomSpeaker] Play упал: " + e);
            }

            __result = null;
            return false;
        }
    }

    [HarmonyPatch(typeof(Audio.Manager), nameof(Audio.Manager.Stop),
        new[] { typeof(Vector3), typeof(string) })]
    public static class Patch_Manager_Stop
    {
        public static bool Prefix(Vector3 position, string soundGroupName)
        {
            if (!Patches.TryParseGroup(soundGroupName, out int _))
            {
                return true;
            }

            try
            {
                SpeakerAudio.Stop(position);
            }
            catch (Exception e)
            {
                Log.Error("[CustomSpeaker] Stop упал: " + e);
            }
            return false;
        }
    }
}
