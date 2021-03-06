﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using JumpyImpl.Blocks;
using JumpyImpl.EventArguments;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Outlining;

namespace JumpyImpl
{
    internal class JumpAdornmentManager
    {
        private readonly IWpfTextView _view;
        private readonly IAdornmentLayer _layer;
        private readonly IOutliningManager _outliningManager;
        //private readonly JumpAdornmentProvider _provider;
        private InputBlock _inputBlock;
        //private List<JumpIndicatorBlock> _indicatorBlocks;
        private Dictionary<char, JumpIndicatorBlock> _indicatorBlocks;
        public char[] ValidChars
        {
            get { return _indicatorBlocks.Keys.ToArray(); }
        }
        private const int A = 65;
        private const int Z = 65 + 25;
        private int _minSearchLineIndex;
        private int _maxSearchLineIndex;
        private SnapshotPoint _minSearchPos;
        private SnapshotPoint _maxSearchPos;
        private int _currentCharInt;
        private char _searchingChar;
        private SnapshotPoint _currentCharPoint;
        public KeyBindingCommandFilter KeyBindingCommandFilter { get; set; }
        private bool _smartJump = true;


        private JumpAdornmentManager(IWpfTextView view, KeyBindingCommandFilter keyBindingCommandFilter, IOutliningManager outliningManager)
        {
            _view = view;
            _view.LayoutChanged += OnLayoutChanged;
            _view.Closed += OnClosed;
            _outliningManager = outliningManager;

            _layer = view.GetAdornmentLayer("JumpyAdornmentLayer");

            _indicatorBlocks = new Dictionary<char, JumpIndicatorBlock>(26);
            KeyBindingCommandFilter = keyBindingCommandFilter;
            KeyBindingCommandFilter.KeyPressed += KeyBindingCommandFilter_KeyPressed;
        }

        private void KeyBindingCommandFilter_KeyPressed(object sender, IndicateKeyPressedEventArgs e)
        {
            try
            {
                char c = char.ToUpperInvariant(e.KeyValue);
                if (_indicatorBlocks.Keys.Contains(c))
                {
                    if (c.Equals(e.KeyValue)) //Hack: Uppercase
                    {
                        var moveToPoint = MoveCaretToPosition(c);
                        SnapshotSpan span = _currentCharPoint <= moveToPoint
                                                ? new SnapshotSpan(_currentCharPoint, moveToPoint)
                                                : new SnapshotSpan(moveToPoint, _currentCharPoint);

                        _view.Selection.Select(span, _currentCharPoint < moveToPoint);
                    }
                    else
                    {
                        MoveCaretToPosition(c);
                    }
                    CleanBlocks();
                    ResetVariables();
                }
                else if ((uint)c == 32) //space
                {
                    if (_currentCharInt >= Z) //need expand search
                    {
                        if (_minSearchPos > 0 || _maxSearchPos < _view.TextViewLines.LastVisibleLine.End)
                        {
                            CleanBlocks();
                            ResetCurrentCharInt();
                            DoSearch();
                            //DoRegularSearch(_view.TextViewLines[_minSearchLineIndex], _view.TextViewLines[_maxSearchLineIndex], _searchingChar);
                        }
                    }
                    /*if (_currentCharInt > A)
                    KeyBindingCommandFilter.Intercept = true;*/
                }
                else
                {
                    CleanBlocks();
                    ResetVariables();
                }
            }
            catch (Exception)
            {
                //TODO: Console.WriteLine(exception);
            }

        }

        private void ResetVariables()
        {
            KeyBindingCommandFilter.Intercept = false;
            _view.Caret.IsHidden = false;
        }

