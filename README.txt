CUSTOM SPEAKER — a speaker block with your own sounds
7 Days to Die 3.1
=====================================================

WHAT IT DOES
------------
Adds an electric block, "Custom Speaker". It plays your own audio files,
and everyone nearby hears them — the sound is synced over the network by
the game's own broadcast mechanism.

No Unity Editor and no AssetBundle needed: the files are read straight
from disk in game.

INSTALLATION
------------
1. Copy the CustomSpeaker folder into <game folder>\Mods\
2. Put your audio files into CustomSpeaker\Sounds\
3. Launch the game with EAC disabled (the mod ships a dll).

The mod is required on the server AND on every client. The Sounds folder
must be identical for all players: the network carries the track number,
not the file name, so different file sets mean different sounds.

AUDIO FORMATS
-------------
.ogg (recommended), .wav, .mp3, .aiff

The file name without extension is the track name in the menu. Tracks are
sorted alphabetically.

HOW TO USE
----------
The block is crafted at a workbench (4 forged iron + 3 electrical parts)
or taken from creative, Science group. It needs power like any electric
block: generator, battery bank or solar panel.

Press E on the block to open the radial menu:
  - Turn on / off — manual switch;
  - Settings     — window with the track list and radius;
  - Take         — pick the block up (requires your own claim).

The settings window offers the track table, the audible radius
(30 / 50 / 70 m) and the same on/off button.

Looking at the block shows the current track, radius and state.

SETTINGS (config.json)
----------------------
  Loop          — loop the track while powered (default true)
  Volume        — volume 0..0.5 (the 0.5 cap is hard)
  DefaultRadius — fallback radius when the block cannot be read

config.json is not overwritten when the mod is updated.

TROUBLESHOOTING
---------------
Game console (F1):
  cspeaker          — mod folder, list of found tracks, which clips loaded
  cspeaker rescan   — re-read the Sounds folder without restarting

Every message from the mod is tagged [CustomSpeaker] in the log.

LIMITATION: 64 TRACKS
---------------------
64 files max in the Sounds folder. The limit is hard: the track number is
stored inside the block itself, and the game gives a block only 8 bits of
data — 6 went to the track number, 2 to the radius.

Files past the 64th are listed but cannot be selected.

Want more? Make a copy of the mod — see the full rename checklist in the
Russian section below or in the repository README.

Source code: https://github.com/Leo111444/7DTD-CustomSpeaker

Author: SkyLett


=====================================================================


CUSTOM SPEAKER — динамик со своими звуками
7 Days to Die 3.1
=====================================================

ЧТО ДЕЛАЕТ
----------
Добавляет электрический блок «Кастомный динамик». Он проигрывает ваши
собственные аудиофайлы, и звук слышат все игроки рядом — синхронизация
идёт по сети штатным механизмом игры.

Unity Editor и сборка AssetBundle НЕ нужны: файлы читаются с диска
прямо в игре.

УСТАНОВКА
---------
1. Скопировать папку CustomSpeaker в <папка игры>\Mods\
2. Положить свои аудиофайлы в CustomSpeaker\Sounds\
3. Запустить игру с отключённым EAC (мод содержит dll).

Мод нужен И серверу, И всем клиентам. Папка Sounds должна быть
одинаковой у всех: по сети передаётся номер трека, а не имя файла,
поэтому при разных наборах файлов игроки услышат разные звуки.

ФОРМАТЫ ЗВУКА
-------------
.ogg (рекомендуется), .wav, .mp3, .aiff

Имя файла без расширения — это имя трека в меню. Порядок треков
алфавитный.

КАК ПОЛЬЗОВАТЬСЯ
----------------
Блок крафтится на верстаке (4 кованого железа + 3 электродетали) или
берётся в креативе (группа Science).

Динамику нужно питание — генератор, аккумулятор или солнечная батарея,
как любому электроблоку.

Нажатие E на блоке открывает круговое меню:
  - Включить / выключить — ручной выключатель;
  - Настройки           — окно со списком треков и радиусом;
  - Забрать             — снять блок (нужен свой клейм).

В окне настроек: выбор трека из таблицы, переключение радиуса
слышимости (30 / 50 / 70 м) и та же кнопка включения.

Наведение на блок показывает текущий трек, радиус и состояние.

НАСТРОЙКИ (config.json)
-----------------------
  Loop          — зациклить трек, пока подано питание (по умолчанию true)
  Volume        — громкость 0..0.5 (жёсткий потолок 0.5)
  DefaultRadius — запасной радиус, если блок не удалось прочитать

Файл config.json не перезаписывается при обновлении мода.

ДИАГНОСТИКА
-----------
Консоль игры (F1):
  cspeaker          — папка мода, список найденных треков, какие клипы
                      загрузились
  cspeaker rescan   — перечитать папку Sounds без перезапуска игры

Все сообщения мода в логе помечены [CustomSpeaker].

ОГРАНИЧЕНИЕ: 64 ТРЕКА
---------------------
Максимум 64 файла в папке Sounds. Лимит жёсткий: номер трека хранится
внутри самого блока, а игра выделяет под данные блока всего 8 бит —
6 из них ушли под номер трека, 2 под радиус.

Файлы сверх 64-го будут видны в списке, но выбрать их не получится.

Хотите больше — сделайте копию мода. Просто скопировать папку
недостаточно: у копий совпадут глобальные имена, и игра либо ругнётся,
либо оба мода начнут мешать друг другу. В копии нужно переименовать:

  1. ModInfo.xml            — Name и DisplayName
  2. CustomSpeaker.csproj   — AssemblyName и RootNamespace
  3. src\*.cs               — namespace CustomSpeaker
                              класс BlockCSpeaker
                              класс XUiC_SpeakerWindow
                              Patches.GroupPrefix ("customSpeakerTrack")
                              id Harmony ("com.skylett.customspeaker")
                              команды в ConsoleCmdCustomSpeaker
  4. Config\blocks.xml      — имя блока customSpeaker и значение Class
  5. Config\recipes.xml     — имя рецепта
  6. Config\Localization.csv— все ключи
  7. Config\XUi_InGame\*    — имя окна customSpeakerWindow и controller

Папку Sounds каждая копия читает свою собственную — они не смешиваются.

Исходники: https://github.com/Leo111444/7DTD-CustomSpeaker

Автор: SkyLett
