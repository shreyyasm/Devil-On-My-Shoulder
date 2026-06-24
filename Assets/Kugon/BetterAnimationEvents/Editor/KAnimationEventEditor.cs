using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kugon.BetterAnimationEvents.Editor
{
    public class KAnimationEventEditor : EditorWindow
    {
        [Serializable]
        public enum SelectionState
        {
            None,
            BoxSelection,
            EventDrag,
            EventHover,
            ChannelDrag,
            TimelineHeadDrag,
            ChannelEditing,
            Resizing,
        }

        private bool ExperimentMode
        {
            get => SaveHandler.SaveFile.EditorOptions.ExperimentMode;
            set => SaveHandler.SaveFile.EditorOptions.ExperimentMode = value;
        }

        // Helper Classes
        private KAnimationEventIcon Icon;
        private KAnimationEventInputs Input;
        private KAnimationEventColor KAnimationColor;
        private KUndoHelper KUndo;
        private KClipBoard ClipBoard;
        private KAnimationPreview AnimationPreview;
        private KMethodHandler Method;
        private KComponentHelper Component;
        public KAnimationEventSaveHandler SaveHandler;

        //
        private static bool isInitialized;

        // Animation
        private bool isPlaying;
        private int currentFrame;
        private int samples = 30;
        private GenericMenu animationListMenu;

        private float elapsedTime;
        private float animationSpeed = 1f;
        private int animationSpeedMax = 2;
        
        // Channel
        private string channelName;
        private KChannelLayout currentChannelLayout;
        private GenericMenu channelLayoutListMenu;
        
        // Cache
        // private HashSet<int> animatorClipsNew;
        private HashSet<AnimationClip> animatorClips;

        // Area Rect
        private float timelineBorderThickness = 40f;
        private Rect timelineRect;
        private Rect emptyAreaRect;
        private string originalChannelName;
        private int isEditingChannel = -1;
        int maxIteration = int.MaxValue;

        // Window Related
        private float currentZoom = 1f;
        private float minSplit = 300f;
        private float split = 300f;
        private float endSplit = 15f;
        private float previousRightWidth;
        private float minGapBetweenFrames = 40f;
        private bool isMiddleMouseDragging = false;
        private float timelineHeadPosition;
        private Rect scrollRect;
        private Rect leftRect;
        private Rect rightRect;
        private Vector2 notifyScroll;
        private Vector2 controlScroll;
        private bool hoverRepaint;
        private bool autoApplySelectedColor;
        private bool isWindowLocked;
        private bool isReadOnlyClip;

        private GenericMenu eventMenu;
        private GenericMenu channelMenu;
        private GenericMenu menu;

        // Selection Box
        private Rect boxRect;
        private Vector2 mouseStartPos;

        private GenericMenu functionMenu;
        private bool functionToggled;

        // Selection
        private Animator selectedAnimator;
        private RuntimeAnimatorController runtimeAnimationController;
        private KAnimationClipData selectedClip;
        private KAnimationClipData lastSavedClip;
        private SelectionHandler selectionHandler;
        private bool multipleSelectionActive;
        private SelectionState selectionState = SelectionState.None;
        private KColor selectedEventColor;
        private KColor pickedColor;
        private HashSet<Tuple<int, int>> duplicatedFrames = new HashSet<Tuple<int, int>>();
        private KAnimationEvent hoveredNameTag;

        private List<(int frame, int channel)> originalPositions = new List<(int frame, int channel)>();
        private int dragStartFrame;
        private int dragStartChannel;

        // Options
        bool showSampleRate = true;
        bool showHoverInfo = true;
        bool showAllClips = true; // Gives you acces to all saved clips
        bool showSeconds = false;
        private bool useCurveAnimationSpeed = false;
        private KSort animationSortOrder = KSort.NameAscending;
        bool applyRootMotion = true; // TODO Try to add this as a feature


        private AnimationEventTooltipState
            eventTooltip = AnimationEventTooltipState.ShowAlways; // Option To Show NameTags

        private Vector2 GlobalMousePosition;
        private Vector2 HoverMousePositin;
        private Vector2 CachedMousePosition;


        public bool HasValidClip => selectedClip != null && selectedClip.IsValid;
        public KAnimationClipData GetCurrentClipData => HasValidClip ? selectedClip : null;

        public bool CanRunEditor
        {
            get
            {
                if (selectedAnimator == null) return false;
                if (selectedAnimator.runtimeAnimatorController == null) return false;

                return true;
                // Editor is all set to run with no fault
            }
        }

        private KAnimationEditorOptions EditorOptions => SaveHandler.SaveFile.EditorOptions;

        [MenuItem("Window/Animation/Animation Event")]
        public static void ShowWindow()
        {
            GetWindow<KAnimationEventEditor>("Animation Event", true);

            var window = GetWindow<KAnimationEventEditor>("Animation Event", true);
            window.titleContent.image = EditorGUIUtility.IconContent("AnimationClip On Icon").image;
            window.minSize = new Vector2(600, 300);
        }

        // private void CreateGUI()
        // {
        //     Init();
        //     SelectAnimator();
        // }

        private void OnEnable()
        {
            if (!isInitialized)
            {
                isInitialized = true;
            }
            Init();

            EditorOptions.Load(ref showSeconds, ref showSampleRate, ref samples, ref showAllClips, ref eventTooltip,
                ref showHoverInfo, ref animationSpeedMax, ref KAnimationColor.UserGeneratedColor,
                ref useCurveAnimationSpeed, ref animationSortOrder, ref applyRootMotion);
            EditorOptions.LoadWindowOptions(ref autoApplySelectedColor);

            Selection.selectionChanged += SelectAnimator;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Undo.undoRedoPerformed += UndoPerformed;
            EditorApplication.update += CheckEditorUpdate;

            BuildFunctionMenuDropDown();
            AnimationMode.StopAnimationMode();
        }

        private void OnDisable()
        {
            EditorOptions.Save(showSeconds, showSampleRate, samples, showAllClips, eventTooltip, showHoverInfo,
                animationSpeedMax, KAnimationColor.UserGeneratedColor, useCurveAnimationSpeed, animationSortOrder, applyRootMotion);
            EditorOptions.SaveWindowOptions(autoApplySelectedColor);

            Selection.selectionChanged -= SelectAnimator;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Undo.undoRedoPerformed -= UndoPerformed;
            EditorApplication.update -= CheckEditorUpdate;

            SaveHandler.SaveFileDirty();
            AnimationPreview.IsPreview = false;

            isInitialized = false;
            // AnimationMode.StopAnimationMode();
        }

        private void UndoPerformed()
        {
            selectionState = SelectionState.None;

            if (SaveHandler.SaveFile.undoSelected != null)
                selectedClip = SaveHandler.Load(SaveHandler.SaveFile.undoSelected);

            selectionHandler.VerifySelected();

            if (selectionHandler.selectedChannel > selectedClip.Channels.Count - 1)
                selectionHandler.SelectChannel(selectedClip.Channels.Count - 1);

            Repaint();
        }

        private void Init()
        {
            Icon = new KAnimationEventIcon();
            Input = new KAnimationEventInputs();
            KAnimationColor = new KAnimationEventColor();
            SaveHandler = new KAnimationEventSaveHandler();
            SaveHandler.LoadSaveFile();
            KUndo = new KUndoHelper(SaveHandler.SaveFile);
            ClipBoard = new KClipBoard();
            AnimationPreview = new KAnimationPreview();
            Component = new KComponentHelper();
            
            KAnimationColor.OriginalColor = GUI.color;

            animationListMenu = new GenericMenu();
            selectionHandler = new SelectionHandler();
            Method = new KMethodHandler();

            selectedClip = null;
            selectedEventColor = KColor.Default();
            pickedColor = KColor.Default();
            duplicatedFrames = new HashSet<Tuple<int, int>>();
            
            SelectFirstOrDefaultChannelLayout();
            
            EditorGUIUtility.SetIconSize(Vector2.zero);

            BuildMenuMenuDropDown();
            SelectAnimator();
        }

        private void Update()
        {
            if (isPlaying && CanRunEditor && HasValidClip && AnimationPreview.IsPreview)
            {
                float speed = 1;
                if (useCurveAnimationSpeed)
                {
                    float time = Mathf.InverseLerp(0, selectedClip.Lenght, elapsedTime);
                    speed = EditorOptions.animationSpeedCurve.Evaluate(time);
                    speed = Mathf.Clamp(speed, 0.01f, 5f);
                }
                else
                {
                    speed = animationSpeed;
                }

                elapsedTime += Time.deltaTime * speed;

                AnimationPreview.SampleAnimationClip(selectedAnimator.gameObject, selectedClip, ref elapsedTime);
                currentFrame = Mathf.FloorToInt(elapsedTime * selectedClip.FrameRate);
                SceneView.RepaintAll();
                Repaint();
            }
        }

        private void CheckEditorUpdate()
        {
            if (CanRunEditor && selectedAnimator.runtimeAnimatorController != runtimeAnimationController)
            {
                OnRuntimeAnimationControllerChanged(selectedAnimator.runtimeAnimatorController);
                Repaint();
            }
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                AnimationPreview.IsPreview = false;
                Repaint();
            }
        }

        private Vector2 GetValidMousePosition()
        {
            Event evt = Event.current;
            if (evt != null &&
                (evt.type == EventType.MouseMove ||
                 evt.type == EventType.MouseDown ||
                 evt.type == EventType.MouseDrag ||
                 evt.type == EventType.Repaint))
            {
                CachedMousePosition = evt.mousePosition;
            }

            return CachedMousePosition;
        }

        private void OnGUI()
        {
            GlobalMousePosition = Event.current.mousePosition;
            Vector2 MousePosInGUI = GUIUtility.ScreenToGUIPoint(GlobalMousePosition);
            Vector2 MouseScreenPoint = GUIUtility.GUIToScreenPoint(GlobalMousePosition);

            // Handle Keyboard and Mouse
            bool guiIsFocused = EditorGUIUtility.editingTextField || GUI.GetNameOfFocusedControl() != "";

            if (!guiIsFocused && Input.SpaceDown(true) && selectionState == SelectionState.None)
            {
                isPlaying = !isPlaying;
                if (isPlaying) AnimationPreview.IsPreview = true;
                Repaint();
            }
            else if (HasValidClip && Input.DeleteDown(true))
            {
                DeleteSelectedEvents();
                Repaint();
            }
            else if (Input.EscapeUp())
            {
                selectionHandler.UnselectAll();
                GUI.FocusControl(null);
                Repaint();
            }
            else if (!guiIsFocused && Input.FDown(true))
            {
                FitToScreen();
                Repaint();
            }

            if (Event.current.control)
            {
                if (Input.Copy(true) && HasValidClip)
                {
                    TryCopySelectedEvents();
                }

                if (Input.Paste(true) && HasValidClip)
                {
                    TryPasteSelectedEvents();
                }
            }

            if (Input.MouseMiddle(EventType.MouseDown, true))
            {
                isMiddleMouseDragging = true;
                mouseStartPos = GlobalMousePosition;
            }

            // Handle mouse drag
            if (Input.MouseMiddle(EventType.MouseDrag, true) && isMiddleMouseDragging)
            {
                Vector2 delta = GlobalMousePosition - mouseStartPos;
                mouseStartPos = GlobalMousePosition;
                notifyScroll -= delta;
                notifyScroll.x = Math.Max(0, notifyScroll.x);
                notifyScroll.y = Math.Max(0, notifyScroll.y);
            }

            // Handle mouse button release
            if (Input.MouseMiddle(EventType.MouseUp, true))
                isMiddleMouseDragging = false;

            multipleSelectionActive = Event.current.shift;

            Rect windowRect = position;
            GUI.enabled = CanRunEditor && HasValidClip;

            #region  Top Area

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            Draw_TopControl();

            EditorGUILayout.EndHorizontal();
          
            #endregion

            EditorGUILayout.BeginHorizontal();
            
            #region Animation Control - Left Area

            GUILayout.BeginVertical(GUILayout.ExpandHeight(true), GUILayout.Width(split));

            Draw_ControlMenu();

            float textWidth = 110f;
            Draw_ExtraOptions(textWidth);

            GUILayout.Space(10f);
            GUILayout.FlexibleSpace();

            Draw_EventControl(textWidth);
            GUILayout.Space(1f);

            Draw_BottomOptions();
            
            GUILayout.EndVertical();
            TryUpdateLastRect(ref leftRect);

            #endregion

            #region TimelineDraw - Right Area

            float startPos = Mathf.Max(split, minSplit);
            GUILayout.BeginVertical(GUILayout.ExpandHeight(true), GUILayout.MaxWidth(position.width - startPos));
           
            // Timeline
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("", GUIStyle.none);

            EditorGUILayout.EndHorizontal();
            TryUpdateLastRect(ref timelineRect);
            
            EditorGUI.DrawRect(timelineRect, AnimationPreview.IsPreview ? KAnimationColor.DarkTimeline : KAnimationColor.BlackTimeline);
            
            // Change Timeline Color
            if (HasValidClip)
            {
                float startX = Mathf.Max(timelineRect.x, timelineRect.x + timelineBorderThickness - notifyScroll.x);
                float endX = timelineRect.x + timelineBorderThickness + GetPositonX(selectedClip.FrameCount) - notifyScroll.x;

                if (endX > timelineRect.x)
                {
                    Rect colorRect = timelineRect;
                    colorRect.x = startX;
                    colorRect.width = endX - startX;
                    EditorGUI.DrawRect(colorRect, AnimationPreview.IsPreview ? KAnimationColor.LightTimeline : KAnimationColor.Odd);
                }
            }

            #region Move Timeline Head

            EditorGUIUtility.AddCursorRect(timelineRect, MouseCursor.Pan);
            if (HasValidClip && selectionState == SelectionState.None && Event.current.type == EventType.MouseDown &&
                timelineRect.Contains(Event.current.mousePosition))
            {
                selectionState = SelectionState.TimelineHeadDrag;
                AnimationPreview.IsPreview = true;
                isPlaying = false;
                MoveTimelineHead();
            }

            if (selectionState == SelectionState.TimelineHeadDrag)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    MoveTimelineHead();
                }
                
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, position.height), MouseCursor.Pan);

                if (Event.current.type == EventType.MouseUp)
                {
                    selectionState = SelectionState.None;
                    Repaint();
                }
            }

            #endregion

            // Empty Area Section
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("", GUIStyle.none);
            EditorGUILayout.EndHorizontal();
            // Change Color
            emptyAreaRect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawRect(emptyAreaRect, new Color(0, 0, 0, 0.2f));

            if (Input.MouseLeft(EventType.MouseDown) && emptyAreaRect.Contains(Event.current.mousePosition))
            {
                selectionHandler.UnselectAll();
                GUI.FocusControl(null);
                Repaint();
            }
            else if (CanRunEditor && Input.MouseRight(EventType.MouseDown) &&
                     emptyAreaRect.Contains(Event.current.mousePosition))
            {
                Rect eventRect = emptyAreaRect;
                eventRect.x = Event.current.mousePosition.x;
                eventRect.y = EditorGUIUtility.singleLineHeight;
                BuildTimelineEventMenuDropDown();
                eventMenu.DropDown(eventRect);
            }

            float scrollDelta = 0;
            if (Event.current.type == EventType.ScrollWheel)
            {
                scrollDelta = Event.current.delta.y;
                Event.current.delta = Vector2.zero;
            }

            #region ScrollArea

            if (CanRunEditor)
            {
                // Width Adjustment
                if (Math.Abs(previousRightWidth - rightRect.width) > 0.01f)
                {
                    float oldSpace = GetSpace(previousRightWidth);
                    float newSpace = GetSpace(rightRect.width);

                    notifyScroll.x = (notifyScroll.x / oldSpace) * newSpace;

                    previousRightWidth = rightRect.width;
                }

                notifyScroll = EditorGUILayout.BeginScrollView(notifyScroll);

                // Draw Channels And Animation Keys
                if (HasValidClip && selectedClip.Channels.Count > 0)
                {
                    Draw_Channels(rightRect);

                    for (int frameIndex = 0; frameIndex < maxIteration; frameIndex++)
                    {
                        // float xPos = stampStartX + frameIndex * space;
                        float xPos = timelineBorderThickness + GetPositonX(frameIndex);

                        if (xPos < -5) continue;
                        if (xPos > rightRect.width + notifyScroll.x)
                        {
                            Draw_AnimationKeysAndTags();
                            Draw_BoxSelection();
                            break;
                        }

                        if (frameIndex % FrameStep() == 0) // frame skip
                            EditorGUI.DrawRect(new Rect(xPos, 0, 1f, position.height + notifyScroll.y), KAnimationColor.ClearGray);
                    }
                }

                EditorGUILayout.BeginHorizontal();

                GUILayout.Space((rightRect.width - 75f - timelineBorderThickness) / 2f + notifyScroll.x);
                if (GUILayout.Button("Add Channel", GUILayout.Width(150f)))
                    AddChannel();

                if (showHoverInfo && selectionState == SelectionState.None && hoveredNameTag != null)
                {
                    Draw_HoverInfo(hoveredNameTag);
                    hoveredNameTag = null;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndScrollView();
                TryUpdateLastRect(ref scrollRect);

                // Zooming
                Event e = Event.current;
                if (scrollDelta != 0 && scrollRect.Contains(e.mousePosition))
                {
                    int frameCount = Mathf.Max(1, HasValidClip ? selectedClip.FrameCount : 60);
                    float spaceBefore = GetSpace();

                    float mouseLocalX = e.mousePosition.x - timelineRect.x - timelineBorderThickness + notifyScroll.x;
                    float frameUnderMouse = mouseLocalX / spaceBefore;

                    float zoomFactor = 1f - scrollDelta * 0.01f;
                    currentZoom *= zoomFactor;

                    float minSpace = 0.01f;
                    float maxSpace = rightRect.width / 2;
                    float minZoom = minSpace / ((rightRect.width - timelineBorderThickness * 2) / frameCount);
                    float maxZoom = maxSpace / ((rightRect.width - timelineBorderThickness * 2) / frameCount);
                    currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

                    float spaceAfter = GetSpace();

                    notifyScroll.x = frameUnderMouse * spaceAfter - (e.mousePosition.x - timelineRect.x - timelineBorderThickness);
                    notifyScroll.x = Mathf.Max(0, notifyScroll.x);

                    Event.current.Use();
                    Repaint();
                }
            }

            #endregion

            #region Draw_TopEvents-Frames-Timestamps-TimelineHead

            float firstY = timelineRect.y;
            float secondY = emptyAreaRect.y;
            
            float space = GetSpace();
            float stampStartX = leftRect.width + timelineBorderThickness - notifyScroll.x;
            int maxFrames = HasValidClip ? selectedClip.FrameCount : 60;

            if (CanRunEditor && HasValidClip && selectedClip.Channels.Count > 0)
            {
                var eventMarker = Icon.EventMarker;

                HashSet<Tuple<int, int>> checkedFrames = new HashSet<Tuple<int, int>>();
                duplicatedFrames = new HashSet<Tuple<int, int>>();

                foreach (var e in selectedClip.Events)
                {
                    Tuple<int, int> tupleKey = new Tuple<int, int>(selectedClip.FrameByTime(e.time), e.channelIndex);

                    if (checkedFrames.Contains(tupleKey))
                    {
                        checkedFrames.Add(tupleKey);
                        duplicatedFrames.Add(tupleKey);
                    }
                    else
                        checkedFrames.Add(tupleKey);
                }

                // selectedClip.SortEvents();
                HashSet<Tuple<int, int>> hash = new HashSet<Tuple<int, int>>(); // To Avoid Redraw
                foreach (var e in selectedClip.Events)
                {
                    int eventFrame = selectedClip.FrameByTime(e.time);
                    Tuple<int, int> tupleKey = new Tuple<int, int>(selectedClip.FrameByTime(e.time), e.channelIndex);
                    float positionX = GetPositionXByFrame(eventFrame);
                    
                    if(positionX < leftRect.width) continue;

                    if (hash.Contains(tupleKey)) continue;
                    hash.Add(tupleKey);

                    Rect buttonRect = new Rect(positionX - eventMarker.width / 2f, secondY, eventMarker.width,
                        eventMarker.height);

                    if (Input.MouseLeft(EventType.MouseDown) && buttonRect.Contains(Event.current.mousePosition))
                        TopEventClicked(eventFrame);

                    // if (duplicatedFrames.Contains(tupleKey))
                    //     ChangeGuiColor(KAnimationColor.Red);
                    // else
                    //     ResetGuiColor();

                    GUI.DrawTexture(buttonRect, eventMarker, ScaleMode.ScaleToFit);
                }

                ResetGuiColor();
            }

            
            GUIStyle bigLabel = new GUIStyle(EditorStyles.label) { fontSize = 10 };
            
            int frameStep = FrameStep();

            int startIndex = Mathf.Max(0, Mathf.FloorToInt((-5 - stampStartX) / space));
            startIndex = startIndex - (startIndex % frameStep);
            int endIndex = Mathf.Min(maxIteration, Mathf.CeilToInt((rightRect.width + notifyScroll.x) / space));

            float lerp = Mathf.InverseLerp(5, minGapBetweenFrames * 0.8f, GetSpace());
            
            float MajorHeight = timelineRect.height * 0.6f;
            float MinorHeight = Mathf.Lerp(0, MajorHeight,lerp);
            float LabelHeight = timelineRect.height * 0.4f;
            
            for (int frameIndex = startIndex; frameIndex < endIndex; frameIndex++)
            {
                float xPos = stampStartX + frameIndex * space;

                if (xPos < leftRect.width) continue;

                if (frameIndex % frameStep == 0) // Major tick
                {
                    EditorGUI.DrawRect(new Rect(xPos, firstY + (timelineRect.height - MajorHeight), 1f, MajorHeight), KAnimationColor.Whitish);

                    string label;
                    if (showSeconds)
                    {
                        label = FrameToSeconds(frameIndex, "F2");
                    }
                    else
                        label = frameIndex.ToString();

                    EditorGUI.LabelField(new Rect(xPos + 2, firstY + LabelHeight / 2, 50, LabelHeight), label, bigLabel);
                }
                else if(MinorHeight > 1) // Minor tick
                {
                    EditorGUI.DrawRect(new Rect(xPos, firstY + (timelineRect.height - MinorHeight), 1f, MinorHeight), KAnimationColor.ClearGray);
                }

                if (CanRunEditor && HasValidClip && frameIndex == currentFrame) // Timeline head
                {
                    EditorGUI.DrawRect(new Rect(xPos, firstY, 2f, rightRect.height - firstY / 2 - 1), KAnimationColor.White);
                    Draw_TimelineHead(xPos + 1f, firstY, KAnimationColor.White);
                }
                else if (frameIndex == 0 || frameIndex == maxFrames) // First/last line
                {
                    EditorGUI.DrawRect(new Rect(xPos, firstY, 1f, timelineRect.height - 1), KAnimationColor.Whitish);
                }
            }
            
            if (selectionState == SelectionState.TimelineHeadDrag)
            {
                bigLabel.fontSize = 12;
                bigLabel.fontStyle = FontStyle.Bold;
                bigLabel.alignment = TextAnchor.MiddleCenter;
                
                float labelX = stampStartX + currentFrame * space;
                Rect hoverFrame = new Rect(labelX + 10f, firstY, firstY, firstY);

                string label = showSeconds ? FrameToSeconds(currentFrame) : currentFrame.ToString();
                
                Vector2 size = bigLabel.CalcSize(new GUIContent(label));

                hoverFrame.width = size.x + firstY;
                
                DrawRoundedRect(hoverFrame, KAnimationColor.BlackTimeline);
                EditorGUI.LabelField(hoverFrame, label, bigLabel);
            }
            
            #endregion
            
            #endregion

            
            GUILayout.EndVertical();

            TryUpdateLastRect(ref rightRect);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = true;
            Rect splitterRect = new Rect(leftRect.width - 2f, leftRect.y, 5f, leftRect.height);
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            // Handle dragging
            if (Input.MouseLeft(EventType.MouseDown) && splitterRect.Contains(GlobalMousePosition))
                selectionState = SelectionState.Resizing;

            split = Mathf.Clamp(split, minSplit, windowRect.width - minSplit);

            if (selectionState == SelectionState.Resizing)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    split = Mathf.Clamp(GlobalMousePosition.x, minSplit, windowRect.width - minSplit);
                    Repaint();
                }

                if (Event.current.type == EventType.MouseUp)
                    selectionState = SelectionState.None;
            }

            EditorGUI.DrawRect(splitterRect, KAnimationColor.DarkGray);
            GUI.enabled = wasEnabled;
            
            EditorGUILayout.EndHorizontal();
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                GUI.FocusControl(null);
                GUIUtility.keyboardControl = 0;
                Repaint();
            }

            // Repaint(); // TODO Remove Before Build
        }

        private string FrameToSeconds(int frameIndex, string format = "0.0000")
        {
            int sec = frameIndex / samples;
            float d = (float)(frameIndex % samples) / samples;
            float total = sec + d;
            return total.ToString(format);
        }
        

        private void MoveTimelineHead()
        {
            float localX = Mathf.Max(Event.current.mousePosition.x + notifyScroll.x - timelineBorderThickness - leftRect.width, 0f);
            currentFrame = Mathf.RoundToInt(localX / GetSpace());

            // selectedClip.Sample(selectedAnimator.gameObject, currentFrame);
            AnimationPreview.SampleAnimationClip(selectedAnimator.gameObject, selectedClip, currentFrame);
            elapsedTime = currentFrame / selectedClip.FrameRate;

            Repaint();
        }


        private Texture2D roundedSquare;
        void DrawRoundedRect(Rect rect, Color color)
        {
            if (roundedSquare == null)
            {
                roundedSquare = new Texture2D(1, 1);
                roundedSquare.SetPixel(0, 0, color);
                roundedSquare.Apply();
            }

            GUI.DrawTexture(rect, roundedSquare, ScaleMode.StretchToFill, true, 0, color, 0, 5f);
        }
        

        private void TryUpdateLastRect(ref Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
                rect = GUILayoutUtility.GetLastRect();
        }

        private void DeleteSelectedEvents()
        {
            KUndo.RecordObject("Remove Events");
            foreach (var kevt in selectionHandler.SelectedEvents)
                selectedClip.RemoveEvent(kevt);
            
            selectedClip.UpdateTime();
            KUndo.SaveRecord();

            selectionHandler.DeleteEvents();
        }

        private void TryPasteSelectedEvents()
        {
            if (!ClipBoard.HasCopy()) return;
            selectionHandler.UnselectAll();
            KUndo.RecordObject("Paste Events");
            float deltaTime = selectedClip.TimeByFrame(currentFrame) - ClipBoard.time;
            int channelDelta = selectionHandler.selectedChannel - ClipBoard.channelIndex;
            foreach (var kevt in ClipBoard.clippedEvents)
            {
                var copy = kevt.Copy();
                copy.channelIndex += channelDelta;
                copy.time += deltaTime;
                var added = selectedClip.AddEvent(copy);

                selectionHandler.Select(added, true, true);
            }
            selectedClip.UpdateTime();
            KUndo.SaveRecord();
        }

        private void TryCopySelectedEvents()
        {
            if (selectionHandler.SelectedEvents.Count > 0)
            {
                ClipBoard.CopyEvents(selectionHandler.SelectedEvents);
            }
        }

        private int FrameStep()
        {
            int step = Mathf.Max(1, Mathf.CeilToInt(minGapBetweenFrames / GetSpace()));

            if (GetSpace() > minGapBetweenFrames)
                return 1;
            
            return Mathf.Max(5, Mathf.CeilToInt(step / 5f) * 5);
        }

        void DrawSoftShadow(Rect rect, Color shadowColor, int shadowSize = 4)
        {
            for (int i = 1; i <= shadowSize; i++)
            {
                Color color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, shadowColor.a / (i * 2));
                EditorGUI.DrawRect(new Rect(rect.x - i, rect.y - i, rect.width, rect.height), color);
            }
        }

        private void Draw_Channels(Rect rightRect)
        {
            float minSize = Mathf.Max(GetSpace() * selectedClip.FrameCount, (rightRect.width + 1) + notifyScroll.x);
            for (int i = 0; i < selectedClip.Channels.Count; i++) // Channel Selection
            {
                KAnimationChannel channel = selectedClip.Channels[i];

                Rect rowRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(minSize));

                if (channel.index == selectionHandler.selectedChannel)
                    EditorGUI.DrawRect(rowRect, KAnimationColor.SelectionColor);
                else
                    EditorGUI.DrawRect(rowRect, i % 2 == 0 ? KAnimationColor.Odd : KAnimationColor.Even);

                EditorGUILayout.BeginHorizontal();

                GUI.Box(rowRect, "");

                // Select Channel
                if (selectionState == SelectionState.None && Input.MouseLeft(EventType.MouseDown) &&
                    rowRect.Contains(Event.current.mousePosition))
                {
                    selectionHandler.SelectChannel(selectedClip.Channels[i].index);
                    GUI.FocusControl(null);
                    isEditingChannel = -1;
                    Repaint();
                }
                else if (selectionState == SelectionState.None && Input.MouseRight(EventType.MouseDown) &&
                         rowRect.Contains(Event.current.mousePosition))
                {
                    Rect pos = new Rect();
                    pos.x = Event.current.mousePosition.x;
                    pos.y = EditorGUIUtility.singleLineHeight * (i + 1);
                    BuildChannelMenuDropDown(channel);
                    channelMenu.DropDown(pos);
                }

                // Editing Channel
                rowRect.x += notifyScroll.x + 5f; // Label Position

                Event e = Event.current;
                if (selectionState == SelectionState.ChannelEditing && isEditingChannel == channel.index)
                {
                    rowRect.width = GUI.skin.label.CalcSize(new GUIContent(channel.name)).x;
                    GUI.SetNextControlName("EditChannel");
                    string newName = GUI.TextField(rowRect, channel.name, EditorStyles.label);

                    if (newName != channel.name)
                    {
                        KUndo.RecordObject("Edit Channel Name");
                        channel.name = newName;
                        selectedClip.UpdateTime();
                        Repaint();
                    }

                    if (GUI.GetNameOfFocusedControl() != "EditChannel")
                        GUI.FocusControl("EditChannel");

                    if (e.isKey && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
                    {
                        isEditingChannel = -1;
                        selectionState = SelectionState.None;
                        e.Use();
                        GUI.SetNextControlName("");
                        GUI.FocusControl("");
                        KUndo.SaveRecord();
                    }
                    else if (Input.EscapeUp(true) || Input.MouseLeft(EventType.MouseDown, true))
                    {
                        channel.name = originalChannelName;
                        isEditingChannel = -1;
                        selectionState = SelectionState.None;
                        GUI.FocusControl("");
                        KUndo.SaveRecord();
                    }
                }
                else // Double Click to Edit
                {

                    GUI.Label(rowRect, channel.name, EditorStyles.label);
                    if (selectionState == SelectionState.None && e.type == EventType.MouseDown && e.clickCount == 2 &&
                        rowRect.Contains(e.mousePosition))
                    {
                        StartEditingChannelName(channel);
                        Repaint();
                        e.Use();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void StartEditingChannelName(KAnimationChannel channel)
        {
            isEditingChannel = channel.index;
            originalChannelName = channel.name;
            selectionState = SelectionState.ChannelEditing;
            notifyScroll.x = 0f;
        }

        private void MoveChannelUpAndDown(int channelIndex, int moveDir)
        {
            // moveDir: -1 = down, 1 = up

            if (selectedClip == null || selectedClip.Channels == null)
                return;

            int targetIndex = channelIndex + moveDir;

            if (channelIndex < 0 || channelIndex >= selectedClip.Channels.Count)
                return;
            if (targetIndex < 0 || targetIndex >= selectedClip.Channels.Count)
                return;

            KUndo.RecordObject("Move Channel");

            // Swap channels
            var temp = selectedClip.Channels[channelIndex];
            selectedClip.Channels[channelIndex] = selectedClip.Channels[targetIndex];
            selectedClip.Channels[targetIndex] = temp;

            // Swap events
            foreach (var kevt in selectedClip.Events)
            {
                if (kevt.channelIndex == channelIndex)
                    kevt.channelIndex = targetIndex;
                else if (kevt.channelIndex == targetIndex)
                    kevt.channelIndex = channelIndex;
            }

            for (int i = 0; i < selectedClip.Channels.Count; i++)
                selectedClip.Channels[i].index = i;

            KUndo.SaveRecord();
            Repaint();
        }

        private void RemoveEventsOnChannel(int channelIndex)
        {
            if (!HasValidClip) return;

            KUndo.RecordObject("Remove Events On Channel");
            for (int i = selectedClip.Events.Count - 1; i >= 0; i--)
            {
                if (selectedClip.Events[i].channelIndex == channelIndex)
                {
                    selectedClip.Events.RemoveAt(i);
                }
            }

            KUndo.SaveRecord();
        }

        private void RemoveChannel(int channelIndex, bool removeChannelsBelow = false)
        {
            KUndo.RecordObject("Remove Channels");

            if (removeChannelsBelow)
            {
                channelIndex++;
                while (channelIndex < selectedClip.Channels.Count)
                {
                    if (selectedClip.Channels.Count <= 1) return;
                    selectedClip.Channels.RemoveAt(channelIndex);

                    for (int i = 0; i < selectedClip.Channels.Count; i++)
                        selectedClip.Channels[i].index = i;

                    foreach (var kevt in selectedClip.Events)
                        if (kevt.channelIndex > channelIndex)
                            kevt.channelIndex--;

                    for (int i = selectedClip.Events.Count - 1; i >= 0; i--)
                        if (selectedClip.Events[i].channelIndex == channelIndex)
                            selectedClip.Events.RemoveAt(i);
                }
            }
            else
            {
                if (selectedClip.Channels.Count <= 1) return;
                selectedClip.Channels.RemoveAt(channelIndex);

                for (int i = 0; i < selectedClip.Channels.Count; i++)
                    selectedClip.Channels[i].index = i;

                for (int i = selectedClip.Events.Count - 1; i >= 0; i--)
                    if (selectedClip.Events[i].channelIndex == channelIndex)
                        selectedClip.Events.RemoveAt(i);
                    else if (selectedClip.Events[i].channelIndex > channelIndex)
                        selectedClip.Events[i].channelIndex -= 1;
            }

            KUndo.SaveRecord();
        }

        private void Draw_BoxSelection()
        {
            Vector2 mousePos = GlobalMousePosition - rightRect.position;
            mousePos.x -= timelineBorderThickness;

            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float xSpace = GetSpace();
            float timelineHeight = timelineRect.height * 2;

            if (selectionState == SelectionState.None &&
                Input.MouseLeft(EventType.MouseDown) &&
                rightRect.Contains(GlobalMousePosition))
            {
                selectionState = SelectionState.BoxSelection;
                mouseStartPos = mousePos + notifyScroll;
                boxRect = new Rect();
                Repaint();
            }

            if (selectionState == SelectionState.BoxSelection)
            {
                if (Input.MouseLeft(EventType.MouseDrag))
                {
                    Vector2 relativePos = mousePos + notifyScroll;

                    if (!multipleSelectionActive)
                        selectionHandler.UnselectAll();

                    float minX = Mathf.Min(mouseStartPos.x, relativePos.x);
                    float maxX = Mathf.Max(mouseStartPos.x, relativePos.x);
                    float minY = Mathf.Min(mouseStartPos.y, relativePos.y);
                    float maxY = Mathf.Max(mouseStartPos.y, relativePos.y);

                    minX = Mathf.Round(minX / xSpace) * xSpace + timelineBorderThickness - 2;
                    maxX = Mathf.Round(maxX / xSpace) * xSpace + timelineBorderThickness + 2;
                    minY = Mathf.Floor((minY - timelineHeight) / singleLineHeight) * singleLineHeight;
                    maxY = Mathf.Ceil((maxY - timelineHeight) / singleLineHeight) * singleLineHeight;

                    boxRect.x = minX;
                    boxRect.y = minY;
                    boxRect.width = maxX - minX;
                    boxRect.height = maxY - minY;
                    Repaint();
                }

                if (selectionState == SelectionState.BoxSelection)
                {
                    EditorGUI.DrawRect(boxRect, new Color(0.3f, 0.5f, 1f, 0.25f));
                    // Handles.DrawSolidRectangleWithOutline(boxRect, Color.Clear, Color.SelectionColor);
                }

                if (Input.MouseLeft(EventType.MouseUp))
                {
                    selectionState = SelectionState.None;
                    boxRect = new Rect();
                    Repaint();
                }
            }
        }

        private void Draw_TimelineHead(float x, float y, Color color)
        {
            float w = EditorGUIUtility.singleLineHeight * 0.33f;
            float rectH = EditorGUIUtility.singleLineHeight * 0.8f;
            float tipH = EditorGUIUtility.singleLineHeight - rectH;

            Vector3[] verts = new Vector3[]
            {
                new Vector3(x - w, y),
                new Vector3(x + w, y),
                new Vector3(x + w, y + rectH),
                new Vector3(x,     y + rectH + tipH),
                new Vector3(x - w, y + rectH),
            };

            Handles.color = color;
            Handles.DrawAAConvexPolygon(verts);
        }
        
        private void Draw_AnimationKeysAndTags()
        {
            float startX = timelineBorderThickness;
            var eventIcon = Icon.EventKey;
            Vector2 scale = new Vector2(15, 15);
            Tuple<int, int> tupleKey = new Tuple<int, int>(-1, -1);

            // Draw NameTags 
            bool hoverOverNameTag = false;
            if (eventTooltip != AnimationEventTooltipState.Hide)
            {
                foreach (var e in selectedClip.Events)
                {
                    int eventFrame = selectedClip.FrameByTime(e.time);
                    float height = EditorGUIUtility.singleLineHeight * (e.channelIndex);
                    string label = "   " + e.eventFunctionName;
                    GUIStyle style = GUI.skin.label;
                    Vector2 size = style.CalcSize(new GUIContent(label));

                    Rect nameTageRect = new Rect(startX + GetPositonX(eventFrame), height + 2, size.x,
                        EditorGUIUtility.singleLineHeight - 4);

                    bool isEmpty = e.eventFunctionName == "";
                    switch (eventTooltip)
                    {
                        case AnimationEventTooltipState.Hover:

                            if (!isEmpty && selectionHandler.HasSelected(e))
                                Draw_NameTag(nameTageRect, label, Method.HasFunctionID(e.eventFunctionName),
                                    selectionHandler.HasSelected(e), e.assignedColor);
                            else
                            {
                                // Adjust to detect Event Hover
                                nameTageRect.x -= (scale.x / 2) + 1;
                                nameTageRect.width = eventIcon.width;

                                if (nameTageRect.Contains(GetValidMousePosition()))
                                {
                                    nameTageRect.x = startX + GetPositonX(eventFrame);
                                    nameTageRect.width = size.x;

                                    if (isEmpty)
                                    {
                                        label = "   ?";
                                        nameTageRect.width = style.CalcSize(new GUIContent(label)).x;
                                    }

                                    Draw_NameTag(nameTageRect, label, Method.HasFunctionID(e.eventFunctionName),
                                        selectionHandler.HasSelected(e), e.assignedColor);

                                    hoverOverNameTag = true;
                                }
                            }

                            break;
                        case AnimationEventTooltipState.ShowAlways:
                            if (!isEmpty)
                            {
                                Draw_NameTag(nameTageRect, label, Method.HasFunctionID(e.eventFunctionName),
                                    selectionHandler.HasSelected(e), e.assignedColor);
                                if (showHoverInfo && selectionState == SelectionState.None &&
                                    nameTageRect.Contains(GetValidMousePosition()))
                                    hoveredNameTag = e;
                            }

                            break;
                    }
                }

                if (hoverOverNameTag != hoverRepaint)
                    Repaint();
            }

            // Draw animationKeys 
            ResetGuiColor();

            bool isHovering = false;
            KAnimationEvent clickedEvent = null;
            foreach (var e in selectedClip.Events)
            {
                int eventFrame = selectedClip.FrameByTime(e.time);

                float height = EditorGUIUtility.singleLineHeight * (e.channelIndex);
                Rect buttonRect = new Rect(
                    startX + GetPositonX(eventFrame) - (scale.x / 2) + 1,
                    height + (scale.y / 5),
                    eventIcon.width,
                    eventIcon.height
                );

                tupleKey = new Tuple<int, int>(eventFrame, e.channelIndex);
                if (duplicatedFrames.Contains(tupleKey))
                {
                    Rect errorRect = buttonRect;
                    errorRect.x -= 15f;
                    errorRect.size = Vector2.one * (EditorGUIUtility.singleLineHeight - 2);

                    GUI.DrawTexture(errorRect, Icon.Warn, ScaleMode.ScaleToFit);
                }

                // Box selection check
                if (Event.current.type == EventType.Repaint && selectionState == SelectionState.BoxSelection &&
                    (boxRect.Contains(buttonRect.center) ||
                     boxRect.Contains(buttonRect.position)))
                {
                    selectionHandler.Select(e, true, true);
                }

                if (!e.assignedColor.IsEqual(KColor.Default()))
                    ChangeGuiColor(e.assignedColor.Color);

                if (selectionHandler.HasSelected(e))
                    ChangeGuiColor(KAnimationColor.SelectionColor);

                // Mouse down click

                if (!multipleSelectionActive && buttonRect.Contains(GetValidMousePosition())) // Hover control
                    isHovering = true;

                if (Input.MouseLeft(EventType.MouseDown) && buttonRect.Contains(GetValidMousePosition()))
                    clickedEvent = e;

                GUI.DrawTexture(buttonRect, eventIcon, ScaleMode.ScaleToFit);
                ResetGuiColor();
            }

            if (clickedEvent != null)
                EventClicked(clickedEvent, isHovering);

            if (selectionState == SelectionState.None || selectionState == SelectionState.EventHover)
                selectionState = isHovering ? SelectionState.EventHover : SelectionState.None;

            if (isHovering != hoverRepaint)
                Repaint();

            hoverRepaint = isHovering;

            // TODO : Make it function
            var mousePos = Event.current.mousePosition;
            mousePos.x -= timelineBorderThickness;
            mousePos.y -= EditorGUIUtility.singleLineHeight / 2;

            var frameIndex = Mathf.RoundToInt(mousePos.x / GetSpace());
            frameIndex = Mathf.Max(0, frameIndex);
            var channelIndex = Mathf.RoundToInt(mousePos.y / EditorGUIUtility.singleLineHeight);
            channelIndex = Mathf.Clamp(channelIndex, 0, selectedClip.Channels.Count - 1);

            if (selectionState == SelectionState.EventHover)
            {
                if (Input.MouseLeft(EventType.MouseDown))
                {
                    selectionState = SelectionState.EventDrag;

                    dragStartFrame = frameIndex;
                    dragStartChannel = channelIndex;

                    originalPositions.Clear();
                    foreach (var selectedEvt in selectionHandler.SelectedEvents)
                        originalPositions.Add((selectedClip.FrameByTime(selectedEvt.time), selectedEvt.channelIndex));
                }
            }

            if (selectionState == SelectionState.EventDrag)
            {
                if (Input.MouseLeft(EventType.MouseDrag))
                {
                    KUndo.RecordObject("Event Drop", true);

                    int deltaFrame = frameIndex - dragStartFrame;
                    int deltaChannel = channelIndex - dragStartChannel;

                    for (int i = 0; i < selectionHandler.SelectedEvents.Count; i++)
                    {
                        var selectedEvt = selectionHandler.SelectedEvents[i];
                        var (origFrame, origChannel) = originalPositions[i];

                        selectedEvt.time = selectedClip.TimeByFrame(Mathf.Max(0, origFrame + deltaFrame));
                        selectedEvt.channelIndex =
                            Mathf.Clamp(origChannel + deltaChannel, 0, selectedClip.Channels.Count - 1);
                    }

                    Repaint();
                }

                if (Input.MouseLeft(EventType.MouseUp))
                {
                    selectionState = SelectionState.None;
                    KUndo.SaveRecord();
                    Repaint();
                }
            }

            ResetGuiColor();
        }

        private void Draw_NameTag(Rect nameTageRect, string label, bool hasFunctinName, bool isSelected, KColor color)
        {
            Vector3[] shape =
            {
                new Vector3(nameTageRect.x, nameTageRect.y + nameTageRect.height / 2), // Tip of triangle (middle left)
                new Vector3(nameTageRect.x + 10, nameTageRect.y), // Top-left corner
                new Vector3(nameTageRect.x + nameTageRect.width, nameTageRect.y), // Top-right corner
                new Vector3(nameTageRect.x + nameTageRect.width,
                    nameTageRect.y + nameTageRect.height), // Bottom-right corner
                new Vector3(nameTageRect.x + 10, nameTageRect.y + nameTageRect.height) // Bottom-left corner
            };

            Handles.color = isSelected ? KAnimationColor.SelectionColor : color.Color;
            Handles.DrawAAConvexPolygon(shape);
            Handles.color = KAnimationColor.Even;

            Vector3[] closedShape = new Vector3[shape.Length + 1];
            shape.CopyTo(closedShape, 0);
            closedShape[closedShape.Length - 1] = shape[0]; // Close the loop

            Handles.DrawAAPolyLine(closedShape);
            // EditorGUI.DrawRect(nameTageRect, Color.ClearGray);

            if (isSelected)
                ChangeGuiColor(KAnimationColor.OriginalColor);
            else
                ChangeGuiColor(KColor.GetReadableTextColor(color.Darker()));

            if (!hasFunctinName)
            {
                ChangeGuiColor(KAnimationColor.Red);
            }

            EditorGUI.LabelField(nameTageRect, label, EditorStyles.boldLabel);
            ResetGuiColor();
        }

        private void Draw_HoverInfo(KAnimationEvent kAnimationEvent)
        {
            string label = GenerateLabel(kAnimationEvent);
            
            GUIStyle style = GUI.skin.label;
            Vector2 hoverPosition = GetValidMousePosition() + Vector2.one * 15f;
            Vector2 hoverSize = style.CalcSize(new GUIContent(label));

            Rect hoverRect = new Rect(hoverPosition, hoverSize);

            float padding = 5f;

            Vector3[] shape =
            {
                new Vector3(hoverRect.x - padding, hoverRect.y),                                                // Top-left corner
                new Vector3(hoverRect.x + padding + hoverRect.width, hoverRect.y),                              // Top-right corner
                new Vector3(hoverRect.x + padding + hoverRect.width, hoverRect.y + hoverRect.height),           // Bottom-right corner
                new Vector3(hoverRect.x - padding, hoverRect.y + hoverRect.height)                              // Bottom-left corner
            };

            Handles.color = KAnimationColor.BlackTimeline;
            Handles.DrawAAConvexPolygon(shape);

            Vector3[] closedShape = new Vector3[shape.Length + 1];
            shape.CopyTo(closedShape, 0);
            closedShape[closedShape.Length - 1] = shape[0]; // Close the loop

            Handles.color = KAnimationColor.Even;
            Handles.DrawAAPolyLine(closedShape);

            EditorGUI.LabelField(hoverRect, label);
            Repaint();
        }

        private void Draw_BottomOptions()
        {
            string saveIcon = "-";
            if (HasValidClip)
                saveIcon = selectedClip.HasSaved() ? "" : "*";

            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Save current{saveIcon}", EditorStyles.toolbarButton, GUILayout.MinWidth(85f)))
                SaveCurrent();

            if (GUILayout.Button($"Save all ", EditorStyles.toolbarButton, GUILayout.MinWidth(50f)))
                SaveAll();

            if (GUILayout.Button($"Reset", EditorStyles.toolbarButton, GUILayout.MinWidth(50f)))
                Reset();

            GUILayout.FlexibleSpace();
            bool wasToggled = functionToggled;
            functionToggled = GUILayout.Toggle(functionToggled, functionToggled ? "Function" : "Default",
                EditorStyles.toolbarButton, GUILayout.MinWidth(100f));

            if (!wasToggled && functionToggled)
                ToggleFunctionView(true);
            else if (wasToggled && !functionToggled)
                ToggleFunctionView(false);

            // GUILayout.Space();
            GUILayout.EndHorizontal();
        }

        private void Draw_EventControl(float textWidth)
        {
            if (selectionHandler.HasSelectedEvents)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Event Setup ({selectionHandler.SelectedEvents.Count})", EditorStyles.boldLabel,
                    GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                Rect dropdownRect = GUILayoutUtility.GetLastRect();

                if (functionToggled)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUI.showMixedValue = selectionHandler.FunctionValue.Item2;

                    string rawName = selectionHandler.FunctionValue.Item1;
                    string label = Method.HasFunctionID(selectionHandler.FunctionValue.Item1)
                        ? rawName
                        : rawName == ""
                            ? $"{KConst.NoFunctionSelected}"
                            : rawName + $" {KConst.FunctionNotSupported}";

                    if (EditorGUILayout.DropdownButton(new GUIContent(label), FocusType.Passive,
                            EditorStyles.toolbarDropDown))
                    {
                        dropdownRect.x += 5;
                        BuildFunctionMenuDropDown();
                        functionMenu.DropDown(dropdownRect);
                    }

                    EditorGUI.showMixedValue = false;

                    GUILayout.Space(5f);
                    GUILayout.EndHorizontal();

                    if (Method.GetMethodInfo(selectionHandler.FunctionValue.Item1, out KMethodHandler.KMethodInfo info))
                    {
                        var param = info.GetParameters().FirstOrDefault();

                        if (param != null)
                        {
                            Type paramType = param.ParameterType;

                            if (paramType.IsEnum) // Enum Type
                            {
                                DrawEnumName(textWidth, paramType);
                            }
                            else if (paramType == typeof(AnimationEvent))
                            {
                                DrawFunctionName(textWidth);
                                DrawFloatName(textWidth);
                                DrawIntName(textWidth);
                                DrawStringName(textWidth);
                                DrawObjectName(textWidth, typeof(Object));
                            }
                            else
                            {
                                switch (Type.GetTypeCode(paramType))
                                {
                                    case TypeCode.String:
                                        DrawStringName(textWidth);
                                        break;
                                    case TypeCode.Int32:
                                        DrawIntName(textWidth);
                                        break;
                                    case TypeCode.Single:
                                        DrawFloatName(textWidth);
                                        break;
                                    case TypeCode.Object:
                                        DrawObjectName(textWidth,
                                            info.GetParameters()[0].ParameterType);
                                        break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    DrawFunctionName(textWidth);
                    DrawFloatName(textWidth);
                    DrawIntName(textWidth);
                    DrawStringName(textWidth);
                    DrawObjectName(textWidth, typeof(Object));
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                string eventCount = HasValidClip ? selectedClip.Events.Count.ToString() : "";
                GUILayout.Label("Select Event", GUILayout.ExpandWidth(true));
                GUILayout.Label($"{eventCount}", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private void Draw_ExtraOptions(float textWidth)
        {
            // GUILayout.BeginHorizontal();
            //
            // GUILayout.Button("Test", EditorStyles.toolbarButton);
            // GUILayout.Button("Test", EditorStyles.toolbarButton);
            // GUILayout.Button("Test", EditorStyles.toolbarButton);
            //
            //
            // GUILayout.EndHorizontal();
            
            bool wasEnabled = GUI.enabled;
            GUI.enabled = true;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Animation Speed", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(15f);
            if (useCurveAnimationSpeed)
                EditorOptions.animationSpeedCurve = EditorGUILayout.CurveField(EditorOptions.animationSpeedCurve);
            else
            {
                animationSpeed = EditorGUILayout.Slider(animationSpeed, 0.1f, animationSpeedMax);
                animationSpeed = Mathf.Clamp(animationSpeed, 0.1f, animationSpeedMax);
                animationSpeed = Mathf.Round(animationSpeed * 10f) / 10f;
            }

            GUILayout.Space(15f);
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Color Setup", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(15f);

            // GUILayout.Label("Pick Color");
            pickedColor.Color = EditorGUILayout.ColorField(pickedColor.Color);

            GUILayout.Space(5f);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Save Color", GUILayout.Width(100f)))
            {
                bool isExist = false;
                foreach (var uColor in KAnimationColor.UserGeneratedColor)
                {
                    if (KColor.AreColorsEqual(uColor, pickedColor))
                    {
                        Debug.LogWarning("Color Already Exists.");
                        isExist = true;
                        break;
                    }
                }

                if (!isExist)
                {
                    KAnimationColor.UserGeneratedColor.Add(new KColor(pickedColor.Color));
                    Repaint();
                }
            }

            GUILayout.Space(15f);
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);

            int extraOptionCount = 2;
            int columnCount = Mathf.FloorToInt((Mathf.Max(split, minSplit) - 30) / 45);
            int total = KAnimationColor.UserGeneratedColor.Count + extraOptionCount;
            int rowCount = Mathf.CeilToInt(total / columnCount) + 1;

            KColor removeAfterLoop = null;

            int index = 0;
            bool exit = false;
            for (int i = 0; i < rowCount; i++)
            {
                if (exit) continue;
                GUILayout.BeginHorizontal();
                GUILayout.Space(15f);
                for (int j = 0; j < columnCount; j++)
                {
                    if (index >= KAnimationColor.UserGeneratedColor.Count)
                    {
                        int innerIndex = index - KAnimationColor.UserGeneratedColor.Count;
                        if (innerIndex == 0)
                        {
                            if (GUILayout.Button(Icon.RemoveColor, EditorStyles.toolbarButton, GUILayout.Width(40f)))
                                removeAfterLoop = selectedEventColor;
                        }
                        else if (innerIndex == 1)
                        {
                            if (GUILayout.Button(Icon.NewColor, EditorStyles.toolbarButton, GUILayout.Width(40f)))
                            {
                                selectedEventColor = KColor.Default();
                                SetAnimationEventColor(selectedEventColor);
                            }
                        }
                        else
                        {
                            exit = true;
                            break;
                        }

                        // GUILayout.Space(5f);
                        //
                        // if (GUILayout.Button(Icon.NewColor, EditorStyles.toolbarButton, GUILayout.Width(40f)))
                        //     Debug.Log("");
                        GUILayout.Space(5f);
                        index++;
                    }
                    else
                    {
                        if (GUILayout.Button("", EditorStyles.toolbarButton, GUILayout.Width(40f)))
                        {
                            selectedEventColor = KAnimationColor.UserGeneratedColor[index];
                            SetAnimationEventColor(selectedEventColor);
                        }

                        Rect lastRect = GUILayoutUtility.GetLastRect();
                        EditorGUI.DrawRect(lastRect, KAnimationColor.UserGeneratedColor[index].Color);
                        if (selectedEventColor == KAnimationColor.UserGeneratedColor[index])
                            Handles.DrawSolidRectangleWithOutline(lastRect, Color.clear, KAnimationColor.White);

                        GUILayout.Space(5f);
                        index++;
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(5f);
            }

            if (removeAfterLoop != null)
            {
                KAnimationColor.UserGeneratedColor.Remove(removeAfterLoop);
                if (KAnimationColor.UserGeneratedColor.Count == 0)
                    selectedEventColor = KColor.Default();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(15f);
            GUILayout.Label("Auto Apply", GUILayout.Width(textWidth - 15));
            autoApplySelectedColor = GUILayout.Toggle(autoApplySelectedColor, "");
            GUILayout.EndHorizontal();
            
            // Draw Channel Editor
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Channel Setup", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(15f);

            GUILayout.Label("Name", GUILayout.Width(textWidth / 2));
            channelName = EditorGUILayout.TextField(channelName);
            
            if (GUILayout.Button(Icon.SaveChannelTemplate, GUILayout.Width(50f)))
            {
                AddChannelTemplate();
            }
            GUILayout.Space(15f);
            GUILayout.EndHorizontal();
            
            Rect dropdownRect = GUILayoutUtility.GetLastRect();
            GUILayout.BeginHorizontal();
            GUILayout.Space(15f);

            if (EditorGUILayout.DropdownButton(new GUIContent(currentChannelLayout.layoutName), FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                dropdownRect.x += 5;

                BuildChannelTemplateDropDown();
                channelLayoutListMenu.DropDown(dropdownRect);
                Repaint();
            }
            
            if (GUILayout.Button(Icon.ApplyChannelTemplate, GUILayout.Width(50f)))
            {
                ApplyChannelTemplate(currentChannelLayout);
            }
            if (GUILayout.Button(Icon.RemoveChannelTemplate, GUILayout.Width(25f)))
            {
                RemoveChannelTemplate();
            }
          
            GUILayout.Space(15f);
            GUILayout.EndHorizontal();


            GUI.enabled = wasEnabled;
        }

        private void SetAnimationEventColor(KColor color)
        {
            foreach (var e in selectionHandler.SelectedEvents)
            {
                e.assignedColor = color;
            }
        }
        
         private void Draw_TopControl()
        {
            bool wasEnabled = GUI.enabled;
            
            // Animation Selection
             if (!CanRunEditor)
            {
                ChangeGuiColor(KAnimationColor.Red);
                if (selectedAnimator == null)
                    GUILayout.Label("Missing Animator", GUILayout.MaxWidth(175));
                else if (selectedAnimator.runtimeAnimatorController == null)
                    GUILayout.Label("Missing Animation Controller", GUILayout.MaxWidth(175));
                ResetGuiColor();
            }
            else if (!HasValidClip)
            {
                wasEnabled = GUI.enabled;
                GUI.enabled = true;

                bool hasClip = animatorClips != null && animatorClips.Count != 0;
                ChangeGuiColor(KAnimationColor.Red);

                string label = "(Clip Destroyed)";
                if (!hasClip)
                {
                    ResetGuiColor();
                    label = "[No Clip]";
                }
                
                GUIContent selectedClipContent = new GUIContent(label);
                if (EditorGUILayout.DropdownButton(selectedClipContent, FocusType.Passive, EditorStyles.layerMaskField,
                        GUILayout.MaxWidth(175)) && hasClip)
                {
                    Rect dropdownRect = GUILayoutUtility.GetLastRect();
                    dropdownRect.x += 5;

                    BuildAnimationListMenuDropDown();
                    animationListMenu.DropDown(dropdownRect);
                    Repaint();
                }

                ResetGuiColor();

                GUI.enabled = wasEnabled;
            }
            else
            {
                // Rect dropdownRect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(175));
                GUIContent selectedClipContent = new GUIContent(selectedClip.IsValid
                    ? (IsClipBelongsToThisAnimationController(selectedClip)
                        ? selectedClip.Name
                        : "- " + selectedClip.Name)
                    : "[No Clip Selected]");
                if (EditorGUILayout.DropdownButton(selectedClipContent, FocusType.Passive, EditorStyles.layerMaskField,
                        GUILayout.MinWidth(150), GUILayout.ExpandWidth(true)))
                {
                    Rect dropdownRect = GUILayoutUtility.GetLastRect();
                    dropdownRect.x += 5;

                    BuildAnimationListMenuDropDown();
                    animationListMenu.DropDown(dropdownRect);
                    Repaint();
                }

                if (GUILayout.Button(Icon.FindAnimation, EditorStyles.toolbarButton, GUILayout.Width(20f)))
                {
                    string path = AssetDatabase.GetAssetPath(selectedClip.Clip);
                    Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                    Object clipAsset = allAssets.FirstOrDefault(a => a is AnimationClip && a.name == selectedClip.Clip.name);

                    if (clipAsset != null)
                    {
                        Selection.activeObject = clipAsset;
                        EditorGUIUtility.PingObject(clipAsset);
                    }
                }
            }
             
            if (isReadOnlyClip && GUILayout.Button(Icon.ReadOnlyWarning, EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                if (selectedClip != null && selectedClip.IsValid)
                {
                    if (EditorUtility.DisplayDialog(
                            "Duplicate Animation Clip",
                            "This clip is Read-Only. Do you want to create an editable duplicate?\nThis will also auto-replace in Animator Controllers.",
                            "Duplicate", "Cancel"))
                    {
                        selectedClip.DuplicateAnimationClip();
                        OnRuntimeAnimationControllerChanged(runtimeAnimationController);
                        SaveHandler.RebuildDictionary();
                        BuildAnimationListMenuDropDown();
                        OnAnimationSelected(selectedClip.Clip);
                        Repaint();
                    }
                }
            }
            
            
            GUILayout.FlexibleSpace();
            // GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));
            bool wasPreview = AnimationPreview.IsPreview;
            AnimationPreview.IsPreview = GUILayout.Toggle(AnimationPreview.IsPreview, Icon.Preview,
                EditorStyles.toolbarButton, GUILayout.Width(60));
            if (!CanRunEditor) AnimationPreview.IsPreview = false;
            else if (!wasPreview && AnimationPreview.IsPreview)
                AnimationPreview.SampleAnimationClip(selectedAnimator.gameObject, selectedClip, currentFrame);

            if (GUILayout.Button(Icon.GoToFirst, EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                currentFrame = 0;
                elapsedTime = 0;
                if (AnimationPreview.IsPreview)
                    AnimationPreview.SampleAnimationClip(selectedAnimator.gameObject, selectedClip, currentFrame);
            }

            if (GUILayout.Button(Icon.Prev, EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                currentFrame = Mathf.Max(--currentFrame, 0);
                if (AnimationPreview.IsPreview)
                    AnimationPreview.SampleAnimationClip(selectedAnimator.gameObject, selectedClip, currentFrame);
            }

            isPlaying = GUILayout.Toggle(isPlaying, Icon.Play, EditorStyles.toolbarButton, GUILayout.Width(25));

            if (GUILayout.Button(Icon.Next, EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                currentFrame = Mathf.Min(++currentFrame, selectedClip?.FrameCount ?? 60);
                if (AnimationPreview.IsPreview)
                    AnimationPreview.SampleAnimationClip(selectedAnimator.gameObject, selectedClip, currentFrame);
            }

            if (GUILayout.Button(Icon.GoToLast, EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                currentFrame = selectedClip.FrameCount;
                elapsedTime = selectedClip.Lenght;
                if (AnimationPreview.IsPreview)
                    AnimationPreview.SampleAnimationClip(selectedAnimator.gameObject, selectedClip, currentFrame);
            }

            EditorGUI.BeginChangeCheck();
            string input = EditorGUILayout.TextField(showSeconds ? FrameToSeconds(currentFrame) : currentFrame.ToString(), GUILayout.Width(100), GUILayout.Height(16));

            if (EditorGUI.EndChangeCheck())
            {
                if (showSeconds)
                {
                    string[] parts = input.Split('.');
                    if (parts.Length >= 1 && int.TryParse(parts[0], out int sec))
                    {
                        int frames = sec * samples;
            
                        if (parts.Length == 2)
                        {
                            string decPart = parts[1];
                            float divisor = Mathf.Pow(10, decPart.Length);
                            if (float.TryParse(decPart, out float decVal))
                            {
                                frames += Mathf.RoundToInt((decVal / divisor) * samples);
                            }
                        }
                        currentFrame = frames;
                    }
                }
                else
                {
                    if (int.TryParse(input, out int frame))
                        currentFrame = frame;
                }
            }
            
            currentFrame = Mathf.Max(0, currentFrame);
            
            
            GUILayout.FlexibleSpace();
            // isWindowLocked = EditorGUILayout.Toggle(isWindowLocked);
            isWindowLocked = GUILayout.Toggle(isWindowLocked, isWindowLocked ? Icon.LockOnIcon : Icon.LockIcon, EditorStyles.toolbarButton, GUILayout.MinWidth(25f));

            wasEnabled = GUI.enabled;
            GUI.enabled = true;
            if (GUILayout.Button(Icon.Settings, EditorStyles.toolbar, GUILayout.Width(25f)))
            {
                Rect dropRect = new Rect(0, 0, 0, 0);
                dropRect.x += position.width;
                BuildMenuMenuDropDown();
                menu.DropDown(dropRect);
            }
            
            GUI.enabled = wasEnabled;
        }
         
         // private void ShowButton(Rect position)
         // {
         //     if (GUI.Button(position, isWindowLocked ? Icon.LockOnIcon : Icon.LockIcon, EditorStyles.iconButton))
         //     {
         //         isWindowLocked = !isWindowLocked;
         //         Repaint();
         //     }
         //     
         //     // isWindowLocked = GUI.Toggle(position, isWindowLocked, isWindowLocked ? Icon.LockOnIcon : Icon.LockIcon);
         // }

        private void Draw_ControlMenu()
        {
            // Draw_Control();
            GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true), GUILayout.MinWidth(split), GUILayout.Height(25));
            // GUILayout.Height(25);


            if (showSampleRate)
            {
                GUILayout.Label("Samples");
                samples = EditorGUILayout.IntField(samples, GUILayout.Width(35), GUILayout.Height(16));
                samples = Mathf.Clamp(samples, 1, 999);
            }
            
            
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent(Icon.AddChannel, "Add channel."), EditorStyles.toolbarButton,
                    GUILayout.Width(25)))
                AddChannel();

            if (GUILayout.Button(new GUIContent(Icon.AddEvent, "Add event."), EditorStyles.toolbarButton,
                    GUILayout.Width(25)))
                AddEvent();

            GUILayout.Space(1f);
            GUILayout.EndHorizontal();
        }

        private void FitToScreen()
        {
            currentZoom = 1f;
            notifyScroll = Vector2.zero;

            if (!HasValidClip) return;

            if (selectionHandler.SelectedEvents.Count == 0)
            {
                currentZoom = 1f;
                notifyScroll = Vector2.zero;
                return;
            }

            int minFrame = int.MaxValue;
            int maxFrame = int.MinValue;
            foreach (var ev in selectionHandler.SelectedEvents)
            {
                int frame = selectedClip.FrameByTime(ev.time);
                if (frame < minFrame) minFrame = frame;
                if (frame > maxFrame) maxFrame = frame;
            }

            int frameRange = Mathf.Max(1, maxFrame - minFrame);
            float targetWidth = rightRect.width - timelineBorderThickness * 2;
            float targetSpace = targetWidth / (frameRange + 2);

            int frameCount = Mathf.Max(1, selectedClip.FrameCount);
            float minSpace = 0.01f;
            float maxSpace = rightRect.width / 2;
            float minZoom = minSpace / (targetWidth / frameCount);
            float maxZoom = maxSpace / (targetWidth / frameCount);

            currentZoom = targetSpace / (targetWidth / frameCount);
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

            float spaceAfter = GetSpace();
            float midFrame = (minFrame + maxFrame) / 2f;
            float midPixel = midFrame * spaceAfter;

            notifyScroll.x = midPixel - (targetWidth / 2f) + timelineBorderThickness;
            notifyScroll.x = Mathf.Max(notifyScroll.x, 0);
        }

        private void ToggleFunctionView(bool toFunction)
        {
            if (toFunction)
            {
                // if (selectionHandler.MethodID.Item2)
                //     return;
                //
                // bool has = false;
                // foreach (var cFunc in cachedFunctions)
                // {
                //     if (cFunc.Key == selectionHandler.FunctionValue.Item1)
                //     {
                //         selectionHandler.SelectedFunction = cFunc.Value;
                //         has = true;
                //         break;
                //     }
                // }
                //
                // if (!has)
                // {
                //     selectionHandler.SelectedFunction = null;
                // }
            }
            else
            {
            }
        }

        private void DrawObjectName(float textWidth, Type type)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(GetFriendlyTypeName(type), GUILayout.Width(textWidth));
            EditorGUI.showMixedValue = selectionHandler.ObjectValue.Item2;

            var newValue = EditorGUILayout.ObjectField(selectionHandler.ObjectValue.Item1, type, false);
            if (newValue != selectionHandler.ObjectValue.Item1)
            {
                KUndo.RecordObject("Change Object Value");
                selectionHandler.ObjectValue = (newValue, false);
                selectedClip.UpdateTime();
                KUndo.SaveRecord();
            }

            EditorGUI.showMixedValue = false;
            GUILayout.Space(5f);
            GUILayout.EndHorizontal();
        }

        private void DrawEnumName(float textWidth, Type type)
        {
            GUILayout.BeginHorizontal();
            EditorGUI.showMixedValue = selectionHandler.IntValue.Item2;

            var values = Enum.GetValues(type);
            int newValue = EditorGUILayout.Popup(type.Name, selectionHandler.IntValue.Item1,
                values.Cast<Enum>().Select(e => e.ToString()).ToArray());

            if (newValue != selectionHandler.IntValue.Item1)
            {
                KUndo.RecordObject("Change Enum Value");
                selectionHandler.IntValue = (newValue, false);
                selectedClip.UpdateTime();
                KUndo.SaveRecord();
            }

            EditorGUI.showMixedValue = false;
            GUILayout.Space(5f);
            GUILayout.EndHorizontal();
        }

        private void DrawStringName(float textWidth)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("String", GUILayout.Width(textWidth));
            EditorGUI.showMixedValue = selectionHandler.StringValue.Item2;
            var newValue = EditorGUILayout.DelayedTextField(selectionHandler.StringValue.Item1);
            if (newValue != selectionHandler.StringValue.Item1)
            {
                KUndo.RecordObject("Change String Value");
                selectionHandler.StringValue = (newValue, false);
                selectedClip.UpdateTime();
                KUndo.SaveRecord();
            }

            EditorGUI.showMixedValue = false;
            GUILayout.Space(5f);
            GUILayout.EndHorizontal();
        }

        private void DrawIntName(float textWidth)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Int", GUILayout.Width(textWidth));
            EditorGUI.showMixedValue = selectionHandler.IntValue.Item2;

            var newValue = EditorGUILayout.DelayedIntField(selectionHandler.IntValue.Item1);
            if (newValue != selectionHandler.IntValue.Item1)
            {
                KUndo.RecordObject("Change Int Value");
                selectionHandler.IntValue = (newValue, false);
                selectedClip.UpdateTime();
                KUndo.SaveRecord();
            }

            EditorGUI.showMixedValue = false;
            GUILayout.Space(5f);
            GUILayout.EndHorizontal();
        }

        private void DrawFloatName(float textWidth)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Float", GUILayout.Width(textWidth));
            EditorGUI.showMixedValue = selectionHandler.FloatValue.Item2;

            var newValue = EditorGUILayout.DelayedFloatField(selectionHandler.FloatValue.Item1);
            if (newValue != selectionHandler.FloatValue.Item1)
            {
                KUndo.RecordObject("Change Float Value");
                selectionHandler.FloatValue = (newValue, false);
                selectedClip.UpdateTime();
                KUndo.SaveRecord();
            }

            EditorGUI.showMixedValue = false;
            GUILayout.Space(5f);
            GUILayout.EndHorizontal();
        }

        private void DrawFunctionName(float textWidth)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Function", GUILayout.Width(textWidth));
            EditorGUI.showMixedValue = selectionHandler.FunctionValue.Item2;

            string rawName = selectionHandler.FunctionValue.Item1;

            string label = Method.HasFunctionID(selectionHandler.FunctionValue.Item1)
                ? rawName
                : rawName == ""
                    ? rawName
                    : (rawName + $" {KConst.FunctionNotSupported}");

            string newValue = EditorGUILayout.DelayedTextField(label);

            if (newValue.EndsWith($" {KConst.FunctionNotSupported}"))
                newValue = newValue.Substring(0, newValue.Length - $" {KConst.FunctionNotSupported}".Length);

            if (newValue != selectionHandler.FunctionValue.Item1)
            {
                KUndo.RecordObject("Change Function Value");
                selectionHandler.FunctionValue = (newValue, false);
                selectedClip.UpdateTime();
                KUndo.SaveRecord();
            }

            EditorGUI.showMixedValue = false;
            GUILayout.Space(5f);
            GUILayout.EndHorizontal();
        }

        private float GetSpace(float customWidth = -1)
        {
            float timelineWidth = (customWidth > 0 ? customWidth : rightRect.width) - timelineBorderThickness * 2;
            timelineWidth = Mathf.Max(minSplit, timelineWidth);
            float frameCount = HasValidClip ? selectedClip.FrameCount : 60f;
            float spacePerFrame = (timelineWidth / Mathf.Max(1, frameCount)) * currentZoom;
            return spacePerFrame;
        }

        private float GetPositionXByFrame(int frameIndex)
        {
            return leftRect.width + timelineBorderThickness + GetSpace() * frameIndex - notifyScroll.x;
        }

        private float GetPositonX(int frameIndex)
        {
            return GetSpace() * frameIndex;
        }

        private bool IsRectInsideEditorWindow(Vector2 position, Rect compareRect)
        {
            return position.x + 10 >= 0 &&
                   position.x <= compareRect.width &&
                   position.y + 10 >= 0 &&
                   position.y <= compareRect.height;
        }

        private void SelectAnimator()
        {
            var selection = Selection.activeGameObject;

            if (selection == null)
            {
                Repaint();
                return;
            }
            
            if (Component.TrySelectComponent(this, selection))
            {
                AnimationPreview.IsPreview = false;
                BuildFunctionMenuDropDown();
                FitToScreen();
            }
            else
            {
                FitToScreen();
            }

            if (CanRunEditor && isWindowLocked)
            {
                // foundAnimator = selectedAnimator;
                BuildFunctionMenuDropDown();
                OnRuntimeAnimationControllerChanged(selectedAnimator.runtimeAnimatorController);
                BuildAnimationListMenuDropDown();
                Repaint();
                return;
            }

            Animator foundAnimator = selection.GetComponentInParent<Animator>();

            if (foundAnimator != null)
            {
                selectedAnimator = foundAnimator;

                BuildFunctionMenuDropDown();

                RuntimeAnimatorController controller = foundAnimator.runtimeAnimatorController;

                if (controller == null)
                {
                    Repaint();
                    return;
                }

                OnRuntimeAnimationControllerChanged(controller);

                if (!animatorClips.Any())
                {
                    Repaint();
                    return;
                }

                AnimationClip firstClip = BuildAnimationListMenuDropDown();
                if(firstClip != null)
                    OnAnimationSelected(firstClip);
                else
                    OnAnimationSelected(animatorClips.ElementAt(0));

                FitToScreen();
            }
            else
            {
                runtimeAnimationController = null;
                selectedAnimator = null;
                selectedClip = null;
                FitToScreen();
            }

            Repaint();
        }

        private bool IsClipBelongsToThisAnimationController(KAnimationClipData clipData)
        {
            if (animatorClips == null || clipData == null || !clipData.IsValid) return false;

            return animatorClips.Contains(clipData.Clip);
        }

        private AnimationClip BuildAnimationListMenuDropDown()
        {
            animationListMenu = new GenericMenu();

            // SaveHandler.RebuildDictionary();
            List<KAnimationClipData> saveDatas = SaveHandler.Dict.Values.Where(a => a.IsValid).ToList();
            
            switch (animationSortOrder)
            {
                case KSort.NameAscending:
                    saveDatas.Sort((a, b) => String.Compare(a.Name, b.Name, StringComparison.Ordinal));
                    break;
                case KSort.NameDescending:
                    saveDatas.Sort((a, b) => String.Compare(b.Name, a.Name, StringComparison.Ordinal));
                    break;
                case KSort.NewFirst:
                    saveDatas.Sort((a, b) => b.CreatedTime.CompareTo(a.CreatedTime));
                    break;
                case KSort.OldFirst:
                    saveDatas.Sort((a, b) => a.CreatedTime.CompareTo(b.CreatedTime));
                    break;
                case KSort.LastUpdateFirst:
                    saveDatas.Sort((a, b) => b.LastUpdatedTime.CompareTo(a.LastUpdatedTime));
                    break;
                case KSort.OldUpdateFirst:
                    saveDatas.Sort((a, b) => a.LastUpdatedTime.CompareTo(b.LastUpdatedTime));
                    break;
            }

            AnimationClip output = null;
            string readOnly = " (Read-Only) ";
            foreach (var value in saveDatas)
            {
                if (value.Clip == null) continue;
                if (!showAllClips && !animatorClips.Contains(value.Clip)) continue;

                if (output == null)
                    output = value.Clip;

                // Add menu Item
                var s = SaveHandler.Load(value.Clip);
                bool hasChanges = !s.HasSaved();
                animationListMenu.AddItem(
                    new GUIContent((IsClipBelongsToThisAnimationController(value) ? " " : "- ") + value.Name + (value.IsReadOnly() ? readOnly : "") + (hasChanges ? " *" : "")),
                    HasValidClip && selectedClip == value,
                    data => { OnAnimationSelected(value.Clip); },
                    value.Clip);
            }

            return output;
        }

        private void BuildMenuMenuDropDown()
        {
            menu = new GenericMenu();
            menu.AddItem(new GUIContent("Frames"), !showSeconds, () => { showSeconds = false; });
            menu.AddItem(new GUIContent("Seconds"), showSeconds, () => { showSeconds = true; });

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Show Sample Rates"), showSampleRate,
                () => { showSampleRate = !showSampleRate; });
            string setSampleRate = "Set Sample Rate";
            menu.AddItem(new GUIContent(setSampleRate + "/" + "24"), samples == 24, () => { samples = 24; });
            menu.AddItem(new GUIContent(setSampleRate + "/" + "25"), samples == 25, () => { samples = 25; });
            menu.AddItem(new GUIContent(setSampleRate + "/" + "30"), samples == 30, () => { samples = 30; });
            menu.AddItem(new GUIContent(setSampleRate + "/" + "50"), samples == 50, () => { samples = 50; });
            menu.AddItem(new GUIContent(setSampleRate + "/" + "60"), samples == 60, () => { samples = 60; });

            menu.AddSeparator("");
            // menu.AddItem(new GUIContent("Apply Root Motion"), applyRootMotion, () => applyRootMotion = !applyRootMotion);
            menu.AddItem(new GUIContent("Show Hover Info"), showHoverInfo, () => showHoverInfo = !showHoverInfo);
            menu.AddItem(new GUIContent("Show All Clips"), showAllClips, () =>
            {
                showAllClips = !showAllClips;
                if (!showAllClips && !IsClipBelongsToThisAnimationController(selectedClip))
                {
                    SelectFirstClipInController();
                }
            });
            string setNameTag = "Show Name Tags";
            menu.AddItem(new GUIContent(setNameTag + "/" + "Hide"), eventTooltip == AnimationEventTooltipState.Hide,
                () => { eventTooltip = AnimationEventTooltipState.Hide; });
            menu.AddItem(new GUIContent(setNameTag + "/" + "Hover"), eventTooltip == AnimationEventTooltipState.Hover,
                () => { eventTooltip = AnimationEventTooltipState.Hover; });
            menu.AddItem(new GUIContent(setNameTag + "/" + "Show Always"),
                eventTooltip == AnimationEventTooltipState.ShowAlways,
                () => { eventTooltip = AnimationEventTooltipState.ShowAlways; });

            string setAnimationSortOrder = "Animation Sort Order";
            menu.AddItem(new GUIContent(setAnimationSortOrder + "/" + "Name Ascending"), animationSortOrder == KSort.NameAscending,
                () => { animationSortOrder = KSort.NameAscending; });
            menu.AddItem(new GUIContent(setAnimationSortOrder + "/" + "Name Descending"), animationSortOrder == KSort.NameDescending,
                () => { animationSortOrder = KSort.NameDescending; });
            menu.AddItem(new GUIContent(setAnimationSortOrder + "/" + "Newest First"), animationSortOrder == KSort.NewFirst,
                () => { animationSortOrder = KSort.NewFirst; });
            menu.AddItem(new GUIContent(setAnimationSortOrder + "/" + "Oldest First"), animationSortOrder == KSort.OldFirst,
                () => { animationSortOrder = KSort.OldFirst; });
            menu.AddItem(new GUIContent(setAnimationSortOrder + "/" + "Newest Updated First"), animationSortOrder == KSort.LastUpdateFirst,
                () => { animationSortOrder = KSort.LastUpdateFirst; });
            menu.AddItem(new GUIContent(setAnimationSortOrder + "/" + "Oldest Updated First"), animationSortOrder == KSort.OldUpdateFirst,
                () => { animationSortOrder = KSort.OldUpdateFirst; });
            
            menu.AddItem(new GUIContent("Use Curve For Speed"), useCurveAnimationSpeed,
                () => { useCurveAnimationSpeed = !useCurveAnimationSpeed; });
            if (!useCurveAnimationSpeed)
            {
                string setAnimSpeed = "Set Max Animation Speed";
                menu.AddItem(new GUIContent(setAnimSpeed + "/" + 1), animationSpeedMax == 1,
                    () => { animationSpeedMax = 1; });
                menu.AddItem(new GUIContent(setAnimSpeed + "/" + 2), animationSpeedMax == 2,
                    () => { animationSpeedMax = 2; });
                menu.AddItem(new GUIContent(setAnimSpeed + "/" + 3), animationSpeedMax == 3,
                    () => { animationSpeedMax = 3; });
                menu.AddItem(new GUIContent(setAnimSpeed + "/" + 5), animationSpeedMax == 5,
                    () => { animationSpeedMax = 5; });
            }

            /*
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Experiment Mode"), ExperimentMode, () => { ExperimentMode = !ExperimentMode;});
            */
        }

        private void BuildChannelTemplateDropDown()
        {
            channelLayoutListMenu = new GenericMenu();

            if (SaveHandler.LoadChannelLayouts().Count == 0)
            {
                channelLayoutListMenu.AddItem(new GUIContent("There nothing to show"), false, null);
                return;
            }

            foreach (var channelLayout in SaveHandler.LoadChannelLayouts())
            {
                channelLayoutListMenu.AddItem(new GUIContent(channelLayout.layoutName), channelLayout.layoutName == currentChannelLayout.layoutName,
                    () => { currentChannelLayout = channelLayout;});
            }
        }

        private void ApplyChannelTemplate(KChannelLayout selectedLayout)
        {
            if (!HasValidClip) return;
            
            KUndo.RecordObject("Apply Layout");
            var dataChannels = GetCurrentClipData.Channels;
            for (int i = 0; i < selectedLayout.channelNames.Count; i++)
            {
                string channelName = selectedLayout.channelNames[i];
                if (i >= dataChannels.Count)
                {
                    GetCurrentClipData.AddChannel().name = channelName;
                }
                else
                {
                    dataChannels[i].name = channelName;
                }
            }
            KUndo.SaveRecord();
            Repaint();
        }

        private void AddChannelTemplate()
        {
            if (!HasValidClip) return;
            
            if (string.IsNullOrEmpty(channelName))
            {
                Debug.LogWarning("Channel name cannot be empty");
                return;
            }
                 
            KChannelLayout newLayout = new KChannelLayout(channelName, new List<string>());

            bool allowBlankChannel = true;
            foreach (var channel in GetCurrentClipData.Channels)
            {
                if (!allowBlankChannel && (channel.name == "-" || string.IsNullOrEmpty(channel.name))) continue;
                newLayout.channelNames.Add(channel.name);
            }

            KChannelLayout foundLayout = new KChannelLayout("", new List<string>());
            bool contains = false;
            foreach (var layout in SaveHandler.LoadChannelLayouts())
            {
                if (channelName == layout.layoutName)
                {
                    contains = true;
                    foundLayout = layout;
                    break;
                }
            }

            if (contains)
            {
                bool result = EditorUtility.DisplayDialog(
                    "Confirm Action",
                    "Are you sure you want to overwrite?",
                    "Overwrite",
                    "Cancel"
                );

                if (result)
                {
                    foundLayout.channelNames = newLayout.channelNames;
                    SaveHandler.SaveFile.ChannelLayouts.Add(newLayout);
                    SaveHandler.SaveFileDirty();
                    return;
                }
                else
                {
                    return;
                }
            }
       
            
            SaveHandler.SaveFile.ChannelLayouts.Add(newLayout);
            SaveHandler.SaveFileDirty();
        }

        private void RemoveChannelTemplate()
        {
            SaveHandler.SaveFile.ChannelLayouts.Remove(currentChannelLayout);
            
            SelectFirstOrDefaultChannelLayout();
        }

        private void SelectFirstOrDefaultChannelLayout()
        {
            if (SaveHandler.LoadChannelLayouts().Count > 0)
                currentChannelLayout = SaveHandler.LoadChannelLayouts()[0];
            else
                currentChannelLayout = new KChannelLayout("?", new List<string>());
        }

        private void BuildChannelMenuDropDown(KAnimationChannel channel)
        {
            channelMenu = new GenericMenu();

            channelMenu.AddItem(new GUIContent("Move Up"), false, () => MoveChannelUpAndDown(channel.index, -1));
            channelMenu.AddItem(new GUIContent("Move Down"), false, () => MoveChannelUpAndDown(channel.index, 1));
            channelMenu.AddItem(new GUIContent("Change Name"), false, () => StartEditingChannelName(channel));
            channelMenu.AddSeparator("");
            channelMenu.AddItem(new GUIContent("Remove Events"), false, () => RemoveEventsOnChannel(channel.index));
            channelMenu.AddItem(new GUIContent("Remove Channel"), false, () => RemoveChannel(channel.index));
            channelMenu.AddItem(new GUIContent("Remove Channels Below"), false,
                () => RemoveChannel(channel.index, true));
            if (ExperimentMode)
            {
                channelMenu.AddSeparator("");
            }
        }

        private void BuildTimelineEventMenuDropDown()
        {
            eventMenu = new GenericMenu();

            eventMenu.AddItem(new GUIContent("Add Event"), false, AddEvent);
            eventMenu.AddItem(new GUIContent("Delete Event"), false, DeleteSelectedEvents);
            eventMenu.AddSeparator("");
            eventMenu.AddItem(new GUIContent("Set Color"), false,
                () => { SetAnimationEventColor(selectedEventColor); });
            eventMenu.AddSeparator("");
            eventMenu.AddItem(new GUIContent("Copy Event"), false, TryCopySelectedEvents);
            eventMenu.AddItem(new GUIContent("Paste Event"), false, TryPasteSelectedEvents);
        }

        private void CacheAnimations(AnimationClip[] clips)
        {
            bool hasAdded = false;
            animatorClips = new HashSet<AnimationClip>();
            foreach (var clip in clips)
            {
                if (animatorClips.Contains(clip)) continue;

                animatorClips.Add(clip);
                SaveHandler.Save(clip);
                hasAdded = true;
            }
            
            if(hasAdded)
                SaveHandler.RebuildDictionary();
        }

        private void BuildFunctionMenuDropDown()
        {
            functionMenu = new GenericMenu();
            
            if (!Component.CanUseFunctions()) return;

            Method.GenerateFunctions(Component.SelectedTransform);

            foreach (var function in Method.GetFunctions)
            {
                string paramType = "";
                if (function.GetParameters().Length == 1)
                    paramType = GetFriendlyTypeName(function.GetParameters()[0].ParameterType);

                functionMenu.AddItem(
                    new GUIContent(function.Component.GetType().Name + "/" + function.Name + $" ( {paramType} )"),
                    selectionHandler.FunctionValue.Item1 == function.ID,
                    data => { OnFunctionSelected(function); },
                    0);
            }
        }

        private string GenerateLabel(KAnimationEvent kAnimationEvent)
        {
            if (!HasValidClip) return "";
            
            int frame = selectedClip.FrameByTime(kAnimationEvent.time);
            string methodID = kAnimationEvent.eventFunctionName;
    
            string functionName = Method.HasFunctionID(methodID)
                ? Method.GetClassName(methodID)
                : KConst.FunctionNotSupported;
    
            string label = $"{kAnimationEvent.time:F3} sec (frame {frame})\nClass {functionName}";
    
            if (!Method.GetMethodInfo(methodID, out KMethodHandler.KMethodInfo methodInfo))
                return label;
    
            ParameterInfo param = methodInfo.GetParameters().FirstOrDefault();
            if (param == null)
                return label;

            label += GetParameterLabel(param, kAnimationEvent);
            return label;
        }
        
        private string GetParameterLabel(ParameterInfo param, KAnimationEvent kAnimationEvent)
        {
            Type paramType = param.ParameterType;
    
            if (paramType.IsEnum)
            {
                Enum currentValue = (Enum)Enum.ToObject(paramType, kAnimationEvent.eventInt);
                return $"\n{paramType.Name} : {currentValue}";
            }
    
            if (paramType == typeof(AnimationEvent))
            {
                return $"\nint : {kAnimationEvent.eventInt}" +
                       $"\nfloat : {kAnimationEvent.eventFloat}" +
                       $"\nstring : {kAnimationEvent.eventString}" +
                       $"\nobject : {GetObjectName(kAnimationEvent.eventObject)}";
            }

            switch (Type.GetTypeCode(paramType))
            {
                case TypeCode.String:
                    return $"\n{GetFriendlyTypeName(paramType)} : {kAnimationEvent.eventString}";
                case TypeCode.Int32:
                    return $"\n{GetFriendlyTypeName(paramType)} : {kAnimationEvent.eventInt}";
                case TypeCode.Single:
                    return $"\n{GetFriendlyTypeName(paramType)} : {kAnimationEvent.eventFloat}";
                case TypeCode.Object:
                    return $"\n{GetFriendlyTypeName(paramType)} : {GetObjectName(kAnimationEvent.eventObject)}";
                default:
                    return string.Empty;
            }
        }

        private string GetObjectName(Object obj)
        {
            return obj != null ? obj.name : "None";
        }

        private string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(string)) return "string";
            if (type == typeof(object)) return "object";
            if (type == typeof(void)) return "void";
            if (type.IsEnum) return type.Name;

            return type.Name;
        }

        private void OnRuntimeAnimationControllerChanged(RuntimeAnimatorController controller)
        {
            if (controller == null)
            {
                Debug.LogWarning("Selected Runtime Animation Controller is Null");
                selectedClip = null;
                return;
            }

            runtimeAnimationController = controller;
            CacheAnimations(controller.animationClips);

            if (!showAllClips && !IsClipBelongsToThisAnimationController(selectedClip))
                SelectFirstClipInController();
        }

        private void SelectFirstClipInController()
        {
            OnAnimationSelected(animatorClips.ElementAtOrDefault(0));
        }

        private void AddChannel()
        {
            KUndo.RecordObject("Add Channel");
            selectedClip.AddChannel();
            selectedClip.UpdateTime();
            KUndo.SaveRecord();
            notifyScroll.y = float.MaxValue;
            Repaint();
        }

        private void AddEvent()
        {
            KUndo.RecordObject("Add Event");
            var addedEvent = selectedClip.AddEvent(selectionHandler.selectedChannel, selectedClip.TimeByFrame(currentFrame));
            if (autoApplySelectedColor)
                addedEvent.assignedColor = selectedEventColor;
            
            selectedClip.UpdateTime();
            KUndo.SaveRecord();

            selectionHandler.Select(addedEvent);

            ToggleFunctionView(functionToggled);
        }

        private void TopEventClicked(int clickedFrame)
        {
            selectionHandler.UnselectAll();
            foreach (var e in selectedClip.Events)
            {
                if (selectedClip.FrameByTime(e.time) == clickedFrame)
                {
                    selectionHandler.Select(e, true, true);
                }
            }
        }

        private void EventClicked(KAnimationEvent kAnimationEvent, bool wasHovering)
        {
            selectionHandler.Select(kAnimationEvent, multipleSelectionActive, wasHovering);

            ToggleFunctionView(functionToggled);
        }

        private void OnFunctionSelected(KMethodHandler.KMethodInfo methodInfo)
        {
            KUndo.RecordObject("Select Function");
            // selectionHandler.SelectedFunction = methodInfo;
            selectionHandler.FunctionValue = (methodInfo.Name, false);
            KUndo.SaveRecord();

            SaveHandler.SaveFile.undoMethod = methodInfo;
        }

        private void OnAnimationSelected(AnimationClip clip)
        {
            selectedClip = SaveHandler.Load(clip);
            if (selectedClip == null)
            {
                Debug.LogWarning("Animation Clip Returned Null");
                return;
            }

            selectedClip.RefreshKAnimationClipData(Method);

            KUndo.RecordObject("Select Animation Clip");
            SaveHandler.SaveFile.undoSelected = clip;
            KUndo.SaveRecord();

            lastSavedClip = selectedClip;

            selectionHandler.SelectChannel(0);

            if (currentFrame > selectedClip.FrameCount)
                currentFrame = selectedClip.FrameCount;

            selectionHandler.UnselectAll();
            isReadOnlyClip = KAnimationClipData.IsClipReadOnly(clip);
            FitToScreen();
        }
   
        private void ChangeGuiColor(Color color)
        {
            GUI.color = color;
        }

        private void ResetGuiColor()
        {
            GUI.color = KAnimationColor.OriginalColor;
        }

        private void Reset()
        {
            if (selectedClip == null) return;

            KUndo.RecordObject("Reset Clip");
            selectedClip.Reset();
            selectedClip.UpdateTime();
            KUndo.SaveRecord();
        }

        // TODO : Move Save and load to SaveHandler
        private void SaveCurrent()
        {
            selectedClip = SaveHandler.Load(selectedClip.Clip);
            selectedClip.SaveToAnimation();
            selectedClip.UpdateTime();
            
            EditorUtility.SetDirty(selectedClip.Clip);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void SaveAll()
        {
            foreach (var clipData in SaveHandler.Dict.Values)
            {
                if (!showAllClips && !animatorClips.Contains(clipData.Clip)) continue;

                if (clipData.Clip == null) continue;
                selectedClip = SaveHandler.Load(clipData.Clip);
                selectedClip.SaveToAnimation();
                selectedClip.UpdateTime();
                EditorUtility.SetDirty(selectedClip.Clip);
            }
            
            selectedClip = lastSavedClip;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void Log_KAnimationEvent(KAnimationEvent kevt)
        {
            return;
            // Debug.Log("Method ID :" + kevt.methodID);
        }
    }
}