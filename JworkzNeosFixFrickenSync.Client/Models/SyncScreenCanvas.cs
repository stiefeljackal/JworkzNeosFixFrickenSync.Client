using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using System;
using System.Collections.Concurrent;
using System.Linq;
using JworkzNeosMod.Models;
using JworkzNeosMod.Client.Services;
using CodeX;
using System.Reflection;
using HarmonyLib;
using JworkzNeosMod.Client.Utilities;
using RecordUtil = CloudX.Shared.RecordUtil;
using System.Threading.Tasks;
using NeosModLoader;

namespace JworkzNeosMod.Client.Models
{
    public class SyncScreenCanvas : IDisposable
    {
        public static readonly color BTN_COLOR = new color(0.314f, 0.784f, 0.471f);

        private const byte LIST_PADDING_SIZE = 25;

        private const short LIST_ORDER_OFFSET = 1000;

        private const byte LIST_ITEM_HEIGHT = 150;

        private const byte LIST_ITEM_IMAGE_WIDTH = 175;

        private const short LIST_ITEM_MIN_WIDTH = 1670;
       
        private static readonly float2 LIST_ANCHOR_MAX = new float2(1, 0.95f);

        private const byte LIST_ITEM_THREE_INFO_LINE_HEIGHT = 34;

        private const byte LIST_ITEM_BODY_SPACING_PADDING = 10;

        private const float LIST_ITEM_BODY_TEXT_CONTENT_FLEXIBLE_WIDTH = 0.65f;

        private const byte LIST_ITEM_BODY_ACTION_BTN_FONT_SIZE = 30;

        private const byte LIST_ITEM_BODY_ACTION_BTN_PREFERRED_WIDTH = 185;

        private const string LIST_ITEM_BODY_ACTION_BTN_CONFIM_TEXT = "Really?";

        private static readonly float2 LIST_HEADER_ANCHOR_MIN = new float2(0, 0.95f);

        private const byte LIST_HEADER_SPACING = 25;

        private const short LIST_HEADER_OFFSET = 500;

        private const byte LIST_HEADER_TOPBOTTOM_PADDING = 5;

        private static readonly color LIST_HEADER_COLOR = new color(0, 0, 0, 0.92f);

        private const byte LIST_HEADER_FONT_SIZE = 32;

        private const ushort LIST_HEADER_BTN_PREFERREDWIDTH = 260;

        private const byte LIST_HEADER_BTN_FONT_SIZE = 24;

        private const byte PROGRESS_BAR_MIN_HEIGHT = 30;

        private const byte TITLE_FONT_SIZE = 64;

        private const byte SUBTITLE_FONT_SIZE = 24;

        private const byte PROGRESS_STAGE_FONT_SIZE = 30;

        private static readonly color LIST_ITEM_SECTION_FILL_COLOR = new color(color.White.rgb, 0.25f);

        private static readonly color LIST_ITEM_TRANSPARENT_COLOR = new color(color.White.rgb, 0.75f);
        
        private static readonly color STATE_COLOR_NORMAL = color.White;

        private static readonly color STATE_COLOR_SUCCESS = new color(0.4f, 1f, 0.4f, 1f);

        private static readonly color STATE_COLOR_WARNING = new color(1f, 0.6f, 0, 1f);

        private static readonly color STATE_COLOR_FAILURE = color.Red;

        private static readonly color FOLDER_COLOR = new color(1f, 0.75f, 0.5f, 0.75f);

        private Canvas ScreenCanvas { get; }

        public Slot HeaderSlot { get; private set; }

        public Slot BodySlot { get; private set; }

        public Slot EmptyListScreenSlot { get; private set; }

        public Slot SyncListSlot { get; private set; }

        private Sync<int> TotalSyncsCompleted = new Sync<int>();

        private Sync<int> FailedSyncsCount = new Sync<int>();

        public UIBuilder UIBuilder { get; }

        private ConcurrentDictionary<string, SyncTaskViewModel> _syncTaskViewModels = new ConcurrentDictionary<string, SyncTaskViewModel>();

