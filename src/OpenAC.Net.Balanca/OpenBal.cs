using System;
using System.Threading;
using System.Threading.Tasks;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Logging;
using OpenAC.Net.Devices;

namespace OpenAC.Net.Balanca;

/// <summary>Comunicação com balanças por dispositivos OpenAC (serial, TCP e outros).</summary>
public abstract class OpenBal : OpenDisposable, IOpenLog
{
    private readonly object sincronizacao = new();
    private readonly Func<IDeviceConfig, OpenDeviceStream> criarDispositivo;
    private ProtocoloBalanca protocolo;
    private CancellationTokenSource? cancelamento;
    private OpenDeviceStream? device;
    private ProtocoloBase? bal;
    private volatile bool monitorar;
    private int delayMonitoramento = 200;
    private int posicaoInicial;
    private int tamanhoResposta;

    internal OpenBal(IDeviceConfig device, Func<IDeviceConfig, OpenDeviceStream>? criarDispositivo = null)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
        this.criarDispositivo = criarDispositivo ?? (config => OpenDeviceFactory.Create(config));
    }

    /// <summary>Ocorre após leitura ou falha; no monitoramento é chamado em uma thread de trabalho.</summary>
    public event EventHandler<BalancaEventArgs>? AoLerPeso;

    /// <summary>Configuração do dispositivo de comunicação.</summary>
    public IDeviceConfig Device { get; }

    /// <summary>Protocolo de comunicação; só pode ser alterado com a conexão fechada.</summary>
    public ProtocoloBalanca Protocolo
    {
        get { lock (sincronizacao) return protocolo; }
        set
        {
            lock (sincronizacao)
            {
                VerificarDesconectado();
                ProtocolosBalanca.Obter(value);
                protocolo = value;
            }
        }
    }

    /// <summary>Posição inicial, baseada em 1, para Genérica e Marte. Zero desabilita a extração.</summary>
    public int PosicaoInicial
    {
        get => posicaoInicial;
        set { lock (sincronizacao) { VerificarDesconectado(); posicaoInicial = ValidarNaoNegativo(value); } }
    }

    /// <summary>Quantidade de caracteres do peso nos protocolos Genérica e Marte.</summary>
    public int TamanhoResposta
    {
        get => tamanhoResposta;
        set { lock (sincronizacao) { VerificarDesconectado(); tamanhoResposta = ValidarNaoNegativo(value); } }
    }

    /// <summary>Ativa a leitura das transmissões contínuas da balança.</summary>
    public bool IsMonitorar { get => monitorar; set => monitorar = value; }

    /// <summary>Intervalo positivo entre leituras do monitor em milissegundos. Padrão: 200.</summary>
    public int DelayMonitoramento
    {
        get => Volatile.Read(ref delayMonitoramento);
        set
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            Volatile.Write(ref delayMonitoramento, value);
        }
    }

    /// <summary>Indica se o dispositivo está conectado.</summary>
    public bool Conectado { get { lock (sincronizacao) return device?.Conectado == true; } }

    /// <summary>Abre o dispositivo e inicia o controle de monitoramento.</summary>
    public void Conectar()
    {
        lock (sincronizacao)
        {
            VerificarDesconectado();
            var definicao = ProtocolosBalanca.Obter(protocolo, PosicaoInicial, TamanhoResposta);
            Device.ControlePorta = false;
            var novo = criarDispositivo(Device);
            try
            {
                novo.Open();
                if (!novo.Conectado) throw new OpenException("Não foi possível conectar à balança.");
                device = novo;
                bal = new ProtocoloBase(novo, definicao);
                cancelamento = new CancellationTokenSource();
                var fonte = cancelamento;
                var protocoloAtual = bal;
                _ = Task.Run(() => MonitorarAsync(protocoloAtual, fonte));
            }
            catch
            {
                novo.Dispose();
                device = null;
                bal = null;
                throw;
            }
        }
    }

    /// <summary>Cancela o monitoramento e libera o dispositivo, inclusive após perda da conexão.</summary>
    public void Desconectar()
    {
        lock (sincronizacao)
        {
            if (device == null) throw new OpenException("A porta não está aberta.");
            cancelamento?.Cancel();
            cancelamento = null;
            var atual = device;
            device = null;
            bal = null;
            try { atual.Close(); }
            finally { atual.Dispose(); }
        }
    }

    /// <summary>Solicita um peso. Falhas de comunicação retornam -9 e são informadas em <see cref="AoLerPeso"/>.</summary>
    public decimal LerPeso()
    {
        BalancaEventArgs resultado;
        decimal peso;
        lock (sincronizacao)
        {
            if (device?.Conectado != true || bal == null) throw new OpenException("A conexão não está ativa.");
            try
            {
                peso = bal.LePeso();
                resultado = new BalancaEventArgs(bal.UltimaResposta, peso);
            }
            catch (Exception ex)
            {
                peso = -9;
                resultado = new BalancaEventArgs(bal.UltimaResposta, ex);
            }
        }
        AoLerPeso?.Invoke(this, resultado);
        return peso;
    }

    /// <summary>Interpreta uma resposta sem abrir o dispositivo, usando o protocolo configurado.</summary>
    public decimal InterpretarRespostaPeso(string resposta)
    {
        if (resposta == null) throw new ArgumentNullException(nameof(resposta));
        lock (sincronizacao)
            return ProtocolosBalanca.Obter(protocolo, PosicaoInicial, TamanhoResposta).Interpretar(resposta);
    }

    private async Task MonitorarAsync(ProtocoloBase atual, CancellationTokenSource fonte)
    {
        var token = fonte.Token;
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(DelayMonitoramento, token).ConfigureAwait(false);
                BalancaEventArgs? resultado = null;
                lock (sincronizacao)
                {
                    if (token.IsCancellationRequested) break;
                    if (!IsMonitorar || !ReferenceEquals(bal, atual)) continue;
                    try
                    {
                        atual.LeSerial(token);
                        resultado = new BalancaEventArgs(atual.UltimaResposta, atual.UltimoPesoLido);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
                    catch (Exception ex) { resultado = new BalancaEventArgs(atual.UltimaResposta, ex); }
                }
                if (resultado != null && !token.IsCancellationRequested)
                {
                    try { AoLerPeso?.Invoke(this, resultado); }
                    catch (Exception ex) { this.Log().Error("Falha no evento AoLerPeso.", ex); }
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        finally { fonte.Dispose(); }
    }

    private void VerificarDesconectado()
    {
        if (device != null) throw new OpenException("Desconecte a balança antes de alterar sua configuração.");
    }

    private static int ValidarNaoNegativo(int valor) =>
        valor >= 0 ? valor : throw new ArgumentOutOfRangeException(nameof(valor));

    /// <inheritdoc />
    protected override void DisposeManaged()
    {
        lock (sincronizacao)
            if (device != null) Desconectar();
    }
}
