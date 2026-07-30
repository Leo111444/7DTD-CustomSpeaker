using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CustomSpeaker
{
    /// <summary>
    /// Треки из папки Sounds мода. Порядок — алфавитный, индекс трека едет по сети,
    /// поэтому набор файлов должен совпадать у всех игроков.
    /// </summary>
    public static class ClipLibrary
    {
        private static readonly string[] SupportedExtensions = { ".ogg", ".wav", ".mp3", ".aiff", ".aif" };

        private static readonly List<string> trackNames = new List<string>();
        private static readonly List<string> trackPaths = new List<string>();
        private static readonly Dictionary<int, AudioClip> clips = new Dictionary<int, AudioClip>();
        private static readonly HashSet<int> loading = new HashSet<int>();

        public static int Count => trackNames.Count;

        public static void Scan()
        {
            trackNames.Clear();
            trackPaths.Clear();

            string dir = Path.Combine(ModApi.ModPath, "Sounds");
            if (!Directory.Exists(dir))
            {
                Log.Warning("[CustomSpeaker] Папка со звуками не найдена: " + dir);
                return;
            }

            foreach (string file in Directory.GetFiles(dir)
                         .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                         .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
            {
                trackPaths.Add(file);
                trackNames.Add(Path.GetFileNameWithoutExtension(file));
            }

            if (trackNames.Count == 0)
            {
                Log.Warning("[CustomSpeaker] В папке " + dir + " нет аудиофайлов (.ogg/.wav/.mp3).");
                return;
            }

            Log.Out("[CustomSpeaker] Треков найдено: " + trackNames.Count + " -> " + string.Join(", ", trackNames.ToArray()));
        }

        public static string NameOf(int index)
        {
            if (index < 0 || index >= trackNames.Count) return "?";
            return trackNames[index];
        }

        public static IReadOnlyList<string> Names => trackNames;

        /// <summary>Клип, если уже загружен. Иначе запускает загрузку и возвращает null.</summary>
        public static AudioClip Get(int index)
        {
            if (index < 0 || index >= trackPaths.Count)
            {
                return null;
            }
            if (clips.TryGetValue(index, out AudioClip clip) && clip != null)
            {
                return clip;
            }
            BeginLoad(index);
            return null;
        }

        /// <summary>Прогрев: грузим все треки заранее, чтобы динамик заиграл без паузы.</summary>
        public static void PreloadAll()
        {
            for (int i = 0; i < trackPaths.Count; i++)
            {
                BeginLoad(i);
            }
        }

        private static void BeginLoad(int index)
        {
            if (clips.ContainsKey(index) || loading.Contains(index))
            {
                return;
            }
            loading.Add(index);

            string path = trackPaths[index];
            AudioFileLoader.Instance.Load(path, "customspeaker/" + trackNames[index], clip =>
            {
                loading.Remove(index);
                if (clip == null)
                {
                    Log.Warning("[CustomSpeaker] Клип не загрузился: " + path);
                    return;
                }
                clips[index] = clip;
                Log.Out("[CustomSpeaker] Клип готов [" + index + "] " + trackNames[index] +
                        " (" + clip.length.ToString("0.0") + " c)");

                // Динамик мог включиться до того, как файл догрузился — досылаем звук.
                SpeakerAudio.OnClipLoaded(index);
            });
        }
    }
}
