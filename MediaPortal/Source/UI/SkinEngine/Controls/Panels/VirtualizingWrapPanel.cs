#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.Utils;
using MediaPortal.Utilities;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  public class VirtualizingWrapPanel : WrapPanel, IVirtualizingPanel
	{
    #region Protected fields

    protected IItemProvider _itemProvider = null;

    // Assigned in Arrange
    protected IList<FrameworkElement> _arrangedItems = new List<FrameworkElement>();

    // Assigned in CalculateInnerDesiredSize
    protected float _averageItemSize = 0;

    protected IItemProvider _newItemProvider = null; // Store new item provider until next render cylce

    #endregion

    #region Ctor

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      VirtualizingWrapPanel p = (VirtualizingWrapPanel)source;
      _itemProvider = copyManager.GetCopy(p._itemProvider);
      _arrangedItems.Clear();
      _averageItemSize = 0;
    }

    public override void Dispose()
    {
      base.Dispose();
      IItemProvider itemProvider = _itemProvider;
      _itemProvider = null;
      if (itemProvider != null)
        MPF.TryCleanupAndDispose(itemProvider);
      itemProvider = _newItemProvider;
      _newItemProvider = null;
      if (itemProvider != null)
        MPF.TryCleanupAndDispose(itemProvider);
    }

    #endregion

    #region Public properties

    public IItemProvider ItemProvider
    {
      get { return _itemProvider; }
    }

    public bool IsVirtualizing
    {
      get { return _itemProvider != null; }
    }

    #endregion

    public void SetItemProvider(IItemProvider itemProvider)
    {
      if (_elementState == ElementState.Running)
        lock (Children.SyncRoot)
        {
          if (_newItemProvider == itemProvider)
            return;
          if (_newItemProvider != null)
            MPF.TryCleanupAndDispose(_newItemProvider);
          _newItemProvider = null;
          if (_itemProvider == itemProvider)
            return;
          _newItemProvider = itemProvider;
        }
      else
      {
        if (_newItemProvider == itemProvider)
          return;
        if (_newItemProvider != null)
          MPF.TryCleanupAndDispose(_newItemProvider);
        _newItemProvider = null;
        if (_itemProvider == itemProvider)
          return;
        if (_itemProvider != null)
          MPF.TryCleanupAndDispose(_itemProvider);
        _itemProvider = itemProvider;
      }
      InvalidateLayout(true, true);
    }


    protected LineMeasurement CalculateLine(int startIndex, SizeF? measureSize, bool invertLayoutDirection)
    {
      LineMeasurement result = LineMeasurement.Create();
      if (invertLayoutDirection)
        result.EndIndex = startIndex;
      else
        result.StartIndex = startIndex;
      result.TotalExtendsInNonOrientationDirection = 0;
      int numChildren = ItemProvider.NumItems;
      int directionOffset = invertLayoutDirection ? -1 : 1;
      float offsetInOrientationDirection = 0;
      float extendsInOrientationDirection = measureSize.HasValue ? GetExtendsInOrientationDirection(Orientation, measureSize.Value) : float.NaN;
      int currentIndex = startIndex;
      for (; invertLayoutDirection ? (currentIndex >= 0) : (currentIndex < numChildren); currentIndex += directionOffset)
      {
        FrameworkElement child = GetElement(currentIndex);
        SizeF desiredChildSize;
        if (measureSize.HasValue)
        {
          desiredChildSize = measureSize.Value;
          child.Measure(ref desiredChildSize);
        }
        else
          desiredChildSize = child.DesiredSize;
        float lastOffsetInOrientationDirection = offsetInOrientationDirection;
        offsetInOrientationDirection += GetExtendsInOrientationDirection(Orientation, desiredChildSize);
        if (!float.IsNaN(extendsInOrientationDirection) && offsetInOrientationDirection > extendsInOrientationDirection)
        {
          if (invertLayoutDirection)
            result.StartIndex = currentIndex + 1;
          else
            result.EndIndex = currentIndex - 1;
          result.TotalExtendsInOrientationDirection = lastOffsetInOrientationDirection;
          return result;
        }
        if (desiredChildSize.Height > result.TotalExtendsInNonOrientationDirection)
          result.TotalExtendsInNonOrientationDirection = desiredChildSize.Height;
      }
      if (invertLayoutDirection)
        result.StartIndex = currentIndex + 1;
      else
        result.EndIndex = currentIndex - 1;
      result.TotalExtendsInOrientationDirection = offsetInOrientationDirection;
      return result;
    }

    protected void LayoutLine(PointF pos, LineMeasurement line)
    {
      float offset = 0;
      for (int i = line.StartIndex; i <= line.EndIndex; i++)
      {
        FrameworkElement layoutChild = GetItem(i, ItemProvider, true);
        SizeF desiredChildSize = layoutChild.DesiredSize;
        SizeF size;
        PointF location;

        if (Orientation == Orientation.Horizontal)
        {
          size = new SizeF(desiredChildSize.Width, line.TotalExtendsInNonOrientationDirection);
          location = new PointF(pos.X + offset, pos.Y);
          ArrangeChildVertical(layoutChild, layoutChild.VerticalAlignment, ref location, ref size);
          offset += desiredChildSize.Width;
        }
        else
        {
          size = new SizeF(line.TotalExtendsInNonOrientationDirection, desiredChildSize.Height);
          location = new PointF(pos.X, pos.Y + offset);
          ArrangeChildHorizontal(layoutChild, layoutChild.HorizontalAlignment, ref location, ref size);
          offset += desiredChildSize.Height;
        }

        layoutChild.Arrange(new RectangleF(location, size));

        _arrangedItems.Add(layoutChild);
      }
    }

    protected override void ArrangeChildren()
    {
      bool fireScrolled = false;
      lock (Children.SyncRoot)
      {
        if (ItemProvider == null)
        {
          base.ArrangeChildren();
          return;
        }

        _totalHeight = 0;
        _totalWidth = 0;
        int numItems = ItemProvider.NumItems;
        if (numItems > 0)
        {
          PointF actualPosition = ActualPosition;
          SizeF actualSize = new SizeF((float)ActualWidth, (float)ActualHeight);

          // For Orientation == vertical, this is ActualHeight, for horizontal it is ActualWidth
          float actualExtendsInOrientationDirection = GetExtendsInOrientationDirection(Orientation, actualSize);
          // For Orientation == vertical, this is ActualWidth, for horizontal it is ActualHeight
          float actualExtendsInNonOrientationDirection = GetExtendsInNonOrientationDirection(Orientation, actualSize);
          // Hint: We cannot skip the arrangement of lines above _actualFirstVisibleLineIndex or below _actualLastVisibleLineIndex
          // because the rendering and focus system also needs the bounds of the currently invisible children
          float startPosition = 0;
          // If set to true, we'll check available space from the last to first visible child.
          // That is necessary if we want to scroll a specific child to the last visible position.
          bool invertLayouting = false;
          lock (_renderLock)
            if (_pendingScrollIndex.HasValue)
            {
              fireScrolled = true;
              int pendingSI = _pendingScrollIndex.Value;
              if (_scrollToFirst)
                _actualFirstVisibleLineIndex = pendingSI;
              else
              {
                _actualLastVisibleLineIndex = pendingSI;
                invertLayouting = true;
              }
              _pendingScrollIndex = null;
            }

          // todo : honor _actualFirstVisibleLineIndex since we might have scrolled! -> for that we need to know how many items fit on one line!
          int _averageItemsPerLine = 0;
          if (_arrangedLines.Count > 0 && _arrangedItems.Count > 0)
            _averageItemsPerLine = (int)Math.Round((float)_arrangedItems.Count / _arrangedLines.Count);
          _arrangedItems.Clear();
          _arrangedLines.Clear();
          int index = _actualFirstVisibleLineIndex * _averageItemsPerLine;
          float accumulatedExtendsInNonOrientationDirection = 0.0f;
          while (index < numItems && accumulatedExtendsInNonOrientationDirection < actualExtendsInNonOrientationDirection)
          {
            LineMeasurement line = CalculateLine(index, _innerRect.Size, false);
            _arrangedLines.Add(line);
            index = line.EndIndex + 1;
            accumulatedExtendsInNonOrientationDirection += line.TotalExtendsInNonOrientationDirection;
          }

          // 1) Calculate scroll indices
          if (_doScroll)
          { // Calculate last visible child
            float spaceLeft = actualExtendsInNonOrientationDirection;
            if (invertLayouting)
            {
              CalcHelper.Bound(ref _actualLastVisibleLineIndex, 0, numItems - 1);
              _actualFirstVisibleLineIndex = _actualLastVisibleLineIndex + 1;
              while (_actualFirstVisibleLineIndex > 0)
              {
                LineMeasurement line = _arrangedLines[_actualFirstVisibleLineIndex - 1];
                spaceLeft -= line.TotalExtendsInNonOrientationDirection;
                if (spaceLeft + DELTA_DOUBLE < 0)
                  break;
                _actualFirstVisibleLineIndex--;
              }

              if (spaceLeft > 0)
              { // Correct the last scroll index to fill the available space
                int maxArrangedLine = _arrangedLines.Count - 1;
                while (_actualLastVisibleLineIndex < maxArrangedLine)
                {
                  LineMeasurement line = _arrangedLines[_actualLastVisibleLineIndex + 1];
                  spaceLeft -= line.TotalExtendsInNonOrientationDirection;
                  if (spaceLeft + DELTA_DOUBLE < 0)
                    break; // Found item which is not visible any more
                  _actualLastVisibleLineIndex++;
                }
              }
            }
            else
            {
              //CalcHelper.Bound(ref _actualFirstVisibleLineIndex, 0, _arrangedLines.Count - 1);
              _actualLastVisibleLineIndex = _actualFirstVisibleLineIndex - 1;
              while (_actualLastVisibleLineIndex - _actualFirstVisibleLineIndex + 1 < _arrangedLines.Count)
              {
                LineMeasurement line = _arrangedLines[_actualLastVisibleLineIndex - _actualFirstVisibleLineIndex + 1];
                spaceLeft -= line.TotalExtendsInNonOrientationDirection;
                if (spaceLeft + DELTA_DOUBLE < 0)
                  break;
                _actualLastVisibleLineIndex++;
              }

              /*if (spaceLeft > 0)
              { // Correct the first scroll index to fill the available space
                while (_actualFirstVisibleLineIndex > 0)
                {
                  LineMeasurement line = _arrangedLines[_actualFirstVisibleLineIndex - 1];
                  spaceLeft -= line.TotalExtendsInNonOrientationDirection;
                  if (spaceLeft + DELTA_DOUBLE < 0)
                    break; // Found item which is not visible any more
                  _actualFirstVisibleLineIndex--;
                }
              }*/
            }
          }
          else
          {
            _actualFirstVisibleLineIndex = 0;
            _actualLastVisibleLineIndex = _arrangedLines.Count - 1;
          }
          /*
          // 2) Calculate start position
          for (int i = 0; i < _actualFirstVisibleLineIndex; i++)
          {
            LineMeasurement line = _arrangedLines[i];
            startPosition -= line.TotalExtendsInNonOrientationDirection;
          }
          */
          // 3) Arrange children
          if (Orientation == Orientation.Vertical)
            _totalHeight = actualExtendsInOrientationDirection;
          else
            _totalWidth = actualExtendsInOrientationDirection;
          PointF position = Orientation == Orientation.Vertical ?
              new PointF(actualPosition.X + startPosition, actualPosition.Y) :
              new PointF(actualPosition.X, actualPosition.Y + startPosition);
          foreach (LineMeasurement line in _arrangedLines)
          {
            LayoutLine(position, line);

            startPosition += line.TotalExtendsInNonOrientationDirection;
            if (Orientation == Orientation.Vertical)
            {
              position = new PointF(actualPosition.X + startPosition, actualPosition.Y);
              _totalWidth += line.TotalExtendsInNonOrientationDirection;
            }
            else
            {
              position = new PointF(actualPosition.X, actualPosition.Y + startPosition);
              _totalHeight += line.TotalExtendsInNonOrientationDirection;
            }
          }

          _averageItemsPerLine = (int)Math.Round((float)_arrangedItems.Count / _arrangedLines.Count);

          int numInvisible = numItems - _arrangedItems.Count; // Items which have not been arranged above, i.e. item extends have not been added to _totalHeight / _totalWidth
          float invisibleRequiredSize = numInvisible / _averageItemsPerLine * _averageItemSize;
          if (_doScroll)
            invisibleRequiredSize += actualExtendsInOrientationDirection % _averageItemSize; // Size gap from the last item to the end of the actual extends
          if (Orientation == Orientation.Horizontal)
            _totalHeight += invisibleRequiredSize;
          else
            _totalWidth += invisibleRequiredSize;

          _itemProvider.Keep(_arrangedLines.First().StartIndex,
              _arrangedLines.Last().EndIndex);

        }
        else
        {
          _actualFirstVisibleLineIndex = 0;
          _actualLastVisibleLineIndex = -1;
        }
      }
      if (fireScrolled)
        InvokeScrolled();
    }

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      FrameworkElementCollection children = Children;
      lock (children.SyncRoot)
      {
        if (_newItemProvider != null)
        {
          if (children.Count > 0)
            children.Clear(false);
          if (_itemProvider != null)
            MPF.TryCleanupAndDispose(_itemProvider);
          _itemProvider = _newItemProvider;
          _newItemProvider = null;
          _updateRenderOrder = true;
        }
        _averageItemSize = 0;
        IItemProvider itemProvider = ItemProvider;
        if (itemProvider == null)
          return base.CalculateInnerDesiredSize(totalSize);
        int numItems = itemProvider.NumItems;
        if (numItems == 0)
          return SizeF.Empty;

        SizeF resultSize;
        // Get all viewable children (= visible children inside our range)
        IList<FrameworkElement> exemplaryChildren = GetMeasuredViewableChildren(totalSize, out resultSize);
        if (exemplaryChildren.Count == 0)
        { // Might be the case if no item matches into totalSize. Fallback: Use the first visible item.
          for (int i = 0; i < numItems; i++)
          {
            FrameworkElement item = GetItem(i, itemProvider, true);
            if (item == null || !item.IsVisible)
              continue;
            exemplaryChildren.Add(item);
            break;
          }
        }
        if (exemplaryChildren.Count == 0)
          return SizeF.Empty;

        LineMeasurement line = CalculateLine(exemplaryChildren, 0, totalSize, false);
        resultSize.Height = line.TotalExtendsInOrientationDirection;
        resultSize.Width = line.TotalExtendsInNonOrientationDirection;

        _averageItemSize = GetExtendsInOrientationDirection(Orientation, resultSize) / exemplaryChildren.Count;
        return Orientation == Orientation.Vertical ? new SizeF(resultSize.Width, resultSize.Height * numItems / exemplaryChildren.Count) :
            new SizeF(resultSize.Width * numItems / exemplaryChildren.Count, resultSize.Height);

        /*

        int numVisibleChildren = visibleChildren.Count;
        if (numVisibleChildren == 0)
          return SizeF.Empty;
        float totalDesiredWidth = 0;
        float totalDesiredHeight = 0;
        int index = 0;
        while (index < numVisibleChildren)
        {
          LineMeasurement line = CalculateLine(visibleChildren, index, totalSize, false);
          if (line.EndIndex < line.StartIndex)
            // Element doesn't fit
            break;
          switch (Orientation)
          {
            case Orientation.Horizontal:
              if (line.TotalExtendsInOrientationDirection > totalDesiredWidth)
                totalDesiredWidth = line.TotalExtendsInOrientationDirection;
              totalDesiredHeight += line.TotalExtendsInNonOrientationDirection;
              break;
            case Orientation.Vertical:
              if (line.TotalExtendsInOrientationDirection > totalDesiredHeight)
                totalDesiredHeight = line.TotalExtendsInOrientationDirection;
              totalDesiredWidth += line.TotalExtendsInNonOrientationDirection;
              break;
          }
          index = line.EndIndex + 1;
        }
        return new SizeF(totalDesiredWidth, totalDesiredHeight);
        */
      }
    }

    // It's actually "GetVisibleChildren", but that member already exists in Panel
    protected IList<FrameworkElement> GetMeasuredViewableChildren(SizeF totalSize, out SizeF resultSize)
    {
      resultSize = SizeF.Empty;
      return new List<FrameworkElement>();
    }

    protected FrameworkElement GetItem(int childIndex, IItemProvider itemProvider, bool forceMeasure)
    {
      lock (Children.SyncRoot)
      {
        bool newlyCreated;
        FrameworkElement item = itemProvider.GetOrCreateItem(childIndex, this, out newlyCreated);
        if (item == null)
          return null;
        if (newlyCreated)
        {
          // VisualParent and item.Screen were set by the item provider
          item.SetElementState(ElementState.Preparing);
          if (_elementState == ElementState.Running)
            item.SetElementState(ElementState.Running);
        }
        if (newlyCreated || forceMeasure)
        {
          SizeF childSize = Orientation == Orientation.Vertical ? new SizeF((float)ActualWidth, float.NaN) :
              new SizeF(float.NaN, (float)ActualHeight);
          item.Measure(ref childSize);
        }
        return item;
      }
    }

    public override FrameworkElement GetElement(int index)
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
        return base.GetElement(index);

      lock (Children.SyncRoot)
        return GetItem(index, itemProvider, true);
    }

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
      {
        base.AddChildren(childrenOut);
        return;
      }

      lock (Children.SyncRoot)
        CollectionUtils.AddAll(childrenOut, _arrangedItems);
    }

    protected override IEnumerable<FrameworkElement> GetRenderedChildren()
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
        return base.GetRenderedChildren();

      if (_actualFirstVisibleLineIndex < 0 || _actualLastVisibleLineIndex < _actualFirstVisibleLineIndex)
        return new List<FrameworkElement>();      
      int start = _arrangedLines[0].StartIndex;
      int end = _arrangedLines[_actualLastVisibleLineIndex - _actualFirstVisibleLineIndex].EndIndex;
      int amount = end - start;
      return _arrangedItems.Take(amount + 1);//.Skip(start).Take(end - start + 1);
    }

    protected override void MakeChildVisible(UIElement element, ref RectangleF elementBounds)
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
      {
        base.MakeChildVisible(element, ref elementBounds);
        return;
      }

      if (_doScroll)
      {
        int lineIndex = 0;

        IList<FrameworkElement> arrangedItemsCopy;
        lock (Children.SyncRoot)
        {
          arrangedItemsCopy = new List<FrameworkElement>(_arrangedItems);
        }
        IList<LineMeasurement> lines = new List<LineMeasurement>(_arrangedLines);
        foreach (FrameworkElement currentChild in arrangedItemsCopy)
        {
          if (InVisualPath(currentChild, element))
          {
            int oldFirstVisibleLine = _actualFirstVisibleLineIndex;
            int oldLastVisibleLine = _actualLastVisibleLineIndex;
            bool first;
            if (lineIndex < oldFirstVisibleLine)
              first = true;
            else if (lineIndex <= oldLastVisibleLine)
              // Already visible
              break;
            else
              first = false;
            SetScrollIndex(lineIndex, first);
            // Adjust the scrolled element's bounds; Calculate the difference between positions of childen at old/new child indices
            float extendsInOrientationDirection = SumActualLineExtendsInNonOrientationDirection(lines,
                first ? oldFirstVisibleLine : oldLastVisibleLine, lineIndex);
            if (Orientation == Orientation.Horizontal)
              elementBounds.X -= extendsInOrientationDirection;
            else
              elementBounds.Y -= extendsInOrientationDirection;
            break;
          }
        }
        lineIndex++;
      }
    }
	}
}
