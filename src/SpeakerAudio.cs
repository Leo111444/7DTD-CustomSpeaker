using System.Collections.Generic;
using UnityEngine;

namespace CustomSpeaker
{
    /// <summary>
    /// Клиентское воспроизведение динамиков. Своё, а не ванильное, ради контроля
    /// над радиусом слышимости и гарантированной остановки.
    /// Команду "играть/стоп" приносит ванильная трансляция звука (см. <see cref="Patches"/>).
    /// </summary>
    public static class SpeakerAudio
    {
        private class Playing
        {
            public GameObject go;
            public AudioSource src;
            public int trackIndex;
        }

        private static readonly Dictionary<Vector3i, Playing> playing = new Dictionary<Vector3i, Playing>();

        /// <summary>Блоки, ждущие догрузки своего трека: индекс трека → позиции.</summary>
        private static readonly Dictionary<int, HashSet<Vector3i>> pending = new Dictionary<int, HashSet<Vector3i>>();

        public static void Play(Vector3 worldPos, int trackIndex)
        {
            Vector3i blockPos = ToBlockPos(worldPos);
            Stop(worldPos);

            if (trackIndex < 0 || (ClipLibrary.Count > 0 && trackIndex >= ClipLibrary.Count))
            {
                Log.Warning("[CustomSpeaker] У динамика в " + blockPos + " выбран трек #" + trackIndex +
                            ", а найдено только " + ClipLibrary.Count + " — звука не будет.");
                return;
            }

            AudioClip clip = ClipLibrary.Get(trackIndex);
            if (clip == null)
            {
                // Файл ещё грузится (или его нет) — включим, когда будет готов.
                if (!pending.TryGetValue(trackIndex, out HashSet<Vector3i> set))
                {
                    set = new HashSet<Vector3i>();
                    pending[trackIndex] = set;
                }
                set.Add(blockPos);
                return;
            }

            float radius = RadiusAt(blockPos);

            var go = new GameObject("CustomSpeaker_" + blockPos);
            Transform parent = Audio.Manager.Instance != null ? Audio.Manager.Instance.PositionalSoundsPlayingT : null;
            if (parent != null)
            {
                go.transform.SetParent(parent, worldPositionStays: false);
            }
            go.transform.position = worldPos - Origin.position;

            AudioSource src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = ModApi.Config.Loop;
            src.playOnAwake = false;
            src.volume = Mathf.Clamp(ModApi.Config.Volume, 0f, ModConfig.MaxVolume);
            src.spatialBlend = 1f;                       // полностью 3D
            src.rolloffMode = AudioRolloffMode.Linear;   // предсказуемый радиус
            src.minDistance = Mathf.Max(1f, radius * 0.15f);
            src.maxDistance = radius;
            src.dopplerLevel = 0f;
            src.Play();

            playing[blockPos] = new Playing { go = go, src = src, trackIndex = trackIndex };
        }

        public static void Stop(Vector3 worldPos)
        {
            Vector3i blockPos = ToBlockPos(worldPos);

            foreach (KeyValuePair<int, HashSet<Vector3i>> kv in pending)
            {
                kv.Value.Remove(blockPos);
            }

            if (!playing.TryGetValue(blockPos, out Playing p))
            {
                return;
            }
            playing.Remove(blockPos);

            if (p.src != null) p.src.Stop();
            if (p.go != null) Object.Destroy(p.go);
        }

        public static void StopAll()
        {
            foreach (KeyValuePair<Vector3i, Playing> kv in playing)
            {
                if (kv.Value.src != null) kv.Value.src.Stop();
                if (kv.Value.go != null) Object.Destroy(kv.Value.go);
            }
            playing.Clear();
            pending.Clear();
        }

        /// <summary>Трек догрузился — запускаем динамики, которые его ждали.</summary>
        public static void OnClipLoaded(int trackIndex)
        {
            if (!pending.TryGetValue(trackIndex, out HashSet<Vector3i> set) || set.Count == 0)
            {
                return;
            }
            var positions = new List<Vector3i>(set);
            set.Clear();

            foreach (Vector3i pos in positions)
            {
                Play(pos.ToVector3(), trackIndex);
            }
        }

        /// <summary>Радиус берём из самого блока (meta2), чтобы у всех был одинаковый.</summary>
        private static float RadiusAt(Vector3i blockPos)
        {
            try
            {
                World world = GameManager.Instance != null ? GameManager.Instance.World : null;
                if (world != null)
                {
                    BlockValue bv = world.GetBlock(blockPos);
                    if (bv.Block is BlockCSpeaker)
                    {
                        return BlockCSpeaker.RadiusFromMeta(bv);
                    }
                }
            }
            catch
            {
                // блока может уже не быть — упадём на значение по умолчанию
            }
            return ModApi.Config.DefaultRadius;
        }

        private static Vector3i ToBlockPos(Vector3 worldPos)
        {
            return new Vector3i(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), Mathf.FloorToInt(worldPos.z));
        }
    }
}