        private ConcurrentDictionary<string, Slot> _syncTaskSlots = new ConcurrentDictionary<string, Slot>();

        private static readonly MethodInfo RecordManagerLastFailReasonSetter = AccessTools.PropertySetter(typeof(RecordManager), nameof(RecordManager.LastFailReason));

        public SyncScreenCanvas(Canvas screenCanvas)
        {
            ScreenCanvas = screenCanvas;
            UIBuilder = new UIBuilder(screenCanvas);
            RadiantUI_Constants.SetupDefaultStyle(UIBuilder);

            TotalSyncsCompleted.Initialize(screenCanvas.World, screenCanvas);
            FailedSyncsCount.Initialize(screenCanvas.World, screenCanvas);

            RecordKeeper.Instance.EntryMarkedCompleted += OnSyncTaskMarkedCompleted;
            RecordKeeper.Instance.EntryRestarted += OnSyncTaskRestarted;
            RecordKeeper.Instance.EntryRemoved += OnSyncTaskRemoved;

            Userspace.UserspaceWorld.RunSynchronously(RefreshStatistics);
        }

        public bool HasSyncTaskViewModel(Record record)
        {
            return _syncTaskViewModels.ContainsKey(record.RecordId);
        }

        public void CreateSyncTaskViewModel(Record record, UploadProgressState state)
        {
            if (HasSyncTaskViewModel(record)) { return; }

            var model = new SyncTaskViewModel(SyncListSlot);
            _syncTaskViewModels.TryAdd(record.RecordId, model);

            UpdateSyncTaskViewModel(model, record, state);
            var syncTaskSlot = AddSyncTaskLineItem(record.RecordId, model, state);
            _syncTaskSlots.TryAdd(record.RecordId, syncTaskSlot);
        }

        public void UpdateSyncTaskViewModel(Record record, UploadProgressState state)
        {
            var hasModel = _syncTaskViewModels.TryGetValue(record.RecordId, out var syncTaskViewModel);
        
            if (!hasModel) { return; }

            UpdateSyncTaskViewModel(syncTaskViewModel, record, state);
        }

        private void UpdateSyncTaskViewModel(SyncTaskViewModel model, Record record, UploadProgressState state)
        {
            color colorStatus;
            switch (state.Indicator)
            {
                case UploadProgressIndicator.Success:
                    colorStatus = STATE_COLOR_SUCCESS; break;
                case UploadProgressIndicator.Failure:
                    colorStatus = STATE_COLOR_FAILURE; break;
                case UploadProgressIndicator.Canceled:
                    colorStatus = STATE_COLOR_WARNING; break;
                default:
                    colorStatus = STATE_COLOR_NORMAL; break;
            }
            model.UpdateInfo(record, colorStatus, state);
        }

        private bool RemoveSyncTaskViewModel(string recordId, Predicate<SyncTaskViewModel> predicate = null)
        {
            var hasModel = _syncTaskViewModels.TryGetValue(recordId, out SyncTaskViewModel model);
            
            if (!hasModel) { return false; }

            var hasSlot = _syncTaskSlots.TryGetValue(recordId, out var slot);
            var isPredicateTrue = predicate == null ? true : predicate(model);

            var isRemovable = hasSlot && isPredicateTrue;

            if (isRemovable)
            {
                slot.Destroy();
                model.Dispose();
                _syncTaskViewModels.TryRemove(recordId, out var _);
            }

            return isRemovable;
        }

        public void InitUI()
        {
            AddBackgroundScreen();
            AddListHeader();
            AddListScreen();
            AddNoItemsMessageScreen();
        }

