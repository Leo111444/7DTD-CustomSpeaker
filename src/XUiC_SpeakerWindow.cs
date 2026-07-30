using System;
using UnityEngine.Scripting;

namespace CustomSpeaker
{
    /// <summary>
    /// Окно настроек динамика: таблица треков, радиус, вкл/выкл.
    /// Все изменения пишутся в BlockValue, поэтому расходятся по сети ванильно.
    /// </summary>
    [Preserve]
    public class XUiC_SpeakerWindow : XUiController
    {
        /// <summary>Сколько строк списка нарисовано в windows.xml.</summary>
        public const int Rows = 10;

        public static string ID = "";

        private Vector3i blockPos;
        private int page;

        public override void Init()
        {
            base.Init();
            ID = WindowGroup.Id;

            for (int i = 0; i < Rows; i++)
            {
                int row = i;
                Hook("track" + i, () => SelectTrack(page * Rows + row));
            }

            Hook("powerToggle", TogglePower);
            Hook("radiusMinus", () => ChangeRadius(-1));
            Hook("radiusPlus", () => ChangeRadius(1));
            Hook("pagePrev", () => ChangePage(-1));
            Hook("pageNext", () => ChangePage(1));
            Hook("closeButton", Close);
        }

        private void Hook(string childName, Action action)
        {
            XUiController child = GetChildById(childName);
            if (child == null)
            {
                Log.Warning("[CustomSpeaker] В окне нет элемента '" + childName + "'");
                return;
            }
            child.OnPress += (_sender, _mouseButton) =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Log.Error("[CustomSpeaker] Обработчик '" + childName + "' упал: " + e);
                }
                IsDirty = true;
            };
        }

        public static void Show(XUi _xui, Vector3i _blockPos)
        {
            var group = (XUiWindowGroup)_xui.playerUI.windowManager.GetWindow(ID);
            XUiC_SpeakerWindow window = group.Controller.GetChildByType<XUiC_SpeakerWindow>();
            window.blockPos = _blockPos;
            window.page = 0;
            window.RefreshBindings();
            _xui.playerUI.windowManager.Open(ID, _bModal: true);
        }

        // --- действия ---

        private void SelectTrack(int trackIndex)
        {
            Log.Out("[CustomSpeaker] Выбор трека #" + trackIndex + " (страница " + page + ") для " + blockPos);
            if (trackIndex < 0 || trackIndex >= ClipLibrary.Count)
            {
                return;
            }
            BlockCSpeaker.ApplyTrack(GameManager.Instance.World, blockPos, trackIndex);
        }

        private void TogglePower()
        {
            BlockCSpeaker.ApplyMuteToggle(GameManager.Instance.World, blockPos);
        }

        private void ChangeRadius(int direction)
        {
            BlockCSpeaker.ApplyRadiusStep(GameManager.Instance.World, blockPos, direction);
        }

        private void ChangePage(int delta)
        {
            int pages = PageCount;
            page = ((page + delta) % pages + pages) % pages;
        }

        private void Close()
        {
            xui.playerUI.windowManager.Close(ID);
        }

        private int PageCount => Utils.FastMax(1, (ClipLibrary.Count + Rows - 1) / Rows);

        // --- отрисовка ---

        public override void OnOpen()
        {
            base.OnOpen();
            IsDirty = true;
        }

        public override void Update(float _dt)
        {
            base.Update(_dt);
            if (IsDirty)
            {
                RefreshBindings();
                IsDirty = false;
            }
        }

        public override bool GetBindingValueInternal(ref string _value, string _bindingName)
        {
            // Биндинги считаются в том числе при разборе XML, когда мира ещё нет.
            World world = GameManager.Instance != null ? GameManager.Instance.World : null;
            BlockValue bv = world != null ? world.GetBlock(blockPos) : BlockValue.Air;

            switch (_bindingName)
            {
                case "powerText":
                    _value = BlockCSpeaker.IsMuted(bv)
                        ? Localization.Get("cspeakerWindowTurnOn")
                        : (BlockCSpeaker.IsPowered(world, blockPos)
                            ? Localization.Get("cspeakerWindowTurnOffPlaying")
                            : Localization.Get("cspeakerWindowTurnOffNoPower"));
                    return true;

                case "radiusText":
                    _value = BlockCSpeaker.IsMuted(bv)
                        ? Localization.Get("cspeakerWindowRadiusMuted")
                        : string.Format(Localization.Get("cspeakerWindowRadius"),
                            BlockCSpeaker.RadiusFromMeta(bv).ToString("0"));
                    return true;

                case "pageText":
                    _value = string.Format(Localization.Get("cspeakerWindowPage"), page + 1, PageCount);
                    return true;

                case "pagingVisible":
                    _value = (ClipLibrary.Count > Rows).ToString();
                    return true;
            }

            for (int i = 0; i < Rows; i++)
            {
                int trackIndex = page * Rows + i;
                bool exists = trackIndex < ClipLibrary.Count;

                if (_bindingName == "track" + i + "visible")
                {
                    _value = exists.ToString();
                    return true;
                }
                if (_bindingName == "track" + i + "name")
                {
                    _value = exists ? ClipLibrary.NameOf(trackIndex) : "";
                    return true;
                }
                if (_bindingName == "track" + i + "selected")
                {
                    _value = (exists && BlockCSpeaker.TrackFromMeta(bv) == trackIndex).ToString();
                    return true;
                }
            }

            return base.GetBindingValueInternal(ref _value, _bindingName);
        }
    }
}