        private SnapshotPoint MoveCaretToPosition(char c)
        {
            /*_view.Caret.MoveTo(_view.TextSnapshot.GetLineFromLineNumber(1).Start);
            var span = _view.TextSnapshot.CreateTrackingSpan(1, 1, SpanTrackingMode.EdgeInclusive);
            _view.ViewScroller.EnsureSpanVisible(span.GetSpan(_view.TextSnapshot));
            return;*/
            //_view.Caret.IsHidden = true;
            var moveToPoint = GetMoveToPoint(c);
            if (!_view.Selection.IsEmpty)
                _view.Selection.Select(new SnapshotSpan(moveToPoint, moveToPoint), false);
            _view.Caret.MoveTo(moveToPoint);
            return moveToPoint;
            //_view.Caret.IsHidden = false;
        }

        private SnapshotPoint GetMoveToPoint(char c)
        {
            var point = _indicatorBlocks[c].Point;
            if (_searchingChar == (char)Key.End)
                return point + 1;
            if (_searchingChar == (char)Key.Home)
                return point;
            if (_smartJump)
            {
                var c1 = point.GetChar();
                if (c1 == ';')
                {
                    return point + 1;
                }
                if (IsAtoZ(c1))
                {
                    if (point.Position != point.GetContainingLine().End)
                    {
                        var nextPoint = point + 1;
                        c1 = nextPoint.GetChar();
                        return IsAtoZ(c1) ? point : nextPoint;
                    }
                    return point + 1;
                }
            }
            return point;
        }

        private static bool IsAtoZ(char c, bool ignoreCase = true)
        {
            if (ignoreCase)
                return char.ToUpperInvariant(c) >= A && char.ToUpperInvariant(c) <= Z;
            return c >= A && c <= Z;
        }

        public static JumpAdornmentManager Create(IWpfTextView view, KeyBindingCommandFilter keyBindingCommandFilter,IOutliningManager outliningManager)
        {
            return view.Properties.GetOrCreateSingletonProperty(() => new JumpAdornmentManager(view, keyBindingCommandFilter, outliningManager));
        }

        private void OnClosed(object sender, EventArgs e)
        {
            KeyBindingCommandFilter.KeyPressed -= KeyBindingCommandFilter_KeyPressed;
            _view.LayoutChanged -= OnLayoutChanged;
            _view.Closed -= OnClosed;
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            /*//Get all of the comments that intersect any of the new or reformatted lines of text.
            List<CommentAdornment> newComments = new List<CommentAdornment>();

            //The event args contain a list of modified lines and a NormalizedSpanCollection of the spans of the modified lines.  
            //Use the latter to find the comments that intersect the new or reformatted lines of text. 
            foreach (Span span in e.NewOrReformattedSpans)
            {
                newComments.AddRange(this.provider.GetComments(new SnapshotSpan(this._view.TextSnapshot, span)));
            }

            //It is possible to get duplicates in this list if a comment spanned 3 lines, and the first and last lines were modified but the middle line was not. 
            //Sort the list and skip duplicates.
            newComments.Sort((a, b) => a.GetHashCode().CompareTo(b.GetHashCode()));

            CommentAdornment lastComment = null;
            foreach (CommentAdornment comment in newComments)
            {
                if (comment != lastComment)
                {
                    lastComment = comment;
                    this.DrawComment(comment);
                }
            }*/
            if (e.NewSnapshot != e.OldSnapshot)
            {
                CleanBlocks();
                ResetVariables();
            }

        }

        public void Execute()
        {
            try
            {
                KeyBindingCommandFilter.AdornmentManager = this;

                DrawInputBox();
            }
            catch (Exception)
            {
                //TODO:Console.WriteLine(exception);
            }
        }