        private Slot AddSyncTaskLineItem(string recordId, SyncTaskViewModel model, UploadProgressState state)
        {

            EmptyListScreenSlot.ActiveSelf = false;

            UIBuilder.NestInto(SyncListSlot);

            var firstChild = SyncListSlot.Children.FirstOrDefault();

            var listItemSlot = UIBuilder.HorizontalLayout(childAlignment: Alignment.MiddleLeft).Slot;
            listItemSlot.Name = recordId;
            listItemSlot.OrderOffset = firstChild != null ? firstChild.OrderOffset - 1 : 0;
            var listItemLayoutElementComponent = listItemSlot.AttachComponent<LayoutElement>();
            listItemSlot.AttachComponent<ObjectRoot>();
            listItemLayoutElementComponent.MinHeight.Value = listItemLayoutElementComponent.PreferredHeight.Value = LIST_ITEM_HEIGHT;
            UIBuilder.NestInto(listItemSlot);

            AddSyncTaskLineItemImage(model);

            UIBuilder.NestInto(listItemSlot);
            var listItemInfoContentSlot = UIBuilder.VerticalLayout(childAlignment: Alignment.TopLeft).Slot;
            listItemInfoContentSlot.Name = "Sync Record Info Content";
            listItemInfoContentSlot.AttachComponent<LayoutElement>().MinWidth.Value = LIST_ITEM_MIN_WIDTH;
            UIBuilder.NestInto(listItemInfoContentSlot);

            AddSyncTaskLineItemBody(model);

            UIBuilder.NestInto(listItemInfoContentSlot);

            AddTextUISlot("Sync Record Stage", PROGRESS_STAGE_FONT_SIZE, model.StatusColor, LIST_ITEM_THREE_INFO_LINE_HEIGHT, model.TaskStage);
            AddProgressBar(model.StatusColor, model.Progress);

            return listItemSlot;
        }

        private void AddSyncTaskLineItemBody(SyncTaskViewModel model)
        {
            var bodyUi = UIBuilder.HorizontalLayout(LIST_ITEM_BODY_SPACING_PADDING, childAlignment: Alignment.MiddleCenter);
            bodyUi.PaddingTop.Value = LIST_ITEM_BODY_SPACING_PADDING;
            bodyUi.PaddingBottom.Value = LIST_ITEM_BODY_SPACING_PADDING;

            var bodySlot = bodyUi.Slot;
            bodySlot.Name = "Sync Record Content Body";
            UIBuilder.NestInto(bodySlot);

            var bodyTextUi = UIBuilder.VerticalLayout(childAlignment: Alignment.MiddleCenter);
            var bodyTextSlot = bodyTextUi.Slot;
            bodyTextSlot.Name = "Sync Record Content Body - Text";
            bodyTextSlot.GetComponent<LayoutElement>().FlexibleWidth.Value = LIST_ITEM_BODY_TEXT_CONTENT_FLEXIBLE_WIDTH;
            UIBuilder.NestInto(bodyTextSlot);

            AddTextUISlot("Sync Record Name", TITLE_FONT_SIZE, color.White, LIST_ITEM_THREE_INFO_LINE_HEIGHT, model.TaskTitle, vertAlignnment: TextVerticalAlignment.Top);
            AddTextUISlot("Sync Record Path", SUBTITLE_FONT_SIZE, LIST_ITEM_TRANSPARENT_COLOR, LIST_ITEM_THREE_INFO_LINE_HEIGHT, model.TaskInventoryPath);

            UIBuilder.NestInto(bodySlot);

            AddButton("Delete Unsynced", LIST_ITEM_BODY_ACTION_BTN_FONT_SIZE, OnDeleteUnsyncedPressed, model.IsTaskConflicting, preferredWidth: LIST_ITEM_BODY_ACTION_BTN_PREFERRED_WIDTH);
            AddButton("Force Sync", LIST_ITEM_BODY_ACTION_BTN_FONT_SIZE, OnForceSyncPressed, model.IsTaskConflicting, preferredWidth: LIST_ITEM_BODY_ACTION_BTN_PREFERRED_WIDTH);
            AddButton("Keep Both", LIST_ITEM_BODY_ACTION_BTN_FONT_SIZE, OnKeepBothSyncPressed, model.IsTaskConflicting, preferredWidth: LIST_ITEM_BODY_ACTION_BTN_PREFERRED_WIDTH);
            AddButton("Retry Sync", LIST_ITEM_BODY_ACTION_BTN_FONT_SIZE, OnSyncRetryPressed, model.CanTaskRetry, preferredWidth: LIST_ITEM_BODY_ACTION_BTN_PREFERRED_WIDTH);
        }

