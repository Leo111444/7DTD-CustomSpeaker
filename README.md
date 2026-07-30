# Custom Speaker

**A speaker block with your own sounds for 7 Days to Die 3.1**

![version](https://img.shields.io/badge/version-0.4.0-blue)
![game](https://img.shields.io/badge/7%20Days%20to%20Die-3.1-orange)
![type](https://img.shields.io/badge/type-server%20%2B%20client-lightgrey)
![eac](https://img.shields.io/badge/EAC-must%20be%20off-red)

[English](#english) · [Русский](#русский)

---

## English

An electric block that plays **your own audio files**. Everyone nearby hears it — the sound is synced over the network by the game's own broadcast mechanism.

**No Unity Editor and no AssetBundle needed:** drop an `.ogg` into the mod folder and it plays.

### Features

- Your own tracks in `.ogg`, `.wav`, `.mp3` — just files in a folder
- Up to 64 tracks, picked from a list window
- Audible radius: 30 / 50 / 70 meters
- Manual on/off switch in the block's radial menu
- Multiplayer ready: dedicated servers and co-op
- Settings are stored inside the block itself — saved in the world and visible to every player

### Installation

1. Copy the `CustomSpeaker` folder into `<game folder>\Mods\`
2. Put your audio files into `CustomSpeaker\Sounds\`
3. Launch the game with EAC disabled (the mod ships a dll)

> **Important:** the mod is required on the server and on every client, and the `Sounds` folder must be identical for all players — the network carries the track number, not the file name.

### How to use

The block is crafted at a workbench (4 forged iron + 3 electrical parts) or taken from creative, Science group. It needs power like any electric block.

Press **E** to open the radial menu:

| Entry | What it does |
| --- | --- |
| Turn on / off | Manual switch, independent of power |
| Settings | Window with the track list and radius |
| Take | Pick the block up (requires your own claim) |

Looking at the speaker shows the current track, radius and state.

### Settings

`config.json` in the mod folder (never overwritten on update):

| Option | Meaning |
| --- | --- |
| `Loop` | Loop the track while the block is powered |
| `Volume` | Volume `0..0.5` (the 0.5 cap is hard) |
| `DefaultRadius` | Fallback radius when the block cannot be read |

### Troubleshooting

Game console (**F1**):

```
cspeaker          — mod folder, list of found tracks, which clips got loaded
cspeaker rescan   — re-read the Sounds folder without restarting the game
```

Every message from the mod is tagged `[CustomSpeaker]` in the log.

### How it works

Two findings in the game's code carry the whole mod:

1. **Sound without a bundle.** `Audio.Manager.audioClipAssetCache` is checked inside `LoadAudio` before `DataLoader.LoadAsset`, and clips can be built at runtime from files on disk via `UnityWebRequestMultimedia`. The mod keeps its own clip library ([`ClipLibrary.cs`](src/ClipLibrary.cs)) and plays it through its own `AudioSource` ([`SpeakerAudio.cs`](src/SpeakerAudio.cs)), which is what gives exact control over the radius.
2. **Networking for free.** The block calls `Audio.Manager.BroadcastPlay/BroadcastStop`, the server relays the command to clients, and on the client a Harmony prefix intercepts it ([`Patches.cs`](src/Patches.cs)). The mod defines no network packages of its own.

Speaker state lives in `BlockValue`, so it is saved with the world and synced by vanilla means. There are exactly 8 bits available (`meta` and `meta2` are 4 bits each):

```
bits 2..7  — track index (0..63)
bits 0..1  — mode: 0..2 radius, 3 — manually switched off
```

Power state is not stored in the block: it comes from `TileEntityPowered.IsPowered`, which the game already sends to clients.

### Building from source

```
dotnet build CustomSpeaker.csproj
```

The project references the game assemblies from `7DaysToDie_Data\Managed` and `0Harmony.dll` from `Mods\0_TFP_Harmony`. Paths can be overridden:

```
dotnet build CustomSpeaker.csproj -p:GameManaged=... -p:HarmonyDir=... -p:DeployDir=...
```

After a build the dll, `ModInfo.xml`, `Config`, `Sounds` and `README.txt` are copied into `DeployDir` — by default straight into the game's `Mods` folder.

### Limitation: 64 tracks

The limit is hard — the track number is stored inside the block, and the game gives a block only 8 bits of data. Files past the 64th are listed but cannot be selected.

Want more? Make a **copy of the mod**. Copying the folder alone is not enough: the copies would share global names, so the game either complains or the mods start stepping on each other. Rename these in the copy:

| File | What to rename |
| --- | --- |
| `ModInfo.xml` | `Name`, `DisplayName` |
| `CustomSpeaker.csproj` | `AssemblyName`, `RootNamespace` |
| `src/*.cs` | namespace `CustomSpeaker`, class `BlockCSpeaker`, class `XUiC_SpeakerWindow`, `Patches.GroupPrefix`, Harmony id, commands in `ConsoleCmdCustomSpeaker` |
| `Config/blocks.xml` | block name `customSpeaker` and the `Class` value |
| `Config/recipes.xml` | recipe name |
| `Config/Localization.csv` | all keys |
| `Config/XUi_InGame/*` | window name `customSpeakerWindow` and `controller` |

Each copy reads its own `Sounds` folder — they never mix.

### Repository layout

```
src/                     mod code (C#)
Config/                  blocks, recipes, localization
Config/XUi_InGame/       settings window
Sounds/                  put tracks here (only a note is kept in the repo)
config.json              mod settings
README.txt               description shipped inside the mod folder
description_bbcode.txt   description for forum posts
```

---

## Русский

Электрический блок, который проигрывает **ваши собственные аудиофайлы**. Звук слышат все игроки рядом — синхронизация идёт штатным механизмом трансляции звука самой игры.

**Unity Editor и AssetBundle не нужны:** положил `.ogg` в папку мода — и он играет.

### Возможности

- Свои треки в форматах `.ogg`, `.wav`, `.mp3` — просто файлами в папке
- До 64 треков, выбор в окне со списком
- Радиус слышимости на выбор: 30 / 50 / 70 метров
- Ручной выключатель в круговом меню блока
- Мультиплеер: выделенный сервер и кооператив
- Настройки хранятся в самом блоке — сохраняются в мире и видны всем игрокам

### Установка

1. Скопировать папку `CustomSpeaker` в `<папка игры>\Mods\`
2. Положить свои аудиофайлы в `CustomSpeaker\Sounds\`
3. Запустить игру с отключённым EAC (мод содержит dll)

> **Важно:** мод нужен и серверу, и всем клиентам, а папка `Sounds` должна быть одинаковой у всех игроков — по сети передаётся номер трека, а не имя файла.

### Как пользоваться

Блок крафтится на верстаке (4 кованого железа + 3 электродетали) или берётся в креативе, группа Science. Ему нужно питание, как любому электроблоку.

Нажатие **E** открывает круговое меню:

| Пункт | Что делает |
| --- | --- |
| Включить / выключить | Ручной выключатель, независимо от питания |
| Настройки | Окно со списком треков и радиусом |
| Забрать | Снять блок (нужен свой клейм) |

Наведение на динамик показывает текущий трек, радиус и состояние.

### Настройки

`config.json` в папке мода (не перезаписывается при обновлении):

| Параметр | Значение |
| --- | --- |
| `Loop` | Зациклить трек, пока подано питание |
| `Volume` | Громкость `0..0.5` (потолок 0.5 жёсткий) |
| `DefaultRadius` | Запасной радиус, если блок не удалось прочитать |

### Диагностика

Консоль игры (**F1**):

```
cspeaker          — папка мода, список найденных треков, какие клипы загрузились
cspeaker rescan   — перечитать папку Sounds без перезапуска игры
```

Все сообщения мода в логе помечены `[CustomSpeaker]`.

### Как это устроено

Две опоры, найденные в коде игры:

1. **Звук без бандла.** `Audio.Manager.audioClipAssetCache` проверяется в `LoadAudio` перед `DataLoader.LoadAsset`, а клипы можно собрать в рантайме из файлов на диске через `UnityWebRequestMultimedia`. Мод держит свою библиотеку клипов ([`ClipLibrary.cs`](src/ClipLibrary.cs)) и играет их собственным `AudioSource` ([`SpeakerAudio.cs`](src/SpeakerAudio.cs)) — это даёт точный контроль над радиусом.
2. **Сеть бесплатно.** Блок зовёт `Audio.Manager.BroadcastPlay/BroadcastStop`, сервер рассылает команду клиентам, а на клиенте её перехватывает Harmony-префикс ([`Patches.cs`](src/Patches.cs)). Своих сетевых пакетов мод не заводит.

Состояние динамика живёт в `BlockValue`, поэтому сохраняется в мире и синхронизируется ванильно. Места там ровно 8 бит (`meta` и `meta2` — по 4 бита каждое):

```
биты 2..7  — индекс трека (0..63)
биты 0..1  — режим: 0..2 радиус, 3 — выключен вручную
```

Питание в блоке не хранится: его отдаёт `TileEntityPowered.IsPowered`, который игра и так шлёт клиентам.

### Сборка из исходников

```
dotnet build CustomSpeaker.csproj
```

Проект ссылается на сборки игры из `7DaysToDie_Data\Managed` и `0Harmony.dll` из `Mods\0_TFP_Harmony`. Пути переопределяются свойствами:

```
dotnet build CustomSpeaker.csproj -p:GameManaged=... -p:HarmonyDir=... -p:DeployDir=...
```

После сборки dll, `ModInfo.xml`, `Config`, `Sounds` и `README.txt` копируются в `DeployDir` — по умолчанию прямо в папку `Mods` игры.

### Ограничение: 64 трека

Лимит жёсткий — номер трека хранится внутри блока, а под данные блока игра выделяет всего 8 бит. Файлы сверх 64-го видны в списке, но выбрать их не получится.

Хотите больше — сделайте **копию мода**. Просто скопировать папку недостаточно: у копий совпадут глобальные имена, и игра либо ругнётся, либо моды начнут мешать друг другу. В копии нужно переименовать:

| Файл | Что менять |
| --- | --- |
| `ModInfo.xml` | `Name`, `DisplayName` |
| `CustomSpeaker.csproj` | `AssemblyName`, `RootNamespace` |
| `src/*.cs` | namespace `CustomSpeaker`, класс `BlockCSpeaker`, класс `XUiC_SpeakerWindow`, `Patches.GroupPrefix`, id Harmony, команды в `ConsoleCmdCustomSpeaker` |
| `Config/blocks.xml` | имя блока `customSpeaker` и значение `Class` |
| `Config/recipes.xml` | имя рецепта |
| `Config/Localization.csv` | все ключи |
| `Config/XUi_InGame/*` | имя окна `customSpeakerWindow` и `controller` |

Папку `Sounds` каждая копия читает свою — они не смешиваются.

### Структура репозитория

```
src/                     код мода (C#)
Config/                  блоки, рецепты, локализация
Config/XUi_InGame/       окно настроек
Sounds/                  сюда кладутся треки (в репозитории только памятка)
config.json              настройки мода
README.txt               описание, которое едет в папку мода
description_bbcode.txt   описание для публикации на форумах
```

---

**Author / Автор:** SkyLett
