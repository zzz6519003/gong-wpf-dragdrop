using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace GongSolutions.Wpf.DragDrop
{
    public class DropTargetHighlightAdorner : DropTargetAdorner
    {
        public DropTargetHighlightAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (DropInfo.VisualTargetItem != null)
            {
                Rect rect;
                TreeViewItem tvItem = DropInfo.VisualTargetItem as TreeViewItem;
                if (tvItem != null)
                {
                    var grid = (Grid)((UIElement)VisualTreeHelper.GetChild(DropInfo.VisualTargetItem, 0));
                    var descendant = VisualTreeHelper.GetDescendantBounds(DropInfo.VisualTargetItem);

                    rect = new Rect(DropInfo.VisualTargetItem.TranslatePoint(new Point(), AdornedElement),
                        new Size(descendant.Width + 4, grid.RowDefinitions[0].ActualHeight));
                }
                else
                {
                    rect = new Rect(DropInfo.VisualTargetItem.TranslatePoint(new Point(), AdornedElement),
                    VisualTreeHelper.GetDescendantBounds(DropInfo.VisualTargetItem).Size);
                }

                drawingContext.DrawRoundedRectangle(null, new Pen(Brushes.Gray, 2), rect, 2, 2);
            }
        }
    }
}