        private void AddButton(string btnText, byte fontSize, ButtonEventHandler onBtnPress, Sync<bool> enabledBoolSync = null, color? btnBgColor = null, color? btnFontColor = null, float preferredWidth = -1f)
        {
            var btnUi = UIBuilder.Button(btnText, btnBgColor ?? BTN_COLOR);
            btnUi.LocalPressed += onBtnPress;

            var btnUiText = btnUi.Slot.GetComponentInChildren<Text>();

            btnUiText.Size.Value = fontSize;
            btnUiText.Color.Value = btnFontColor ?? color.Black;

            var btnSlot = btnUi.Slot;
            btnSlot.Name = $"{btnText} Btn";
            btnSlot.GetComponent<LayoutElement>().PreferredWidth.Value = preferredWidth;

            if (enabledBoolSync != null)
            {
                btnUi.Slot.ActiveSelf_Field.DriveFrom(enabledBoolSync);
            }
        }

        private void AddSyncTaskLineItemImage(SyncTaskViewModel model)
        {
            var imageLayout = UIBuilder.VerticalLayout();
            imageLayout.PaddingRight.Value = LIST_PADDING_SIZE;
            var imageLayoutSlot = imageLayout.Slot;
            imageLayoutSlot.Name = "Sync Record Image";
            imageLayoutSlot.GetComponent<LayoutElement>().MinWidth.Value = LIST_ITEM_IMAGE_WIDTH;
            UIBuilder.NestInto(imageLayoutSlot);

            var bgImageUi = UIBuilder.Image();
            bgImageUi.Tint.Value = LIST_ITEM_SECTION_FILL_COLOR;
            var bgImageSlot = bgImageUi.Slot;
            bgImageSlot.Name = "Image Background";
            UIBuilder.NestInto(bgImageSlot);

            if (model.TaskType == SyncTaskType.Folder)
            {
                UIBuilder.Image(FOLDER_COLOR);
                var folderTextUi = UIBuilder.Text("");
                folderTextUi.Content.DriveFrom(model.RecordName);
                folderTextUi.Color.Value = color.Black;
            }
            else
            {
                UIBuilder.Image(model.ThumbnailUri);
            }

            UIBuilder.NestOut();
            UIBuilder.NestOut();
        }

        private Text AddTextUISlot(string slotName, float fontSize, color fontColor, int preferredHeight, Sync<string> syncField = null, TextHorizontalAlignment horzAlignment = TextHorizontalAlignment.Left, TextVerticalAlignment vertAlignnment = TextVerticalAlignment.Middle)
        {
            var textUi = AddTextUISlot(slotName, fontSize, preferredHeight, syncField);
            textUi.Color.Value = fontColor;
            textUi.HorizontalAlign.Value = horzAlignment;
            textUi.VerticalAlign.Value = vertAlignnment;
            UIBuilder.NestOut();

            return textUi;
        }

        private Text AddTextUISlot(string slotName, float fontSize, Sync<color> colorSyncField, int preferredHeight, Sync<string> syncField = null)
        {
            var textUi = AddTextUISlot(slotName, fontSize, preferredHeight, syncField);
            textUi.Color.DriveFrom(colorSyncField);
            UIBuilder.NestOut();

            return textUi;
        }

        private Text AddTextUISlot(string slotName, float fontSize, int preferredHeight, Sync<string> syncField = null)
        {
            var textContainerSlot = UIBuilder.Empty(slotName);
            var textContainerLayoutComponent = textContainerSlot.GetComponent<LayoutElement>();
            textContainerLayoutComponent.PreferredHeight.Value = textContainerLayoutComponent.FlexibleHeight.Value = preferredHeight;
            UIBuilder.NestInto(textContainerSlot);

            var textUi = UIBuilder.Text("");

            if (syncField != null)
            {
                textUi.Content.DriveFrom(syncField);
            }
            textUi.Align = Alignment.MiddleLeft;
            textUi.Size.Value = fontSize;

            return textUi;
        }

