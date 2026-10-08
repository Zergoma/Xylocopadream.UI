using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;

namespace Xylocopadream.UI.Avalonia.Controls;

/// <summary>
/// A path as clickable segments separated by chevrons ("Coffre › Photos › 2026"), the last one in bold, as in Rider's
/// navigation bar. Each item shows its <see cref="TextBinding"/> (or its <c>ToString()</c>);
/// a click runs <see cref="Command"/> with the item:
/// <code>&lt;xd:Breadcrumb ItemsSource="{Binding Path}" TextBinding="{Binding Name}" Command="{Binding GoToCommand}" /&gt;</code>
/// </summary>
public class Breadcrumb : ItemsControl
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<Breadcrumb, ICommand?>(nameof(Command));

    public Breadcrumb()
    {
        ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center });
        ItemTemplate = new FuncDataTemplate<object?>((item, _) => Segment());
    }

    /// <summary>Text of a segment, from its item (ItemsControl.DisplayMemberBinding cannot be used: the segments have
    /// their own template).</summary>
    [AssignBinding]
    public BindingBase? TextBinding { get; set; }

    /// <summary>Run with the clicked item.</summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(ItemsControl);

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        Mark(container, index);
    }

    protected override void ContainerIndexChangedOverride(Control container, int oldIndex, int newIndex)
    {
        base.ContainerIndexChangedOverride(container, oldIndex, newIndex);
        Mark(container, newIndex);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemCountProperty)
        {
            // The last segment changes when items are added or removed at the end.
            foreach (var container in GetRealizedContainers())
            {
                Mark(container, IndexFromContainer(container));
            }
        }
    }

    private void Mark(Control container, int index)
    {
        container.Classes.Set("xd-first", index == 0);
        container.Classes.Set("xd-last", index == ItemCount - 1);
    }

    private Control Segment()
    {
        var chevron = new global::Avalonia.Controls.Shapes.Path { Opacity = 0.5 };
        chevron.Classes.Add("icon");
        chevron.Classes.Add("xd-crumb-chevron");
        chevron.Bind(global::Avalonia.Controls.Shapes.Path.DataProperty, chevron.GetResourceObservable("Xd.Icon.ChevronRight"));

        var text = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        if (TextBinding is { } member)
        {
            text.Bind(TextBlock.TextProperty, member);
        }
        else
        {
            text.Bind(TextBlock.TextProperty, new Binding { Converter = ToText.Instance });
        }

        var button = new Button { Content = text };
        button.Classes.Add("tool");
        button.Classes.Add("crumb");
        button.Click += (_, _) =>
        {
            if (Command is { } command && command.CanExecute(button.DataContext))
            {
                command.Execute(button.DataContext);
            }
        };

        return new StackPanel { Orientation = Orientation.Horizontal, Children = { chevron, button } };
    }

    private sealed class ToText : global::Avalonia.Data.Converters.IValueConverter
    {
        public static readonly ToText Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => value?.ToString();

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
