using System.Collections.Generic;

namespace CustomSpeaker
{
    /// <summary>Диагностика: консольная команда "cspeaker" (F1) показывает, что видит клиент.</summary>
    public class ConsoleCmdCustomSpeaker : ConsoleCmdAbstract
    {
        public override string[] getCommands() => new[] { "cspeaker", "customspeaker" };

        public override string getDescription() => "Custom Speaker: список треков и состояние мода";

        public override string getHelp() =>
            "cspeaker            — путь мода, найденные треки, загруженные клипы\n" +
            "cspeaker rescan     — пересканировать папку Sounds и перезагрузить треки";

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            if (_params.Count > 0 && _params[0] == "rescan")
            {
                ClipLibrary.Scan();
                ClipLibrary.PreloadAll();
                SdtdConsole.Instance.Output("[CustomSpeaker] Пересканировано, треков: " + ClipLibrary.Count);
                return;
            }

            SdtdConsole.Instance.Output("[CustomSpeaker] Папка мода: " + (ModApi.ModPath ?? "?"));
            SdtdConsole.Instance.Output("[CustomSpeaker] Дедик: " + GameManager.IsDedicatedServer +
                                        ", громкость: " + ModApi.Config.Volume +
                                        ", радиус по умолчанию: " + ModApi.Config.DefaultRadius + " м");
            SdtdConsole.Instance.Output("[CustomSpeaker] Треков найдено: " + ClipLibrary.Count);

            for (int i = 0; i < ClipLibrary.Count; i++)
            {
                bool loaded = ClipLibrary.Get(i) != null;
                SdtdConsole.Instance.Output("  [" + i + "] " + ClipLibrary.NameOf(i) + (loaded ? "  — загружен" : "  — не загружен"));
            }

            if (ClipLibrary.Count == 0)
            {
                SdtdConsole.Instance.Output("[CustomSpeaker] Положи .ogg файлы в <папка мода>\\Sounds и выполни 'cspeaker rescan'.");
            }
        }
    }
}