        private void DrawInputBox()
        {
            CleanBlocks();
            KeyBindingCommandFilter.Intercept = false;

            ITextCaret caret = _view.Caret;

            var currentCharPoint = caret.Position.Point.GetPoint(_view.TextBuffer, caret.Position.Affinity);
            if (!currentCharPoint.HasValue)
                return;

            _currentCharPoint = currentCharPoint.Value;
            //TextBounds bounds = caret.ContainingTextViewLine.GetCharacterBounds(currentCharPoint.Value);

            var left = caret.ContainingTextViewLine.Left + caret.Left;
            var top = caret.Top;
            var width = caret.Width + 15;
            var height = caret.Height;
            _inputBlock = new InputBlock(left, top, width, height);
            _inputBlock.JumpTextChanged += InputBlock_JumpTextChanged;
            var snapshotSpan = new SnapshotSpan(_view.TextBuffer.CurrentSnapshot, 0, _view.TextBuffer.CurrentSnapshot.Length);
            _layer.AddAdornment(snapshotSpan, _inputBlock, _inputBlock);

            caret.IsHidden = true;
            _inputBlock.SetFocus();

        }

        private void CleanBlocks()
        {
            CleanInputBox();
            CleanIndicateBoxes();
        }

        private void CleanInputBox()
        {
            if (_inputBlock != null)
            {
                _inputBlock.JumpTextChanged -= InputBlock_JumpTextChanged;
                _layer.RemoveAdornmentsByTag(_inputBlock);
                _inputBlock = null;
            }
        }

        private void CleanIndicateBoxes()
        {
            foreach (var key in _indicatorBlocks.Keys)
            {
                _layer.RemoveAdornmentsByTag(key);
            }
            _indicatorBlocks.Clear();
        }

