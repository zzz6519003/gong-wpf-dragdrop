using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Reflection;
using System.Collections;
using System.Windows.Controls.Primitives;

namespace GongSolutions.Wpf.DragDrop.Utilities
{
    public static class ItemsControlExtensions
    {
        public static bool CanSelectMultipleItems(this ItemsControl itemsControl)
        {
            if (itemsControl is MultiSelector)
            {
                // The CanSelectMultipleItems property is protected. Use reflection to
                // get its value anyway.
                return (bool)itemsControl.GetType()
                    .GetProperty("CanSelectMultipleItems", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(itemsControl, null);
            }
            else if (itemsControl is ListBox)
            {
                return ((ListBox)itemsControl).SelectionMode != SelectionMode.Single;
            }
            else
            {
                return false;
            }
        }

        public static UIElement GetItemContainer(this ItemsControl itemsControl, UIElement child)
        {
            Type itemType = GetItemContainerType(itemsControl);

            if (itemType != null)
            {
                return (UIElement)child.GetVisualAncestor(itemType);
            }

            return null;
        }

        public static UIElement GetItemContainerAt(this ItemsControl itemsControl, Point position)
        {
            IInputElement inputElement = itemsControl.InputHitTest(position);
            UIElement uiElement = inputElement as UIElement;

            if (uiElement != null)
            {
                return GetItemContainer(itemsControl, uiElement);
            }

            return null;
        }

        public static UIElement GetItemContainerAt(this ItemsControl itemsControl, Point position,
                                                   Orientation searchDirection)
        {
            Type itemContainerType = GetItemContainerType(itemsControl);

            if (itemContainerType != null)
            {
                Geometry hitTestGeometry;
                
                if (typeof(TreeViewItem).IsAssignableFrom(itemContainerType))
                {
                    //Console.WriteLine("Is TreeViewItem");
                    hitTestGeometry = new LineGeometry(new Point(0, position.Y), new Point(itemsControl.RenderSize.Width, position.Y));
                    //hitTestGeometry = new RectangleGeometry(new Rect(new Point(0, 0),
                    //                                                 new Point(itemsControl.RenderSize.Width, itemsControl.RenderSize.Height)));

                }
                else
                {
                    switch (searchDirection)
                    {
                        case Orientation.Horizontal:
                            hitTestGeometry = new LineGeometry(new Point(0, position.Y), new Point(itemsControl.RenderSize.Width, position.Y));
                            break;
                        case Orientation.Vertical:
                            hitTestGeometry = new LineGeometry(new Point(position.X, 0), new Point(position.X, itemsControl.RenderSize.Height));
                            break;
                        default:
                            throw new ArgumentException("Invalid value for searchDirection");
                    }
                }

                List<DependencyObject> hits = new List<DependencyObject>();

                //Console.WriteLine("Performing hit test - {0}", itemsControl.ToString());
                VisualTreeHelper.HitTest(itemsControl, null,
                    result =>
                    {
                        DependencyObject itemContainer = result.VisualHit.GetVisualAncestor(itemContainerType);
                        if (itemContainer != null && !hits.Contains(itemContainer) && ((UIElement)itemContainer).IsVisible == true)
                            hits.Add(itemContainer);
                        return HitTestResultBehavior.Continue;
                    },
                    new GeometryHitTestParameters(hitTestGeometry));

                //Console.WriteLine("Hits = {0}", hits.Count);

                return GetClosest(itemsControl, hits, position, searchDirection);
            }

            return null;
        }

        public static Type GetItemContainerType(this ItemsControl itemsControl)
        {
            // There is no safe way to get the item container type for an ItemsControl. 
            // First hard-code the types for the common ItemsControls.
            if (itemsControl.GetType().IsAssignableFrom(typeof(ListBox)))
            {
                return typeof(ListBoxItem);
            }
            else if (itemsControl.GetType().IsAssignableFrom(typeof(TreeView)))
            {
                return typeof(TreeViewItem);
            }
            else if (itemsControl.GetType().IsAssignableFrom(typeof(ListView)))
            {
                return typeof(ListViewItem);
            }

            // Otherwise look for the control's ItemsPresenter, get it's child panel and the first 
            // child of that *should* be an item container.
            //
            // If the control currently has no items, we're out of luck.
            if (itemsControl.Items.Count > 0)
            {
                IEnumerable<ItemsPresenter> itemsPresenters = itemsControl.GetVisualDescendents<ItemsPresenter>();

                foreach (ItemsPresenter itemsPresenter in itemsPresenters)
                {
                    DependencyObject panel = VisualTreeHelper.GetChild(itemsPresenter, 0);
                    DependencyObject itemContainer = VisualTreeHelper.GetChild(panel, 0);

                    // Ensure that this actually *is* an item container by checking it with
                    // ItemContainerGenerator.
                    if (itemContainer != null &&
                        itemsControl.ItemContainerGenerator.IndexFromContainer(itemContainer) != -1)
                    {
                        return itemContainer.GetType();
                    }
                }
            }

            return null;
        }

        public static Orientation GetItemsPanelOrientation(this ItemsControl itemsControl)
        {
            ItemsPresenter itemsPresenter = itemsControl.GetVisualDescendent<ItemsPresenter>();
            if (itemsPresenter != null)
            {
                DependencyObject itemsPanel = VisualTreeHelper.GetChild(itemsPresenter, 0);
                PropertyInfo orientationProperty = itemsPanel.GetType().GetProperty("Orientation", typeof(Orientation));

                if (orientationProperty != null)
                {
                    return (Orientation)orientationProperty.GetValue(itemsPanel, null);
                }
            }

            // Make a guess!
            return Orientation.Vertical;
        }

        public static IEnumerable GetSelectedItems(this ItemsControl itemsControl)
        {
            if (itemsControl.GetType().IsAssignableFrom(typeof(MultiSelector)))
            {
                return ((MultiSelector)itemsControl).SelectedItems;
            }
            else if (itemsControl.GetType().IsAssignableFrom(typeof(ListBox)))
            {
                ListBox listBox = (ListBox)itemsControl;

                if (listBox.SelectionMode == SelectionMode.Single)
                {
                    return Enumerable.Repeat(listBox.SelectedItem, 1);
                }
                else
                {
                    return listBox.SelectedItems;
                }
            }
            else if (itemsControl.GetType().IsAssignableFrom(typeof(TreeView)))
            {
                return Enumerable.Repeat(((TreeView)itemsControl).SelectedItem, 1);
            }
            else if (itemsControl.GetType().IsAssignableFrom(typeof(Selector)))
            {
                return Enumerable.Repeat(((Selector)itemsControl).SelectedItem, 1);
            }
            else
            {
                return Enumerable.Empty<object>();
            }
        }

        static UIElement GetClosest(ItemsControl itemsControl, List<DependencyObject> items,
                                    Point position, Orientation searchDirection)
        {
            //Console.WriteLine("GetClosest - {0}", itemsControl.ToString());

            UIElement closest = null;
            double closestDistance = double.MaxValue;

            foreach (DependencyObject i in items)
            {
                UIElement uiElement = i as UIElement;

                if (uiElement != null)
                {
                    Point p = uiElement.TransformToAncestor(itemsControl).Transform(new Point(0, 0));
                    double distance = double.MaxValue;
                    
                    if (itemsControl is TreeView)
                    {
                        double xDiff = position.X - p.X;
                        double yDiff = position.Y - p.Y;
                        double hyp = Math.Sqrt(Math.Pow(xDiff, 2d) + Math.Pow(yDiff, 2d));
                        distance = Math.Abs(hyp);
                    }
                    else
                    {
                        switch (searchDirection)
                        {
                            case Orientation.Horizontal:
                                distance = Math.Abs(position.X - p.X);
                                break;
                            case Orientation.Vertical:
                                distance = Math.Abs(position.Y - p.Y);
                                break;
                        }
                    }

                    if (distance < closestDistance)
                    {
                        closest = uiElement;
                        closestDistance = distance;
                    }
                }
            }
            
            return closest;
        }
    }
}
