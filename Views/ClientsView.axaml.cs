using Apteka.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace Apteka.Views;

public partial class ClientsView : UserControl
{
	public ClientsView()
	{
		InitializeComponent();
	}

	private void OnAddressDoubleTapped(object? sender, TappedEventArgs e)
	{
		if (DataContext is ClientsViewModel vm) vm.EditAddress();
	}

	private void OnPhoneDoubleTapped(object? sender, TappedEventArgs e)
	{
		if (DataContext is ClientsViewModel vm) vm.EditPhoneNumbers();
	}
}