using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace CustomSpeaker
{
    /// <summary>Настройки мода из config.json рядом с dll.</summary>
    public class ModConfig
    {
        /// <summary>Зациклить трек, пока на блок подано питание.</summary>
        public bool Loop = true;

        /// <summary>Потолок громкости: динамик не должен перекрикивать игру.</summary>
        public const float MaxVolume = 0.5f;

        /// <summary>Громкость источника, 0..<see cref="MaxVolume"/>.</summary>
        public float Volume = MaxVolume;

        /// <summary>Радиус слышимости для только что поставленного динамика, м.</summary>
        public float DefaultRadius = 30f;

        public static ModConfig Load(string path)
        {
            var cfg = new ModConfig();
            try
            {
                if (!File.Exists(path))
                {
                    Log.Warning("[CustomSpeaker] config.json не найден, беру значения по умолчанию: " + path);
                    return cfg;
                }

                JObject o = JObject.Parse(File.ReadAllText(path));
                if (o["Loop"] != null) cfg.Loop = (bool)o["Loop"];
                if (o["Volume"] != null) cfg.Volume = Math.Min(Math.Max((float)o["Volume"], 0f), MaxVolume);
                if (o["DefaultRadius"] != null) cfg.DefaultRadius = (float)o["DefaultRadius"];
            }
            catch (Exception e)
            {
                Log.Error("[CustomSpeaker] Не смог прочитать config.json: " + e.Message);
            }
            return cfg;
        }
    }
}
