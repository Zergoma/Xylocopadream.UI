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
/// item up or down. The items source must be a list; an <c>ObservableCollection</c> is moved in place.</item>
/// </list>
/// The dragged item's container gets the <c>xd-dragging</c> class.
/// </summary>
public static class Reorder
{
    public static readonly AttachedProperty<bool> AnimateProperty =
        AvaloniaProperty.RegisterAttached<ItemsControl, bool>("Animate", typeof(Reorder));

    public static readonly AttachedProperty<bool> IsHandleProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsHandle", typeof(Reorder));

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

    private sealed class Drag(ItemsControl itemsControl, object item, Control container)
    {
        private Control _container = container;

        public void Start(PointerPressedEventArgs e)
        {
            itemsControl.AddHandler(InputElement.PointerMovedEvent, OnMoved);
            itemsControl.AddHandler(InputElement.PointerReleasedEvent, OnReleased);
            itemsControl.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost);
            itemsControl.LayoutUpdated += OnLayoutUpdated;
            _container.Classes.Add("xd-dragging");
            e.Pointer.Capture(itemsControl);
        }

        private void OnMoved(object? sender, PointerEventArgs e)
        {
            if (itemsControl.ItemsSource is not IList items || itemsControl.ItemsPanelRoot is not { } panel)
            {
                return;
            }

            var y = e.GetPosition(panel).Y;
            var target = itemsControl.GetRealizedContainers()
                .Where(c => c.IsVisible)
                .FirstOrDefault(c => y >= c.Bounds.Top && y < c.Bounds.Bottom);
            if (target is null || target == _container)
            {
                return;
            }

            var from = items.IndexOf(item);
            var to = itemsControl.IndexFromContainer(target);
            if (from >= 0 && to >= 0)
            {
                Move(items, from, to);
            }
        }

        private void OnReleased(object? sender, PointerReleasedEventArgs e) => e.Pointer.Capture(null);

        private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            itemsControl.RemoveHandler(InputElement.PointerMovedEvent, OnMoved);
            itemsControl.RemoveHandler(InputElement.PointerReleasedEvent, OnReleased);
            itemsControl.RemoveHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost);
            itemsControl.LayoutUpdated -= OnLayoutUpdated;
            _container.Classes.Remove("xd-dragging");
        }

        /// <summary>A list without Move recreates the container: keep the class on the current one.</summary>
        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            if (itemsControl.ContainerFromItem(item) is { } current && current != _container)
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
