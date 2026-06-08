using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace Apteka.Views.Components;

public partial class DialogHost : UserControl
{
	public static readonly StyledProperty<object?> DialogContentProperty =
		AvaloniaProperty.Register<DialogHost, object?>(nameof(DialogContent));

	public static readonly StyledProperty<ICommand?> CloseCommandProperty =
		AvaloniaProperty.Register<DialogHost, ICommand?>(nameof(CloseCommand));

	public DialogHost()
	{
		InitializeComponent();
	}

	public object? DialogContent
	{
		get => GetValue(DialogContentProperty);
		set => SetValue(DialogContentProperty, value);
	}

	public ICommand? CloseCommand
	{
		get => GetValue(CloseCommandProperty);
		set => SetValue(CloseCommandProperty, value);
	}
}