        private void AddProgressBar(Sync<color> statusColorField, Sync<float> progressField)
        {
            var progressBarUi = UIBuilder.Image();
            progressBarUi.Tint.Value = LIST_ITEM_SECTION_FILL_COLOR;
            var progressBarSlot = progressBarUi.Slot;
            progressBarSlot.Name = "Sync Record Progress Bar";
            progressBarSlot.GetComponent<LayoutElement>().MinHeight.Value = PROGRESS_BAR_MIN_HEIGHT;

            UIBuilder.NestInto(progressBarSlot);

            var progressBarFillUi = UIBuilder.Image();
            var progressBarFillSlot = progressBarFillUi.Slot;
            var progressBarFillColorSmoothValueComponent = progressBarFillSlot.AttachComponent<SmoothValue<color>>();
            progressBarFillColorSmoothValueComponent.TargetValue.DriveFrom(statusColorField);
            progressBarFillColorSmoothValueComponent.Value.Target = progressBarFillUi.Tint;

            progressBarSlot.Name = "Fill Bar";
            var progressBarFillRectTransform = progressBarFillSlot.GetComponent<RectTransform>();
            var progressBarComponent = progressBarFillSlot.AttachComponent<ProgressBar>();
            progressBarComponent.AnchorMin.Target = progressBarFillRectTransform.AnchorMin;
            progressBarComponent.AnchorMax.Target = progressBarFillRectTransform.AnchorMax;
            var progressBarSmoothValueComponent = progressBarFillSlot.AttachComponent<SmoothValue<float>>();
            progressBarSmoothValueComponent.TargetValue.DriveFrom(progressField);
            progressBarSmoothValueComponent.Value.Target = progressBarComponent.Progress;

            UIBuilder.NestOut();
        }

        private void AddBackgroundScreen()
        {
            UIBuilder.Image(UserspaceRadiantDash.DEFAULT_BACKGROUND);
            BodySlot = UIBuilder.Empty("Body");
            BodySlot.AttachComponent<Mask>();
        }

        private void AddNoItemsMessageScreen()
        {
            UIBuilder.NestInto(BodySlot);
            var emptyListScreenSlot = UIBuilder.Empty("Empty List Screen");
            emptyListScreenSlot.OrderOffset = LIST_ORDER_OFFSET + 1;
            UIBuilder.NestInto(emptyListScreenSlot);
            UIBuilder.Text("All records in the process of syncing or waiting to be synced will be listed here.", alignment: Alignment.MiddleCenter);
            EmptyListScreenSlot = emptyListScreenSlot;
        }


        private void AddListHeader()
        {
            UIBuilder.NestInto(BodySlot);

            var headerUi = UIBuilder.HorizontalLayout(LIST_HEADER_SPACING, LIST_HEADER_TOPBOTTOM_PADDING, LIST_HEADER_SPACING, LIST_HEADER_TOPBOTTOM_PADDING, LIST_HEADER_SPACING, Alignment.MiddleCenter);
            headerUi.RectTransform.AnchorMin.Value = LIST_HEADER_ANCHOR_MIN;
            HeaderSlot = headerUi.Slot;
            HeaderSlot.Name = "Sync Status Header";
            HeaderSlot.OrderOffset = LIST_HEADER_OFFSET;

            var headerImageUi = HeaderSlot.AttachComponent<Image>();
            headerImageUi.Tint.Value = LIST_HEADER_COLOR;

            AddHeaderStatistic("Total Syncs", TotalSyncsCompleted, "Total Syncs: {0}");
            AddHeaderStatistic("Current Failed Syncs", FailedSyncsCount, "Current Failed Syncs: {0}");

            UIBuilder.NestInto(HeaderSlot);

            var headerBtnsUi = UIBuilder.HorizontalLayout(childAlignment: Alignment.MiddleRight);
            headerBtnsUi.Slot.Name = "Header Btns";
            headerBtnsUi.ForceExpandWidth.Value = false;

            UIBuilder.NestInto(headerBtnsUi.Slot);

            AddButton("Clear Synced Activity", LIST_HEADER_BTN_FONT_SIZE, OnClearHistoryPressed, preferredWidth: LIST_HEADER_BTN_PREFERREDWIDTH);
        }

