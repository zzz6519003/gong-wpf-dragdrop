# Drop Handler Example #

A drop handler allows you to specify logic to control a drop. This responsibility will usually be delegated to your ViewModel by setting the DropHandler attached property:

```
<ListBox Grid.Column="1" Grid.Row="1" ItemsSource="{Binding Schools}" 
         dd:DragDrop.IsDragSource="True" dd:DragDrop.IsDropTarget="True"
         dd:DragDrop.DropHandler="{Binding}"/>
```

Here we're binding the drop handler to the current DataContext, which will usually be your ViewModel.

You handle the drop in your ViewModel by implementing the IDropTarget interface:

```
class ExampleViewModel : IDropTarget
{
    public ObservableCollection<SchoolViewModel> Schools { get; private set; }

    void IDropTarget.DragOver(DropInfo dropInfo)
    {
        PupilViewModel sourceItem = dropInfo.Data as PupilViewModel;
        SchoolViewModel targetItem = dropInfo.TargetItem as SchoolViewModel;

        if (sourceItem != null && targetItem != null && targetItem.CanAcceptPupils)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            dropInfo.Effects = DragDropEffects.Copy;
        }
    }

    void IDropTarget.Drop(DropInfo dropInfo)
    {
        PupilViewModel sourceItem = (PupilViewModel)dropInfo.Data;
        SchoolViewModel targetItem = (SchoolViewModel)dropInfo.TargetItem;
        targetItem.Pupils.Add(sourceItem);
    }
}

class SchoolViewModel
{
    public bool CanAcceptPupils { get; set; }
    public ObservableCollection<PupilViewModel> Pupils { get; private set; }
}

class PupilViewModel
{
    public string Name { get; set; }
}
```

The IDropTarget interface defines two methods:

  * DragOver is called repeatedly whilst the drag is over the control. It allows you to give visual feedback to the user indicating what the action of the drop will be.
  * Drop is called when the user drops the data on the control.

Both of these methods accept a single DropInfo parameter which contains information related to the drag/drop, and in the case of DragOver is used to return the current state  of the drag to the framework.

## DragOver ##

Let's take a look at those two methods in detail. First, DragOver:

```
void IDropTarget.DragOver(DropInfo dropInfo)
{
    PupilViewModel sourceItem = dropInfo.Data as PupilViewModel;
    SchoolViewModel targetItem = dropInfo.TargetItem as SchoolViewModel;
```

The first thing the DragOver method does is check that it can handle the data types involved in the drag.

The dropInfo.Data property contains the data being dragged. If the control that was the source of the drag is a bound control, this will be the object that the dragged item was bound to. Here we're trying to cast it to an PupilViewModel.

The dropInfo.TargetItem property contains the object that the item currently under the mouse cursor is bound to. Again, we're trying to cast it to an SchoolViewModel.

```
if (sourceItem != null && targetItem != null && targetItem.CanAcceptPupils)
```

Next, we check that the source and target items are of the accepted types, and check that the target school is currently accepting pupils.

```
dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
```

Here, we set the drop target adorner. Because dropping a Pupil into a School causes the pupil to be added to the school, we chosse the Highlight adorner which highlights the target item. The other available drop target adorner is the Insert adorner, which draws a caret at the point in the list the dropped item will be inserted.

```
dropInfo.Effects = DragDropEffects.Copy;
```

Here we set the drag effect to Copy. This tells the framework that the dragged data can be dropped here and to display a Copy mouse pointer.

If we don't set this property, the default value of DragDropEffects.None is used, which indicates that a drop is not allowed at this position.

## Drop ##

```
void IDropTarget.Drop(DropInfo dropInfo)
{
    PupilViewModel sourceItem = (PupilViewModel)dropInfo.Data;
    SchoolViewModel targetItem = (SchoolViewModel)dropInfo.TargetItem;
```

Here we cast the source and target item to their real types. We can be sure at this point that the source and target items will be of this type because the DragOver method only returns a valid drag if this is the case.

```
    targetItem.Pupils.Add(sourceItem);
}
```

Finally we add the pupil to the school.