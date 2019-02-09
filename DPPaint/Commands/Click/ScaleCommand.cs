﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using DPPaint.Extensions;
using DPPaint.Shapes;
using DPPaint.Visitor;

namespace DPPaint.Commands.Click
{
    public class ScaleCommand : ICanvasCommand
    {
        /// <inheritdoc />
        public PointerRoutedEventArgs PointerEventArgs { get; set; }
        /// <inheritdoc />
        public Canvas Canvas { get; set; }
        /// <inheritdoc />
        public Stack<List<PaintBase>> UndoStack { get; set; }
        /// <inheritdoc />
        public Stack<List<PaintBase>> RedoStack { get; set; }
        /// <inheritdoc />
        public List<PaintBase> ShapeList { get; set; }
        /// <inheritdoc />

        private readonly ICanvasPage _page;

        private Point _prevPointer;
        private List<PaintBase> _selected;

        public ScaleCommand(ICanvasPage page)
        {
            _page = page;
        }

        /// <inheritdoc />
        public Task PointerPressedExecuteAsync()
        {
            UndoStack.Push(ShapeList.DeepCopy());
            RedoStack.Clear();

            _prevPointer = PointerEventArgs.GetCurrentPoint(Canvas).Position;
            _selected = ShapeList.Where(bs => bs.Selected).ToList();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task PointerReleasedExecuteAsync()
        {
            _selected = null;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task PointerMovedExecuteAsync()
        {
            if (PointerEventArgs.Pointer.IsInContact && _selected.Count > 0)
            {
                Point currentPoint = PointerEventArgs.GetCurrentPoint(Canvas).Position;
                // Calculate mouse movement deltas
                Point difference = new Point(currentPoint.X - _prevPointer.X, currentPoint.Y - _prevPointer.Y);
                _prevPointer = currentPoint;

                foreach (PaintBase paintBase in _selected)
                {
                    // Use visitor pattern to apply scale to the elements
                    paintBase.Accept(new ScaleVisitor(difference.X, difference.Y));
                }

                _page.Draw();
            }

            return Task.CompletedTask;
        }
    }
}
