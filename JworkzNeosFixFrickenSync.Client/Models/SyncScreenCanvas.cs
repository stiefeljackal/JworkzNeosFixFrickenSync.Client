using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using System;
using System.Collections.Concurrent;
using System.Linq;
using JworkzNeosMod.Models;
using FrooxEngine.LogiX.WorldModel;
using JworkzNeosMod.Client.Services;

namespace JworkzNeosMod.Client.Models
{
    public class SyncScreenCanvas : IDisposable
    {
        public static readonly color BTN_COLOR = new color(0.314f, 0.784f, 0.471f);

        private const int LIST_PADDING_SIZE = 25;

        private const short LIST_ORDER_OFFSET = 1000;

        private const int LIST_ITEM_HEIGHT = 150;

        private const int LIST_ITEM_MIN_WIDTH = 1670;
       
        private static readonly float2 LIST_ANCHOR_MAX = new float2(1, 0.95f);

        private const byte LIST_ITEM_THREE_INFO_LINE_HEIGHT = 34;

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

        public SyncScreenCanvas(Canvas screenCanvas)
        {
            ScreenCanvas = screenCanvas;
            UIBuilder = new UIBuilder(screenCanvas);
            RadiantUI_Constants.SetupDefaultStyle(UIBuilder);

            TotalSyncsCompleted.Initialize(screenCanvas.World, screenCanvas);
            TotalSyncsCompleted.Value = 0;

            FailedSyncsCount.Initialize(screenCanvas.World, screenCanvas);

            RecordKeeper.Instance.EntryMarkedCompleted += OnSyncTaskMarkedCompleted;
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
            var syncTaskSlot = AddSyncTaskLineItem(model, state);
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
            var colorStatus = state.IsSuccessful.HasValue ? (state.IsSuccessful.Value ? STATE_COLOR_SUCCESS : STATE_COLOR_FAILURE) : STATE_COLOR_NORMAL;
            model.UpdateInfo(record, colorStatus, state);
        }

        public void InitUI()
        {
            AddBackgroundScreen();
            AddListHeader();
            AddListScreen();
            AddNoItemsMessageScreen();
        }

        private Slot AddSyncTaskLineItem(SyncTaskViewModel model, UploadProgressState state)
        {

            EmptyListScreenSlot.ActiveSelf = false;

            UIBuilder.NestInto(SyncListSlot);

            var firstChild = SyncListSlot.Children.FirstOrDefault();

            var listItemSlot = UIBuilder.HorizontalLayout(childAlignment: Alignment.MiddleLeft).Slot;
            listItemSlot.Name = "Sync Row Item";
            listItemSlot.OrderOffset = firstChild != null ? firstChild.OrderOffset - 1 : 0;
            var listItemLayoutElementComponent = listItemSlot.AttachComponent<LayoutElement>();
            listItemLayoutElementComponent.MinHeight.Value = listItemLayoutElementComponent.PreferredHeight.Value = LIST_ITEM_HEIGHT;
            UIBuilder.NestInto(listItemSlot);

            AddSyncTaskLineItemImage(model);

            UIBuilder.NestInto(listItemSlot);
            var listItemInfoContentSlot = UIBuilder.VerticalLayout(childAlignment: Alignment.TopLeft).Slot;
            listItemInfoContentSlot.Name = "Sync Record Info Content";
            listItemInfoContentSlot.AttachComponent<LayoutElement>().MinWidth.Value = LIST_ITEM_MIN_WIDTH;
            UIBuilder.NestInto(listItemInfoContentSlot);

            AddTextUISlot("Sync Record Name", TITLE_FONT_SIZE, color.White, LIST_ITEM_THREE_INFO_LINE_HEIGHT, model.TaskTitle);
            AddTextUISlot("Sync Record Path", SUBTITLE_FONT_SIZE, LIST_ITEM_TRANSPARENT_COLOR, LIST_ITEM_THREE_INFO_LINE_HEIGHT, model.TaskInventoryPath);
            AddTextUISlot("Sync Record Stage", PROGRESS_STAGE_FONT_SIZE, model.StatusColor, LIST_ITEM_THREE_INFO_LINE_HEIGHT, model.TaskStage);

            AddProgressBar(model.StatusColor, model.Progress);

            return listItemSlot;
        }

        private void AddSyncTaskLineItemImage(SyncTaskViewModel model)
        {
            var imageLayout = UIBuilder.VerticalLayout();
            imageLayout.PaddingRight.Value = LIST_PADDING_SIZE;
            var imageLayoutSlot = imageLayout.Slot;
            imageLayoutSlot.Name = "Sync Record Image";
            imageLayoutSlot.GetComponent<LayoutElement>().MinWidth.Value = LIST_ITEM_HEIGHT;
            UIBuilder.NestInto(imageLayoutSlot);

            var bgImageUi = UIBuilder.Image();
            bgImageUi.Tint.Value = LIST_ITEM_SECTION_FILL_COLOR;
            var bgImageSlot = bgImageUi.Slot;
            bgImageSlot.Name = "Image Background";
            UIBuilder.NestInto(bgImageSlot);

            if (model.IsFolder)
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

        private Text AddTextUISlot(string slotName, float fontSize, color fontColor, int preferredHeight, Sync<string> syncField = null)
        {
            var textUi = AddTextUISlot(slotName, fontSize, preferredHeight, syncField);
            textUi.Color.Value = fontColor;
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
            progressBarFillUi.Tint.DriveFrom(statusColorField);
            var progressBarFillSlot = progressBarFillUi.Slot;
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

            var btnUi = UIBuilder.Button("Clear Synced Activity", BTN_COLOR);
            btnUi.LocalPressed += OnClearHistoryPressed;

            var btnUiText = btnUi.Slot.GetComponentInChildren<Text>();

            btnUiText.Size.Value = LIST_HEADER_BTN_FONT_SIZE;
            btnUiText.Color.Value = color.Black;
            btnUi.Slot.Name = "Clear Synced Activity Btn";
            btnUi.Slot.GetComponent<LayoutElement>().PreferredWidth.Value = LIST_HEADER_BTN_PREFERREDWIDTH;
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

        private void OnClearHistoryPressed(IButton button, ButtonEventData eventData)
        {
            button.World.RunSynchronously(() =>
            {
                var syncTaskViewModels = _syncTaskViewModels.Values.ToArray();

                for (var i = 0; i < syncTaskViewModels.Length; i++)
                {
                    var model = syncTaskViewModels[i];
                    var recordId = model.RecordId.Value;
                    var hasSlot = _syncTaskSlots.TryGetValue(recordId, out var slot);
                    var isSuccessful = model.IsTaskSuccessful.Value.HasValue ? model.IsTaskSuccessful.Value.Value : false;

                    if (hasSlot && isSuccessful)
                    {
                        slot.Destroy();
                        model.Dispose();
                    }
                }
            });
        }

        private void OnSyncTaskMarkedCompleted(object sender, RecordKeeperEntry e)
        {
            Userspace.UserspaceWorld.RunSynchronously(() =>
            {
                var recordKeeper = RecordKeeper.Instance;
                TotalSyncsCompleted.Value = recordKeeper.CompletedSyncs;
                FailedSyncsCount.Value = recordKeeper.CurrentFailedSyncs;
            });
        }

        public void Dispose()
        {
            ScreenCanvas?.Destroy();
            ScreenCanvas?.Dispose();
        }
    }
}
