using System;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace CustomSpeaker
{
    /// <summary>Точка входа мода: 7DTD зовёт InitMod при загрузке dll.</summary>
    public class ModApi : IModApi
    {
        public static string ModPath;
        public static ModConfig Config = new ModConfig();

        public void InitMod(Mod _modInstance)
        {
            try
            {
                ModPath = _modInstance.Path;
                Config = ModConfig.Load(Path.Combine(ModPath, "config.json"));

                // Дедику аудио не нужно: блок и его логика питания работают и без звука.
                if (GameManager.IsDedicatedServer)
                {
                    Log.Out("[CustomSpeaker] Дедик: аудио-часть пропущена.");
                    return;
                }

                new Harmony("com.skylett.customspeaker").PatchAll(Assembly.GetExecutingAssembly());

                ClipLibrary.Scan();
                ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
                ModEvents.WorldShuttingDown.RegisterHandler(OnWorldShuttingDown);

                Log.Out("[CustomSpeaker] Загружен, папка мода: " + ModPath);
            }
            catch (Exception e)
            {
                Log.Error("[CustomSpeaker] InitMod упал: " + e);
            }
        }

        private static void OnGameStartDone(ref ModEvents.SGameStartDoneData _data)
        {
            try
            {
                // Пересканируем: файлы могли положить после запуска игры.
                ClipLibrary.Scan();
                ClipLibrary.PreloadAll();
            }
            catch (Exception e)
            {
                Log.Error("[CustomSpeaker] Подготовка треков упала: " + e);
            }
        }

        private static void OnWorldShuttingDown(ref ModEvents.SWorldShuttingDownData _data)
        {
            try
            {
                SpeakerAudio.StopAll();
            }
            catch (Exception e)
            {
                Log.Error("[CustomSpeaker] Остановка динамиков упала: " + e);
            }
        }
    }
}
