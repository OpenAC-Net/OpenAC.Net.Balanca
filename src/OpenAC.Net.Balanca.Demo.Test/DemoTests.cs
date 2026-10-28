using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using OpenAC.Net.Balanca.Demo;

namespace OpenAC.Net.Balanca.Demo.Test;

public static class TestApp
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
        .UseSkia().WithInterFont();
}

public class DemoTests
{
    [Test]
    public async Task AbreJanelaSimulaPesoEAlternaTransporte()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await using var session = HeadlessUnitTestSession.StartNew(typeof(TestApp));
        var resultados = await session.Dispatch(() =>
        {
            var resultados = new List<(string? Atual, string Esperado)>();
            var window = new MainWindow();
            window.Show();
            try
            {
                var protocolos = window.FindControl<ComboBox>("ProtocolCombo")!;
                protocolos.SelectedItem = ProtocoloBalanca.Toledo;
                var transporte = window.FindControl<ComboBox>("TransportCombo")!;
                transporte.SelectedIndex = 1;
                resultados.Add((window.FindControl<Grid>("TcpPanel")!.IsVisible.ToString(), "True"));
                resultados.Add((window.FindControl<Grid>("SerialPanel")!.IsVisible.ToString(), "False"));
                transporte.SelectedIndex = 0;
                resultados.Add((window.FindControl<Grid>("SerialPanel")!.IsVisible.ToString(), "True"));
                resultados.Add((window.FindControl<Button>("ReadButton")!.IsEnabled.ToString(), "False"));
                resultados.Add((protocolos.ItemCount.ToString(), "39"));

                var tabs = window.FindControl<TabControl>("MainTabs")!;
                tabs.SelectedIndex = 1;
                window.UpdateLayout();
                window.FindControl<TextBox>("SimulationText")!.Text = @"\x0201234\x03";
                var simular = window.FindControl<Button>("SimulateButton")!;
                simular.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                resultados.Add((window.FindControl<TextBlock>("WeightStatus")!.Text, "Peso recebido"));
                resultados.Add((window.FindControl<TextBlock>("WeightText")!.Text?.Contains("kg").ToString(), "True"));
                resultados.Add((window.FindControl<TextBox>("LogText")!.Text?.Contains(@"\x02").ToString(), "True"));
                window.FindControl<TextBox>("SimulationText")!.Text = @"\x02IIIII\x03";
                simular.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                resultados.Add((window.FindControl<TextBlock>("WeightStatus")!.Text, "Peso instável"));
                resultados.Add((window.FindControl<TextBlock>("WeightText")!.Text, "— kg"));
                tabs.SelectedIndex = 0;
                window.UpdateLayout();
                window.CaptureRenderedFrame()?.Save(Path.Combine(Path.GetTempPath(), "openac-balanca-demo.png"), PngBitmapEncoderOptions.Default);
                return resultados;
            }
            finally { window.Close(); }
        }, timeout.Token);
        foreach (var resultado in resultados)
            await Assert.That(resultado.Atual).IsEqualTo(resultado.Esperado);
    }
}