        void InputBlock_JumpTextChanged(object sender, JumpTextChangedEventArgs e)
        {
            try
            {
                ITextCaret caret = _view.Caret;
                caret.IsHidden = false;
                _view.VisualElement.Focus();
                //caret.MoveTo(caret.Position.Point.GetPoint(_view.TextBuffer, caret.Position.Affinity).Value);
                _searchingChar = e.SearchingChar;

                if (_searchingChar == (char)Key.Escape) //user pressed escape key
                {
                    CleanBlocks();
                    ResetVariables();
                    return;
                }

                _inputBlock.Visibility = Visibility.Hidden;

                int currentLineIndex = GetCurrentLineIndex();
                if (currentLineIndex < 0)
                    return;

                ResetCurrentCharInt();

                _maxSearchLineIndex = currentLineIndex;
                _minSearchLineIndex = currentLineIndex;

                SnapshotPoint? currentCharPoint = caret.Position.Point.GetPoint(_view.TextBuffer, caret.Position.Affinity);
                if (!currentCharPoint.HasValue)
                    return;

                //var currentLine = _view.TextViewLines[currentLineIndex];
                _maxSearchPos = GetNextPos(_view.TextViewLines[_maxSearchLineIndex], currentCharPoint.Value);
                _minSearchPos = GetPrevPos(_view.TextViewLines[_minSearchLineIndex], currentCharPoint.Value);

                DoSearch();

                if (_currentCharInt >= A)
                    KeyBindingCommandFilter.Intercept = true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private int GetCurrentLineIndex()
        {
            int currentIndex = -1;
            var currentLine = _view.Caret.ContainingTextViewLine;
            for (int i = 0; i < _view.TextViewLines.Count; i++)
            {
                if (_view.TextViewLines[i] == currentLine)
                {
                    currentIndex = i;
                    break;
                }
            }

            return currentIndex;
        }

        private static SnapshotPoint GetPrevPos(IWpfTextViewLine line, SnapshotPoint currentCharPoint)
        {
            if (currentCharPoint == line.Start)
                return currentCharPoint;
            return currentCharPoint - 1;
        }

        private static SnapshotPoint GetNextPos(IWpfTextViewLine line, SnapshotPoint currentCharPoint)
        {
            if (currentCharPoint == line.End)
                return currentCharPoint;
            return currentCharPoint + 1;
        }

        #region Search

        private void DoSearch()
        {
            switch (_searchingChar)
            {
                case (char) Key.Home:
                    DoLineHeadSearch();
                    break;
                case (char) Key.End:
                    DoLineTailSearch();
                    break;
                default:
                    DoRegularSearch(GetPrevLine(), GetNextLine(), _searchingChar);
                    break;
            }
        }

        private void DoLineHeadSearch()
        {
            if (IsInitLine())
            {
                var currentLine = GetInitLine();
                FindValidHeadAndCreateIndicateBox(currentLine);
            }

            while (!ExhaustedIndicators())
            {
                if (IsPrevLineValid())
                {
                    MoveToPrevLine();
                    var currentLine = GetPrevLine();
                    FindValidHeadAndCreateIndicateBox(currentLine);
                    if (ExhaustedIndicators())
                        break;
                }

                if (IsNextLineValid())
                {
                    MoveToNextLine();
                    var currentLine = GetNextLine();
                    FindValidHeadAndCreateIndicateBox(currentLine);
                    if (ExhaustedIndicators())
                        break;
                }

                if (ExhaustedVisibleLines())
                    break;
            }
        }

        private void DoLineTailSearch()
        {
            if (IsInitLine())
            {
                var currentLine = GetPrevLine(); //which is the current line
                if (currentLine.Start != currentLine.End)
                {
                    FindValidTailAndCreateIndicateBox(currentLine);
                }
            }

            while (!ExhaustedIndicators())
            {
                if (IsPrevLineValid())
                {
                    MoveToPrevLine();
                    var currentLine = GetPrevLine();
                    if (currentLine.Start != currentLine.End)
                    {
                        FindValidTailAndCreateIndicateBox(currentLine);
                        if (ExhaustedIndicators())
                            break;
                    }
                }

                if (IsNextLineValid())
                {
                    MoveToNextLine();
                    var currentLine = GetNextLine();
                    if (currentLine.Start != currentLine.End)
                    {
                        FindValidTailAndCreateIndicateBox(currentLine);
                        if (ExhaustedIndicators())
                            break;
                    }
                }

                if (ExhaustedVisibleLines())
                    break;
            }
        }

        private void DoRegularSearch(IWpfTextViewLine upLine, IWpfTextViewLine downLine, char searchingChar)
        {
            SearchUp(upLine, searchingChar);

            SearchDown(downLine, searchingChar);

            while (!ExhaustedIndicators())
            {
                if (IsPrevLineValid())
                {
                    MoveToPrevLine();
                    var currentLine = GetPrevLine();
                    _minSearchPos = currentLine.End;
                    SearchUp(currentLine, searchingChar);
                    if (ExhaustedIndicators())
                        break;
                }

                if (IsNextLineValid())
                {
                    MoveToNextLine();
                    var currentLine = GetNextLine();
                    _maxSearchPos = currentLine.Start;
                    SearchDown(currentLine, searchingChar);
                    if (ExhaustedIndicators())
                        break;
                }

                if (ExhaustedVisibleLines())
                    break;
            }
        }

        private void SearchUp(IWpfTextViewLine currentLine, char searchingChar)
        {
            if (IsCollaspsedLine(currentLine))
                return;

            while (_minSearchPos >= currentLine.Start && !ExhaustedIndicators())
            {
                SearchAndCreateIndicateBox(_minSearchPos, searchingChar, currentLine);
                if (_minSearchPos == currentLine.Start)
                    break;
                _minSearchPos = _minSearchPos - 1;
            }
        }

        private void SearchDown(IWpfTextViewLine currentLine, char searchingChar)
        {
            if (IsCollaspsedLine(currentLine))
                return;

            while (_maxSearchPos < currentLine.End && !ExhaustedIndicators())
            {
                SearchAndCreateIndicateBox(_maxSearchPos, searchingChar, currentLine);
                _maxSearchPos = _maxSearchPos + 1;
            }
        }

        private bool IsCollaspsedLine(IWpfTextViewLine line)
        {
            var regions = _outliningManager.GetCollapsedRegions(
                line.Snapshot.CreateTrackingSpan(line.Start, line.Length, SpanTrackingMode.EdgeInclusive)
                    .GetSpan(line.Snapshot), false);

            return regions.Any();
        }
        
        private bool IsInitLine()
        {
            return _minSearchLineIndex == _maxSearchLineIndex;
        }

        private IWpfTextViewLine GetInitLine()
        {
            if (IsInitLine())
                return GetPrevLine();
            throw new Exception("Search is no longer at initial position");
        }

        private IWpfTextViewLine GetPrevLine()
        {
            return _view.TextViewLines[_minSearchLineIndex];
        }

        private IWpfTextViewLine GetNextLine()
        {
            return _view.TextViewLines[_maxSearchLineIndex];
        }

        private void MoveToPrevLine()
        {
            _minSearchLineIndex--;
        }

        private void MoveToNextLine()
        {
            _maxSearchLineIndex++;
        }

        private bool ExhaustedIndicators()
        {
            return _currentCharInt >= Z;
        }
        
        private bool IsNextLineValid()
        {
            return _maxSearchLineIndex < _view.TextViewLines.Count - 1;
        }

        private bool IsPrevLineValid()
        {
            return _minSearchLineIndex > 0;
        }

        private bool ExhaustedVisibleLines()
        {
            return !IsPrevLineValid() && !IsNextLineValid();
        }

        private void FindValidTailAndCreateIndicateBox(IWpfTextViewLine currentLine)
        {
            var point = currentLine.End - 1;
            if (IsPointVisible(currentLine, point))
                CreateIndicateBox(point, currentLine, (char) ++_currentCharInt);
        }
        
        private void FindValidHeadAndCreateIndicateBox(IWpfTextViewLine currentLine)
        {
            var head = FindValidHead(currentLine);
            if (IsPointVisible(currentLine, head))
                CreateIndicateBox(head, currentLine, (char) ++_currentCharInt);
        }

        private static bool IsPointVisible(IWpfTextViewLine currentLine, SnapshotPoint point)
        {
            //HACK:Assume the currentline is always the only line of SnapshotLine.
            var bound = currentLine.GetCharacterBounds(point);
            var right = currentLine.VisibleArea.TopRight.X;
            var left = currentLine.VisibleArea.TopLeft.X;
            return bound.Left >= left && bound.Right <= right;
        }

        private static SnapshotPoint FindValidHead(IWpfTextViewLine currentLine)
        {
            SnapshotPoint pos = currentLine.Start;
            while (pos < currentLine.End)
            {
                var c = pos.GetChar();
                if (c != ' ' && c != '\t' && c != '\r' && c != '\n')
                {
                    return pos;
                }
                pos = pos + 1;
            }
            return currentLine.Start;
        }

        
        #endregion

        

        private void ResetCurrentCharInt()
        {
            _currentCharInt = A - 1;
        }

        private void SearchAndCreateIndicateBox(SnapshotPoint point, char searchingChar, IWpfTextViewLine currentLine)
        {
            if (!IsPointVisible(currentLine, point))
                return;

            var c = point.GetChar();
            if (char.ToUpperInvariant(c) == char.ToUpperInvariant(searchingChar))
            {
                var displayChar = (char)(++_currentCharInt);
                CreateIndicateBox(point, currentLine, displayChar);
            }
        }

        private void CreateIndicateBox(SnapshotPoint point, IWpfTextViewLine currentLine, char displayChar)
        {
            try
            {
                var bound = currentLine.GetCharacterBounds(point);
                JumpIndicatorBlock block = new JumpIndicatorBlock(bound.Left, bound.Top, bound.Width, bound.Height,
                                                                  displayChar, point);
                var snapshotSpan = new SnapshotSpan(point, 1);
                _layer.AddAdornment(snapshotSpan, displayChar, block);
                _indicatorBlocks.Add(displayChar, block);
            }
            catch (Exception exception)
            {
                //TODO: log exception message.
                //Console.WriteLine(exception.Message);
            }

        }

        


    }
}