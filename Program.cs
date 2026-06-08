using System;
using System.Globalization;
using System.Threading;
using Avalonia;

namespace Apteka;

internal sealed class Program
{
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args)
	{
		var culutre = new CultureInfo("pl-PL");
		CultureInfo.DefaultThreadCurrentCulture = culutre;
		CultureInfo.DefaultThreadCurrentUICulture = culutre;
		Thread.CurrentThread.CurrentCulture = culutre;
		Thread.CurrentThread.CurrentUICulture = culutre;
		BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);
	}

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace();
	}
}