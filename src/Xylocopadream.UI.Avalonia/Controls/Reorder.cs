using System.Collections;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Xylocopadream.UI.Avalonia.Controls;

/// <summary>
/// Reordering of an <see cref="ItemsControl"/>'s items:
/// <list type="bullet">
/// <item><c>xd:Reorder.Animate="True"</c> on the items control makes items slide to their new place when the
/// collection changes (move, insert, remove), instead of jumping;</item>
/// <item><c>xd:Reorder.IsHandle="True"</c> on a control of the item template (a grip icon) lets the user drag the
/// item up or down. The items source must be a list; an <c>ObservableCollection</c> is moved in place;</item>
/// <item><c>xd:Reorder.Group="name"</c> on several items controls lets an item be dragged from one to another
/// (e.g. between the sections of a form). Their sources must accept each other's items.</item>
/// </list>
/// The dragged item's container gets the <c>xd-dragging</c> class.
/// </summary>
public static class Reorder
{
    public static readonly AttachedProperty<bool> AnimateProperty =
        AvaloniaProperty.RegisterAttached<ItemsControl, bool>("Animate", typeof(Reorder));

    public static readonly AttachedProperty<bool> IsHandleProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsHandle", typeof(Reorder));

    public static readonly AttachedProperty<string?> GroupProperty =
        AvaloniaProperty.RegisterAttached<ItemsControl, string?>("Group", typeof(Reorder));

    /// <summary>Duration of the slide animation.</summary>
    public static TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(180);

    private static readonly ConditionalWeakTable<ItemsControl, Animator> Animators = new();

    static Reorder()
    {
        AnimateProperty.Changed.AddClassHandler<ItemsControl>((control, e) =>
        {
            if (e.NewValue is true)
            {
                Animators.GetValue(control, c => new Animator(c));
            }
            else if (Animators.TryGetValue(control, out var animator))
            {
                animator.Detach();
                Animators.Remove(control);
            }
        });

        IsHandleProperty.Changed.AddClassHandler<Control>((control, e) =>
        {
            if (e.NewValue is true)
            {
                control.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
                control.AddHandler(InputElement.PointerPressedEvent, OnHandlePressed);
            }
            else
            {
                control.RemoveHandler(InputElement.PointerPressedEvent, OnHandlePressed);
            }
        });
    }

    public static bool GetAnimate(ItemsControl control) => control.GetValue(AnimateProperty);

    public static void SetAnimate(ItemsControl control, bool value) => control.SetValue(AnimateProperty, value);

    public static bool GetIsHandle(Control control) => control.GetValue(IsHandleProperty);

    public static void SetIsHandle(Control control, bool value) => control.SetValue(IsHandleProperty, value);

    public static string? GetGroup(ItemsControl control) => control.GetValue(GroupProperty);

    public static void SetGroup(ItemsControl control, string? value) => control.SetValue(GroupProperty, value);

    /// <summary>Moves an item of <paramref name="items"/>: in place when the list has a <c>Move(int, int)</c>
    /// method (ObservableCollection), otherwise by removing and inserting it.</summary>
    public static void Move(IList items, int from, int to)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (from == to)
        {
            return;
        }

        var move = items.GetType().GetMethod("Move", [typeof(int), typeof(int)]);
        if (move is not null)
        {
            move.Invoke(items, [from, to]);
            return;
        }

