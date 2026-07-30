using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace CustomSpeaker
{
    /// <summary>
    /// Динамик со своими треками. Логика питания повторяет ванильный BlockSpeaker,
    /// сверху — радиальное меню (вкл/выкл, настройки) и окно со списком треков.
    ///
    /// Состояние живёт в BlockValue, поэтому сохраняется и синхронизируется ванильно.
    /// Места там ровно 8 бит (meta и meta2 — по 4 бита, это meta2and1):
    ///   биты 2..7 — индекс трека (0..63);
    ///   биты 0..1 — режим: 0..2 радиус из <see cref="RadiusModes"/>, 3 — выключен вручную.
    /// Питание в блоке не храним: его отдаёт TileEntityPowered.IsPowered, который
    /// игра и так синхронизирует с клиентами.
    /// </summary>
    [Preserve]
    public class BlockCSpeaker : BlockPowered
    {
        /// <summary>Радиусы слышимости, между которыми переключают кнопки в окне.</summary>
        public static readonly int[] RadiusModes = { 30, 50, 70 };

        /// <summary>Режим «выключен вручную».</summary>
        public const int MutedMode = 3;

        public const int MaxTracks = 64;

        private readonly BlockActivationCommand[] speakerCmds =
        {
            new BlockActivationCommand("cspeakerToggle", "electric_switch", true),
            new BlockActivationCommand("cspeakerMenu", "more_options", true),
            new BlockActivationCommand("take", "hand", true)
        };

        public BlockCSpeaker()
        {
            HasTileEntity = true;
        }

        public override TileEntityPowered CreateTileEntity(Chunk chunk)
        {
            return new TileEntityPoweredBlock(chunk)
            {
                PowerItemType = PowerItem.PowerItemTypes.Consumer
            };
        }

        // --- состояние в meta ---

        public static int TrackFromMeta(BlockValue bv) => (bv.meta2and1 >> 2) & 0x3F;

        public static int ModeFromMeta(BlockValue bv) => bv.meta2and1 & 0x03;

        public static bool IsMuted(BlockValue bv) => ModeFromMeta(bv) == MutedMode;

        public static bool IsPowered(WorldBase _world, Vector3i _blockPos)
        {
            if (_world == null) return false;
            return _world.GetTileEntity(_blockPos) is TileEntityPowered te && te.IsPowered;
        }

        public static float RadiusFromMeta(BlockValue bv)
        {
            int mode = ModeFromMeta(bv);
            if (mode < 0 || mode >= RadiusModes.Length) return RadiusModes[0];
            return RadiusModes[mode];
        }

        private static bool ShouldPlay(WorldBase _world, Vector3i _blockPos, BlockValue bv)
        {
            return !IsMuted(bv) && IsPowered(_world, _blockPos);
        }

        // --- питание ---

        public override bool ActivateBlock(WorldBase _world, Vector3i _blockPos, BlockValue _blockValue, bool isOn, bool isPowered)
        {
            string group = Patches.GroupName(TrackFromMeta(_blockValue));
            if (isOn && !IsMuted(_blockValue))
            {
                Audio.Manager.BroadcastPlay(_blockPos.ToVector3(), group);
            }
            else
            {
                Audio.Manager.BroadcastStop(_blockPos.ToVector3(), group);
            }
            return true;
        }

        public override void OnBlockLoaded(WorldBase _world, Vector3i _blockPos, BlockValue _blockValue)
        {
            base.OnBlockLoaded(_world, _blockPos, _blockValue);
            if ((TileEntityPoweredBlock)_world.GetTileEntity(_blockPos) == null)
            {
                return;
            }
            // Локально: чанк только что подгрузился, звук ещё не играет.
            if (ShouldPlay(_world, _blockPos, _blockValue))
            {
                Audio.Manager.Play(_blockPos.ToVector3(), Patches.GroupName(TrackFromMeta(_blockValue)));
            }
            else
            {
                Audio.Manager.Stop(_blockPos.ToVector3(), Patches.GroupName(TrackFromMeta(_blockValue)));
            }
        }

        public override void OnBlockUnloaded(WorldBase world, Vector3i blockPos, BlockValue blockValue)
        {
            base.OnBlockUnloaded(world, blockPos, blockValue);
            Audio.Manager.Stop(blockPos.ToVector3(), Patches.GroupName(TrackFromMeta(blockValue)));
        }

        public override void OnBlockRemoved(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
        {
            base.OnBlockRemoved(world, _chunk, _blockPos, _blockValue);
            Audio.Manager.BroadcastStop(_blockPos.ToVector3(), Patches.GroupName(TrackFromMeta(_blockValue)));
        }

        // --- меню ---

        public override bool HasBlockActivationCommands(WorldBase _world, BlockValue _blockValue, Vector3i _blockPos, EntityAlive _entityFocusing)
        {
            return true;
        }

        public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, Vector3i _blockPos, EntityAlive _entityFocusing)
        {
            // Управление динамиком доступно всем: клейм ограничивает только «забрать»,
            // как у ванильных блоков питания.
            speakerCmds[0].enabled = true;
            speakerCmds[1].enabled = true;
            speakerCmds[2].enabled = TakeDelay > 0f &&
                _world.IsMyLandProtectedBlock(_blockPos, _world.GetGameManager().GetPersistentLocalPlayer());
            return speakerCmds;
        }

        public override string GetActivationText(WorldBase _world, BlockValue _blockValue, Vector3i _blockPos, EntityAlive _entityFocusing)
        {
            string track = ClipLibrary.NameOf(TrackFromMeta(_blockValue));
            string state = IsMuted(_blockValue)
                ? Localization.Get("cspeakerStateOff")
                : (IsPowered(_world, _blockPos) ? Localization.Get("cspeakerStatePlaying") : Localization.Get("cspeakerStateNoPower"));

            return string.Format(Localization.Get("cspeakerActivationText"),
                track, RadiusFromMeta(_blockValue).ToString("0"), state);
        }

        public override bool OnBlockActivated(string _commandName, WorldBase _world, Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
        {
            if (_blockValue.ischild)
            {
                Vector3i parentPos = _blockValue.Block.multiBlockPos.GetParentPos(_blockPos, _blockValue);
                return OnBlockActivated(_commandName, _world, parentPos, _world.GetBlock(parentPos), _player);
            }

            switch (_commandName)
            {
                case "cspeakerToggle":
                    ApplyMuteToggle(_world, _blockPos);
                    return true;

                case "cspeakerMenu":
                    XUiC_SpeakerWindow.Show(_player.PlayerUI.xui, _blockPos);
                    return true;

                case "take":
                    takeItemWithTimer(_blockPos, _blockValue, _player, TakeDelay);
                    return true;
            }
            return false;
        }

        // --- действия (зовёт и радиальное меню, и окно настроек) ---

        public static void ApplyMuteToggle(WorldBase _world, Vector3i _blockPos)
        {
            BlockValue bv = _world.GetBlock(_blockPos);
            if (!(bv.Block is BlockCSpeaker)) return;

            // Выключение — это отдельный режим; при включении возвращаем первый радиус.
            int mode = IsMuted(bv) ? 0 : MutedMode;
            WriteMeta(_world, _blockPos, ref bv, TrackFromMeta(bv), mode);
            Broadcast(_world, _blockPos, bv);
        }

        public static void ApplyTrack(WorldBase _world, Vector3i _blockPos, int _trackIndex)
        {
            BlockValue bv = _world.GetBlock(_blockPos);
            if (!(bv.Block is BlockCSpeaker)) return;

            int newTrack = Mathf.Clamp(_trackIndex, 0, MaxTracks - 1);
            int oldTrack = TrackFromMeta(bv);
            if (newTrack == oldTrack) return;

            // Сначала глушим старый трек, иначе на клиентах останется висеть его источник.
            if (ShouldPlay(_world, _blockPos, bv))
            {
                Audio.Manager.BroadcastStop(_blockPos.ToVector3(), Patches.GroupName(oldTrack));
            }

            WriteMeta(_world, _blockPos, ref bv, newTrack, ModeFromMeta(bv));
            Broadcast(_world, _blockPos, bv);
        }

        /// <summary>Переключает радиус на соседнее значение из <see cref="RadiusModes"/>.</summary>
        public static void ApplyRadiusStep(WorldBase _world, Vector3i _blockPos, int _direction)
        {
            BlockValue bv = _world.GetBlock(_blockPos);
            if (!(bv.Block is BlockCSpeaker)) return;

            int mode = ModeFromMeta(bv);
            if (mode == MutedMode) return;   // выключенному динамику радиус ни к чему

            int newMode = Mathf.Clamp(mode + _direction, 0, RadiusModes.Length - 1);
            if (newMode == mode) return;

            bool wasPlaying = ShouldPlay(_world, _blockPos, bv);
            int track = TrackFromMeta(bv);

            WriteMeta(_world, _blockPos, ref bv, track, newMode);

            // Радиус применяется при запуске источника — перезапускаем, если играет.
            if (wasPlaying)
            {
                Audio.Manager.BroadcastStop(_blockPos.ToVector3(), Patches.GroupName(track));
                Audio.Manager.BroadcastPlay(_blockPos.ToVector3(), Patches.GroupName(track));
            }
        }

        private static void WriteMeta(WorldBase _world, Vector3i _blockPos, ref BlockValue bv, int _track, int _mode)
        {
            bv.meta2and1 = (byte)(((_track & 0x3F) << 2) | (_mode & 0x03));
            _world.SetBlockRPC(_blockPos, bv);
        }

        private static void Broadcast(WorldBase _world, Vector3i _blockPos, BlockValue _blockValue)
        {
            try
            {
                string group = Patches.GroupName(TrackFromMeta(_blockValue));
                if (ShouldPlay(_world, _blockPos, _blockValue))
                {
                    Audio.Manager.BroadcastPlay(_blockPos.ToVector3(), group);
                }
                else
                {
                    Audio.Manager.BroadcastStop(_blockPos.ToVector3(), group);
                }
            }
            catch (Exception e)
            {
                Log.Error("[CustomSpeaker] Broadcast упал: " + e);
            }
        }
    }
}