        private void AddHeaderStatistic<T>(string headingText, Sync<T> syncField, string valueFormat) where T : struct
        {
            UIBuilder.NestInto(HeaderSlot);

            var totalSyncsTextUi = AddTextUISlot(headingText, LIST_HEADER_FONT_SIZE, -1);
            var valueTextFormatComponent = totalSyncsTextUi.Slot.AttachComponent<ValueTextFormatDriver<T>>();
            valueTextFormatComponent.Source.Target = syncField;
            valueTextFormatComponent.Format.Value = valueFormat;
            valueTextFormatComponent.Text.Target = totalSyncsTextUi.Content;
        }

        private void AddListScreen()
        {
            UIBuilder.NestInto(BodySlot);

            var syncStatusListUi = UIBuilder.Mask();
            syncStatusListUi.RectTransform.AnchorMax.Value = LIST_ANCHOR_MAX;
            var syncStatusListSlot = syncStatusListUi.Slot;
            syncStatusListSlot.Name = "Sync Status Listing";
            syncStatusListSlot.OrderOffset = LIST_ORDER_OFFSET;

            UIBuilder.NestInto(syncStatusListSlot);

            var syncListScrollAreaUi = UIBuilder.VerticalLayout(padding: LIST_PADDING_SIZE, childAlignment: Alignment.TopLeft);
            syncListScrollAreaUi.Spacing.Value = LIST_PADDING_SIZE;
            var syncListSlot = syncListScrollAreaUi.Slot;
            syncListSlot.Name = "Sync Status Scroll Area";
            syncListSlot.AttachComponent<ScrollRect>();
            var contentSizeFitterComponent = syncListSlot.AttachComponent<ContentSizeFitter>();
            contentSizeFitterComponent.VerticalFit.Value = SizeFit.PreferredSize;
            SyncListSlot = syncListSlot;
        }

        private void RefreshStatistics()
        {
            var recordKeeper = RecordKeeper.Instance;
            TotalSyncsCompleted.Value = recordKeeper.CompletedSyncs;
            FailedSyncsCount.Value = recordKeeper.CurrentFailedSyncs;

            var recordManager = Engine.Current.RecordManager;
            if (!string.IsNullOrEmpty(recordManager.LastFailReason) && recordKeeper.CurrentFailedSyncs <= 0) {
                RecordManagerLastFailReasonSetter.Invoke(recordManager, new[] { string.Empty });
            }
        }

        private void PrepareRecordAction(IButton button, Action<Record> OnRecordReceived)
        {
            var syncTaskListItemSlot = button.Slot.GetObjectRoot();
            var recordId = syncTaskListItemSlot.Name;
            var recordKeeper = RecordKeeper.Instance;
            var recordEntry = recordKeeper.GetRecordEntry(recordId);

            if (recordEntry == null) { return; }

            var isSuccessOrProgress = !recordEntry.IsSuccessfulSync.HasValue || recordEntry.IsSuccessfulSync.Value;

            if (isSuccessOrProgress) { return; }

            OnRecordReceived(recordEntry.Record);
        }

        private void OnDeleteUnsyncedPressed(IButton button, ButtonEventData _)
        {
            if (!button.Confirm(LIST_ITEM_BODY_ACTION_BTN_CONFIM_TEXT)) { return; }

            PrepareRecordAction(button, async (record) => await RemoveRecordFromRecordKeeper(record).ConfigureAwait(false));
        }

