using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using System;
using System.Collections.Concurrent;
using System.Linq;
using JworkzNeosMod.Models;

namespace JworkzNeosMod.Client.Models
{
    public class SyncScreenCanvas : IDisposable
    {
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

        private static readonly color LIST_HEADER_BTN_COLOR = new color(0.25f, 0.43f, 0.85f, 0.4f);

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

        public Slot BodySlot { get; set; }

        public Slot EmptyListScreenSlot { get; set; }

        public Slot SyncListSlot { get; set; }

        private Sync<uint> TotalSyncsCompleted = new Sync<uint>();

        public UIBuilder UIBuilder { get; }

        private ConcurrentDictionary<string, SyncTaskViewModel> _syncTaskViewModels = new ConcurrentDictionary<string, SyncTaskViewModel>();

        public SyncScreenCanvas(Canvas screenCanvas)
        {
            ScreenCanvas = screenCanvas;
            UIBuilder = new UIBuilder(screenCanvas);
            RadiantUI_Constants.SetupDefaultStyle(UIBuilder);

            TotalSyncsCompleted.Initialize(screenCanvas.World, screenCanvas);
            TotalSyncsCompleted.Value = 0;
        }

        public bool HasSyncTaskViewModel(Record record)
        {
            return _syncTaskViewModels.ContainsKey(record.RecordId);
        }

        public void CreateSyncTaskViewModel(Record record, UploadProgressState state)
        {
            if (HasSyncTaskViewModel(record)) { return; }

            var model = new SyncTaskViewModel(SyncListSlot);
            _ = _syncTaskViewModels.TryAdd(record.RecordId, model);

            UpdateSyncTaskViewModel(model, record, state);
            AddSyncTaskLineItem(model, state);
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

            if (state.IsSuccessful.HasValue)
            {
                TotalSyncsCompleted.Value++;
            }
        }

        public void InitUI()
        {
            AddBackgroundScreen();
            AddListHeader();
            AddListScreen();
            AddNoItemsMessageScreen();
        }

        private void AddSyncTaskLineItem(SyncTaskViewModel model, UploadProgressState state)
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
            var headerSlot = headerUi.Slot;
            headerSlot.Name = "Sync Status Header";
            headerSlot.OrderOffset = LIST_HEADER_OFFSET;

            var headerImageUi = headerSlot.AttachComponent<Image>();
            headerImageUi.Tint.Value = LIST_HEADER_COLOR;

            UIBuilder.NestInto(headerSlot);

            var totalSyncsTextUi = AddTextUISlot("Total Syncs", LIST_HEADER_FONT_SIZE, -1);
            var valueTextFormatComponent = totalSyncsTextUi.Slot.AttachComponent<ValueTextFormatDriver<uint>>();
            valueTextFormatComponent.Source.Target = TotalSyncsCompleted;
            valueTextFormatComponent.Format.Value = "Total Syncs Completed: {0}";
            valueTextFormatComponent.Text.Target = totalSyncsTextUi.Content;

            UIBuilder.NestInto(headerSlot);

            var headerBtnsUi = UIBuilder.HorizontalLayout(childAlignment: Alignment.MiddleRight);
            headerBtnsUi.Slot.Name = "Header Btns";
            headerBtnsUi.ForceExpandWidth.Value = false;

            UIBuilder.NestInto(headerBtnsUi.Slot);

            var btnUi = UIBuilder.Button("Clear Synced Activity", LIST_HEADER_BTN_COLOR);
            btnUi.LocalPressed += OnClearHistoryPressed;
            btnUi.Slot.GetComponentInChildren<Text>().Size.Value = LIST_HEADER_BTN_FONT_SIZE;
            btnUi.Slot.Name = "Clear Synced Activity Btn";
            btnUi.Slot.GetComponent<LayoutElement>().PreferredWidth.Value = LIST_HEADER_BTN_PREFERREDWIDTH;
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
            button.World.RunSynchronously(() => SyncListSlot.DestroyChildren());
        }

        public void Dispose()
        {
            ScreenCanvas?.Destroy();
            ScreenCanvas?.Dispose();
        }
    }
}