        var item = items[from];
        items.RemoveAt(from);
        items.Insert(to, item);
    }

    // ------------------------------------------------------------------ drag

    private static void OnHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control handle
            || !e.GetCurrentPoint(handle).Properties.IsLeftButtonPressed
            || handle.FindAncestorOfType<ItemsControl>() is not { ItemsSource: IList } itemsControl
            || ContainerOf(itemsControl, handle) is not { } container
            || itemsControl.ItemFromContainer(container) is not { } item)
        {
            return;
        }

        new Drag(itemsControl, item, container).Start(e);
        e.Handled = true;
    }

    /// <summary>The item container holding <paramref name="element"/>: the ancestor whose parent is the items panel.</summary>
    private static Control? ContainerOf(ItemsControl itemsControl, Visual element)
    {
        for (var visual = element; visual is not null; visual = visual.GetVisualParent())
        {
            if (visual.GetVisualParent() == itemsControl.ItemsPanelRoot)
            {
                return visual as Control;
            }
        }

        return null;
    }

    private sealed class Drag(ItemsControl source, object item, Control container)
    {
        private readonly ItemsControl _source = source;
        private ItemsControl _owner = source;
        private Control _container = container;

        public void Start(PointerPressedEventArgs e)
        {
            // The source keeps the capture even when the item moves to another list.
            _source.AddHandler(InputElement.PointerMovedEvent, OnMoved);
            _source.AddHandler(InputElement.PointerReleasedEvent, OnReleased);
            _source.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost);
            _source.LayoutUpdated += OnLayoutUpdated;
            _container.Classes.Add("xd-dragging");
            e.Pointer.Capture(_source);
        }

        private void OnMoved(object? sender, PointerEventArgs e)
        {
            if (TopLevel.GetTopLevel(_source) is not { } root
                || root.InputHitTest(e.GetPosition(root)) is not Visual hit
                || FindTarget(hit) is not var (target, index)
                || _owner.ItemsSource is not IList from
                || target.ItemsSource is not IList to)
            {
                return;
            }

            var current = from.IndexOf(item);
            if (current < 0)
            {
                return;
            }

            if (target == _owner)
            {
                // Past the middle of the item only: once swapped, items of different heights would otherwise leave the
                // pointer over the other one, and swap them back and forth.
                // In the panel, with the layout bounds: a sliding item is drawn elsewhere (render transform) for a while.
                if (target.ContainerFromIndex(index) is { } over && target.ItemsPanelRoot is { } panel)
                {
                    var y = e.GetPosition(panel).Y;
                    var middle = over.Bounds.Top + (over.Bounds.Height / 2);
                    if ((index > current && y < middle) || (index < current && y > middle))
                    {
                        return;
                    }
                }

                Move(from, current, Math.Min(index, from.Count - 1));
                return;
            }

            try
            {
                from.RemoveAt(current);
                to.Insert(Math.Min(index, to.Count), item);
                _owner = target;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidCastException or NotSupportedException)
            {
                // The other list does not take this kind of item: put it back.
                if (!from.Contains(item))
                {
                    from.Insert(current, item);
                }
            }
        }

        /// <summary>Where the pointer is: an item container of the current list or of a list of the same
        /// <see cref="GroupProperty"/> (index of that item), or the empty part of such a list (its end).</summary>
        private (ItemsControl Target, int Index)? FindTarget(Visual hit)
        {
            var group = GetGroup(_owner);
            for (var visual = hit; visual is not null; visual = visual.GetVisualParent())
            {
                if (visual == _container)
                {
                    return null;
                }

                if (visual is Control control
                    && ItemsControl.ItemsControlFromItemContainer(control) is { } list
                    && Accepts(list, group))
                {
                    return (list, list.IndexFromContainer(control));
                }

                if (visual is ItemsControl other && Accepts(other, group))
                {
                    // Between rows: nothing to do; an empty list of the group: its end.
                    return other != _owner && !other.GetRealizedContainers().Any(c => c.IsVisible) ? (other, int.MaxValue) : null;
                }
            }

            return null;
        }

        private bool Accepts(ItemsControl list, string? group) =>
            list == _owner || (group is not null && GetGroup(list) == group);

        private void OnReleased(object? sender, PointerReleasedEventArgs e) => e.Pointer.Capture(null);

        private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            _source.RemoveHandler(InputElement.PointerMovedEvent, OnMoved);
            _source.RemoveHandler(InputElement.PointerReleasedEvent, OnReleased);
            _source.RemoveHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost);
            _source.LayoutUpdated -= OnLayoutUpdated;
            _container.Classes.Remove("xd-dragging");
        }

        /// <summary>Moving to another list (or a list without Move) recreates the container: keep the class on it.</summary>
        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            if (_owner.ContainerFromItem(item) is { } current && current != _container)
            {
                _container.Classes.Remove("xd-dragging");
                _container = current;
                _container.Classes.Add("xd-dragging");
            }
        }
    }

    // ------------------------------------------------------------- animation

    /// <summary>"FLIP" animation: remembers where each item was laid out; after a collection change, an item laid
    /// out elsewhere starts drawn at its old place and slides to the new one.</summary>
    private sealed class Animator
    {
        private readonly ItemsControl _itemsControl;
        private INotifyCollectionChanged? _source;
        private Dictionary<object, double> _positions = [];
        private bool _changed;

        public Animator(ItemsControl itemsControl)
        {
            _itemsControl = itemsControl;
            _itemsControl.LayoutUpdated += OnLayoutUpdated;
            _itemsControl.PropertyChanged += OnPropertyChanged;
            Subscribe();
        }

        public void Detach()
        {
            _itemsControl.LayoutUpdated -= OnLayoutUpdated;
            _itemsControl.PropertyChanged -= OnPropertyChanged;
            Unsubscribe();
        }

        private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ItemsControl.ItemsSourceProperty)
            {
                Unsubscribe();
                Subscribe();
                _positions = [];
            }
        }

        private void Subscribe()
        {
            _source = _itemsControl.ItemsSource as INotifyCollectionChanged;
            if (_source is not null)
            {
                _source.CollectionChanged += OnCollectionChanged;
            }
        }

        private void Unsubscribe()
        {
            if (_source is not null)
            {
                _source.CollectionChanged -= OnCollectionChanged;
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => _changed = true;

        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            var positions = new Dictionary<object, double>();
            foreach (var container in _itemsControl.GetRealizedContainers())
            {
                if (!container.IsVisible
                    || _itemsControl.ItemFromContainer(container) is not { } item
                    || !positions.TryAdd(item, container.Bounds.Y))
                {
                    continue;
                }

                if (_changed && _positions.TryGetValue(item, out var previous))
                {
                    // Where it is drawn now (possibly mid-slide) relative to its new layout slot.
                    var drawnAt = previous + (container.RenderTransform as TranslateTransform)?.Y ?? previous;
                    var offset = drawnAt - container.Bounds.Y;
                    if (Math.Abs(offset) > 0.5)
                    {
                        Slide(container, offset);
                    }
                }
            }

            _positions = positions;
            _changed = false;
        }

        private static void Slide(Control container, double offset)
        {
            var transform = new TranslateTransform(0, offset);
            container.RenderTransform = transform;
            transform.Transitions =
            [
                new DoubleTransition { Property = TranslateTransform.YProperty, Duration = AnimationDuration, Easing = new CubicEaseOut() },
            ];
            transform.Y = 0;
        }
    }
}