        private void OnForceSyncPressed(IButton button, ButtonEventData _)
        {
            if (!button.Confirm(LIST_ITEM_BODY_ACTION_BTN_CONFIM_TEXT)) { return; }

            PrepareRecordAction(button, (record) => {
                RecordKeeper.Instance.RestartRecord(record);
                Engine.Current.RecordManager.EnqueueRecordSyncTask(record, true);
            });
        }

        private void OnKeepBothSyncPressed(IButton button, ButtonEventData _)
        {
            if (!button.Confirm(LIST_ITEM_BODY_ACTION_BTN_CONFIM_TEXT)) { return; }

            PrepareRecordAction(button, async (record) => {
                var clonedRecord = record.Clone<Record>();
                clonedRecord.RecordId = RecordUtil.GenerateRecordID();
                clonedRecord.LocalVersion = 0;
                clonedRecord.GlobalVersion = 0;
                clonedRecord.Name = $"{record.Name} (Sync Copy)";
                NeosMod.Msg(clonedRecord.Name);
                NeosMod.Msg(clonedRecord.AssetURI);
                await Task.WhenAll(RemoveRecordFromRecordKeeper(record), Engine.Current.RecordManager.SaveRecord(clonedRecord)).ConfigureAwait(false);
            });
        }

        private void OnSyncRetryPressed(IButton button, ButtonEventData eventData)
        {
            if (!button.Confirm(LIST_ITEM_BODY_ACTION_BTN_CONFIM_TEXT)) { return; }

            PrepareRecordAction(button, (record) =>
            {
                RecordKeeper.Instance.RestartRecord(record);
                Engine.Current.RecordManager.EnqueueRecordSyncTask(record);
            });
        }

        private void OnClearHistoryPressed(IButton button, ButtonEventData eventData)
        {
            button.World.RunSynchronously(() =>
            {
                var syncTaskViewKeys = _syncTaskViewModels.Keys.ToArray();

                for (var i = 0; i < syncTaskViewKeys.Length; i++)
                {
                    var key = syncTaskViewKeys[i];
                    RemoveSyncTaskViewModel(key, (model) => model.IsTaskSuccessful || model.IsTaskCanceled);
                }

                EmptyListScreenSlot.ActiveSelf = !_syncTaskViewModels.Any();
            });
        }

        private void OnSyncTaskRestarted(object sender, RecordKeeperEntryEventArgs entry)
        {
            UpdateSyncTaskViewModel(entry.Record, new UploadProgressState("Restarting"));
        }

        private void OnSyncTaskRemoved(object sender, RecordKeeperEntryEventArgs entry)
        {
            UpdateSyncTaskViewModel(entry.Record, new UploadProgressState("Sync Task Removed", UploadProgressIndicator.Canceled));
            RefreshStatistics();
            RefreshInventoryScreen();
        }

        private void OnSyncTaskMarkedCompleted(object sender, RecordKeeperEntryEventArgs e)
        {
            Userspace.UserspaceWorld.RunSynchronously(RefreshStatistics);

            if (e.IsSuccessfulSync && e.PreviousFailedAttempts > 0)
            {
                RefreshInventoryScreen();
            }
        }

        private async Task RemoveRecordFromRecordKeeper(Record record)
        {
            var recordKeeper = RecordKeeper.Instance;
            await Engine.Current.LocalDB.DeleteRecordAsync(record).ConfigureAwait(false);
            recordKeeper.RemoveRecord(record);
        }

        private void RefreshInventoryScreen()
        {
            Userspace.UserspaceWorld.RunSynchronously(() => {
                var inventory = InventoryBrowser.CurrentUserspaceInventory;
                if (inventory == null) { return; }

                inventory.Open(null, SlideSwapRegion.Slide.None);
            });
        }

        public void Dispose()
        {
            RecordKeeper.Instance.EntryMarkedCompleted -= OnSyncTaskMarkedCompleted;
            RecordKeeper.Instance.EntryRestarted -= OnSyncTaskRestarted;
            RecordKeeper.Instance.EntryRemoved -= OnSyncTaskRemoved;

            ScreenCanvas?.Destroy();
            ScreenCanvas?.Dispose();
        }
    }
}
