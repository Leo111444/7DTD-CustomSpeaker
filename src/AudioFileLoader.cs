using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace CustomSpeaker
{
    /// <summary>
    /// Грузит аудиофайл с диска в AudioClip. Живёт на своём GameObject, потому что
    /// UnityWebRequest требует корутину, а значит MonoBehaviour.
    /// </summary>
    public class AudioFileLoader : MonoBehaviour
    {
        private static AudioFileLoader instance;

        public static AudioFileLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("CustomSpeakerAudioLoader");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    instance = go.AddComponent<AudioFileLoader>();
                }
                return instance;
            }
        }

        public void Load(string filePath, string clipName, Action<AudioClip> onLoaded)
        {
            StartCoroutine(LoadRoutine(filePath, clipName, onLoaded));
        }

        private IEnumerator LoadRoutine(string filePath, string clipName, Action<AudioClip> onLoaded)
        {
            string url = new Uri(filePath).AbsoluteUri;
            AudioType type = GuessAudioType(filePath);

            using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(url, type))
            {
                var handler = req.downloadHandler as DownloadHandlerAudioClip;
                if (handler != null)
                {
                    // Полная загрузка в память: клип переживает перезаходы и играется многократно.
                    handler.streamAudio = false;
                }

                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Log.Error("[CustomSpeaker] Не смог загрузить " + filePath + ": " + req.error);
                    onLoaded?.Invoke(null);
                    yield break;
                }

                AudioClip clip = null;
                try
                {
                    clip = DownloadHandlerAudioClip.GetContent(req);
                }
                catch (Exception e)
                {
                    Log.Error("[CustomSpeaker] Файл " + filePath + " не распознан как аудио: " + e.Message);
                }

                if (clip == null)
                {
                    onLoaded?.Invoke(null);
                    yield break;
                }

                clip.name = clipName;
                UnityEngine.Object.DontDestroyOnLoad(clip);
                onLoaded?.Invoke(clip);
            }
        }

        private static AudioType GuessAudioType(string path)
        {
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            switch (ext)
            {
                case ".ogg": return AudioType.OGGVORBIS;
                case ".mp3": return AudioType.MPEG;
                case ".wav": return AudioType.WAV;
                case ".aiff":
                case ".aif": return AudioType.AIFF;
                default: return AudioType.UNKNOWN;
            }
        }
    }
}
