using System.Globalization;
using System.IO.Ports;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using OpenAC.Net.Devices;

namespace OpenAC.Net.Balanca.Demo;

public partial class MainWindow : Window
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenAC.Net.Balanca", "demo.json");
    private OpenBal? balanca;
    private Task? operacao;
    private bool podeFechar;
    private bool fechando;
    private bool inicializado;

    public MainWindow()
    {
        InitializeComponent();
        ProtocolCombo.ItemsSource = Enum.GetValues<ProtocoloBalanca>();
        TransportCombo.ItemsSource = new[] { "Serial", "TCP" };
        ParityCombo.ItemsSource = Enum.GetValues<Parity>();
        StopCombo.ItemsSource = new[] { StopBits.One, StopBits.Two, StopBits.OnePointFive };
        HandshakeCombo.ItemsSource = Enum.GetValues<Handshake>();
        ApplyConfiguration(new DemoConfig());
        try
        {
            if (File.Exists(ConfigPath))
                ApplyConfiguration(JsonSerializer.Deserialize<DemoConfig>(File.ReadAllText(ConfigPath)) ?? new DemoConfig());
        }
        catch (Exception ex) { MessageText.Text = $"Não foi possível carregar a configuração: {ex.Message}"; }
        inicializado = true;
        Closing += OnClosing;
    }

    private void ApplyConfiguration(DemoConfig config)
    {
        ProtocolCombo.SelectedItem = config.Protocolo;
        TransportCombo.SelectedIndex = config.Tcp ? 1 : 0;
        PortText.Text = config.Porta;
        BaudInput.Value = config.Baud;
        BitsInput.Value = config.DataBits;
        ParityCombo.SelectedItem = config.Parity;
        StopCombo.SelectedItem = config.StopBits;
        HandshakeCombo.SelectedItem = config.Handshake;
        HostText.Text = config.Host;
        TcpPortInput.Value = config.TcpPort;
        TimeoutInput.Value = config.Timeout;
        IntervalInput.Value = config.Intervalo;
        PositionInput.Value = config.Posicao;
        LengthInput.Value = config.Tamanho;
    }

    private DemoConfig GetConfiguration() => new()
    {
        Protocolo = (ProtocoloBalanca)(ProtocolCombo.SelectedItem ?? throw new InvalidOperationException("Selecione um protocolo.")),
        Tcp = TransportCombo.SelectedIndex == 1,
        Porta = PortText.Text?.Trim() ?? "",
        Baud = (int)(BaudInput.Value ?? 9600),
        DataBits = (int)(BitsInput.Value ?? 8),
        Parity = (Parity)(ParityCombo.SelectedItem ?? Parity.None),
        StopBits = (StopBits)(StopCombo.SelectedItem ?? StopBits.One),
        Handshake = (Handshake)(HandshakeCombo.SelectedItem ?? Handshake.None),
        Host = HostText.Text?.Trim() ?? "",
        TcpPort = (int)(TcpPortInput.Value ?? 9000),
        Timeout = (int)(TimeoutInput.Value ?? 3000),
        Intervalo = (int)(IntervalInput.Value ?? 200),
        Posicao = (int)(PositionInput.Value ?? 0),
        Tamanho = (int)(LengthInput.Value ?? 0)
    };

    private static OpenBal CreateScale(DemoConfig config)
    {
        OpenBal scale = config.Tcp
            ? new OpenBal<TCPConfig>(new TCPConfig(config.Host, config.TcpPort))
            : new OpenBal<SerialConfig>(new SerialConfig
            {
                Porta = config.Porta, Baud = config.Baud, DataBits = config.DataBits,
                Parity = config.Parity, StopBits = config.StopBits, Handshake = config.Handshake
            });
        scale.Protocolo = config.Protocolo;
        scale.PosicaoInicial = config.Posicao;
        scale.TamanhoResposta = config.Tamanho;
        scale.DelayMonitoramento = config.Intervalo;
        scale.Device.TimeOut = config.Timeout;
        return scale;
    }

    private async void ToggleConnection(object? sender, RoutedEventArgs e)
    {
        if (operacao != null || fechando) return;
        await RunOperation(async () =>
        {
            if (balanca != null)
            {
                var atual = balanca;
                balanca = null;
                atual.AoLerPeso -= OnWeight;
                await Task.Run(atual.Dispose);
                MessageText.Text = "Balança desconectada.";
                return;
            }
            var nova = CreateScale(GetConfiguration());
            try
            {
                await Task.Run(nova.Conectar);
                balanca = nova;
                nova.AoLerPeso += OnWeight;
                MessageText.Text = $"Conectada usando {nova.Protocolo}.";
            }
            catch { nova.Dispose(); throw; }
        });
    }

    private async void ReadWeight(object? sender, RoutedEventArgs e)
    {
        if (balanca == null || operacao != null || fechando) return;
        var atual = balanca;
        await RunOperation(() => Task.Run(() => atual.LerPeso()));
    }

    private async Task RunOperation(Func<Task> action)
    {
        ConfigurationPanel.IsEnabled = ConnectButton.IsEnabled = ReadButton.IsEnabled = MonitorCheck.IsEnabled = false;
        MessageText.Text = "Aguarde…";
        try
        {
            // Yield garante que operacao seja atribuída antes de a ação poder concluir.
            operacao = ExecuteAsync(action);
            await operacao;
        }
        catch (Exception ex) { MessageText.Text = ex.Message; AppendLog($"ERRO: {ex.Message}"); }
        finally { operacao = null; RefreshConnection(); }
    }

    private static async Task ExecuteAsync(Func<Task> action)
    {
        await Task.Yield();
        await action();
    }

    private void RefreshConnection()
    {
        var conectado = balanca?.Conectado == true;
        ConfigurationPanel.IsEnabled = !conectado && !fechando;
        SaveButton.IsEnabled = !fechando;
        ConnectButton.IsEnabled = !fechando;
        ConnectButton.Content = balanca != null ? "Desconectar" : "Conectar";
        ReadButton.IsEnabled = MonitorCheck.IsEnabled = conectado && !fechando;
        ConnectionStatus.Text = conectado ? $"Conectada • {balanca!.Protocolo}" : "Desconectada";
        if (!conectado) MonitorCheck.IsChecked = false;
    }

    private void MonitorChanged(object? sender, RoutedEventArgs e)
    {
        if (balanca != null) balanca.IsMonitorar = MonitorCheck.IsChecked == true;
    }

    private void TransportChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Durante InitializeComponent os controles seguintes ainda não foram criados.
        if (SerialPanel == null || TcpPanel == null || SerialOptions == null) return;
        var tcp = TransportCombo.SelectedIndex == 1;
        SerialPanel.IsVisible = SerialOptions.IsVisible = !tcp;
        TcpPanel.IsVisible = tcp;
    }

    private void DetectPorts(object? sender, RoutedEventArgs e)
    {
        try
        {
            var portas = SerialPort.GetPortNames().Order().ToArray();
            if (portas.Length > 0) PortText.Text = portas[0];
            MessageText.Text = portas.Length == 0 ? "Nenhuma porta serial encontrada. Você pode informar a porta manualmente."
                : $"Portas encontradas: {string.Join(", ", portas)}";
        }
        catch (Exception ex) { MessageText.Text = ex.Message; }
    }

    private void SaveConfiguration(object? sender, RoutedEventArgs e)
    {
        try
        {
            var config = GetConfiguration();
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
            MessageText.Text = $"Configuração salva em {ConfigPath}";
        }
        catch (Exception ex) { MessageText.Text = ex.Message; }
    }

    private void Simulate(object? sender, RoutedEventArgs e)
    {
        try
        {
            using var simulador = CreateScale(GetConfiguration());
            var resposta = Regex.Unescape(SimulationText.Text ?? "");
            ShowWeight(new BalancaEventArgs(resposta, simulador.InterpretarRespostaPeso(resposta)), true);
        }
        catch (Exception ex) { MessageText.Text = ex.Message; }
    }

    private void OnWeight(object? sender, BalancaEventArgs e) => Dispatcher.UIThread.Post(() =>
    {
        if (!fechando && ReferenceEquals(sender, balanca)) ShowWeight(e, false);
    });

    private void ShowWeight(BalancaEventArgs leitura, bool simulacao)
    {
        var status = leitura.Excecao?.Message ?? leitura.Peso switch
        {
            -1 => "Peso instável", -2 => "Peso negativo", -9 => "Falha de leitura / timeout",
            -10 => "Sobrecarga", -11 => "Captura inicial de zero", -12 => "Erro de calibração", _ => "Peso recebido"
        };
        WeightText.Text = leitura.Excecao == null && leitura.Peso >= 0
            ? $"{leitura.Peso:N3} kg" : "— kg";
        WeightStatus.Text = status;
        MessageText.Text = (simulacao ? "Simulação • " : "") + status;
        AppendLog($"{(simulacao ? "SIM" : "RX")} {leitura.Peso?.ToString(CultureInfo.InvariantCulture) ?? "erro"} • {status}\n    {Escape(leitura.Leitura)}");
    }

    private static string Escape(string texto) => string.Concat(texto.Select(c =>
        char.IsControl(c) ? $"\\x{(int)c:X2}" : c.ToString()));

    private void AppendLog(string texto)
    {
        var historico = LogText.Text ?? "";
        if (historico.Length > 60000) historico = historico[^40000..];
        LogText.Text = historico + $"{DateTime.Now:HH:mm:ss.fff} {texto}{Environment.NewLine}";
        LogText.CaretIndex = LogText.Text.Length;
    }

    private void ClearLog(object? sender, RoutedEventArgs e) => LogText.Text = "";

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!inicializado || podeFechar) return;
        if (operacao == null && balanca == null) { podeFechar = true; return; }
        e.Cancel = true;
        if (fechando) return;
        fechando = true;
        try
        {
            if (operacao != null) await operacao;
        }
        catch (Exception ex) { AppendLog($"ERRO: {ex.Message}"); }
        var atual = balanca;
        balanca = null;
        if (atual != null)
        {
            atual.AoLerPeso -= OnWeight;
            try { await Task.Run(atual.Dispose); }
            catch (Exception ex) { AppendLog($"ERRO: {ex.Message}"); }
        }
        podeFechar = true;
        Close();
    }
}
