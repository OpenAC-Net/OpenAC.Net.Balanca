using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using OpenAC.Net.Core.Logging;
using OpenAC.Net.Devices;

namespace OpenAC.Net.Balanca;

internal sealed class ProtocoloBase : IOpenLog
{
    private readonly OpenDeviceStream device;
    private readonly DefinicaoProtocolo definicao;
    private string pendente = "";

    internal ProtocoloBase(OpenDeviceStream device, DefinicaoProtocolo definicao)
    {
        this.device = device;
        this.definicao = definicao;
    }

    // Latin-1 preserva cada byte, inclusive os dígitos codificados da Saturno.
    internal Encoding Encoder { get; set; } = Encoding.GetEncoding(28591);
    internal string UltimaResposta { get; private set; } = "";
    internal decimal UltimoPesoLido { get; private set; }

    internal decimal LePeso(CancellationToken cancellationToken = default)
    {
        UltimaResposta = "";
        UltimoPesoLido = -9;
        var tempo = Stopwatch.StartNew();
        do
        {
            SolicitarPeso();
            if (definicao.Comando.Length > 0 && cancellationToken.WaitHandle.WaitOne(definicao.Espera))
                cancellationToken.ThrowIfCancellationRequested();
            LeSerial(cancellationToken);
            if (!definicao.Estabilizar || UltimoPesoLido != -1) break;
        } while (tempo.ElapsedMilliseconds < Math.Max(1, device.Config.TimeOut));
        return UltimoPesoLido;
    }

    internal void SolicitarPeso()
    {
        if (definicao.Comando.Length == 0) return;
        if (definicao.Limpar)
        {
            pendente = "";
            device.Limpar();
        }
        this.Log().Info($"TX: [{definicao.Comando}]");
        device.Write(Encoder.GetBytes(definicao.Comando));
    }

    internal void LeSerial(CancellationToken cancellationToken = default)
    {
        UltimoPesoLido = -9;
        UltimaResposta = "";
        var anteriores = new decimal[3];
        for (var i = 0; i < definicao.Amostras; i++)
        {
            UltimoPesoLido = -9;
            UltimaResposta = "";
            UltimaResposta = LerResposta(cancellationToken);
            this.Log().Info($"RX: [{UltimaResposta}]");
            UltimoPesoLido = definicao.Interpretar(UltimaResposta);
            anteriores[i % 3] = UltimoPesoLido;
        }
        if (definicao.Amostras > 1 && (anteriores[0] != anteriores[1] || anteriores[1] != anteriores[2]))
            UltimoPesoLido = -1;
    }

    private string LerResposta(CancellationToken cancellationToken)
    {
        var tempo = Stopwatch.StartNew();
        var ultimoDado = 0L;
        while (tempo.ElapsedMilliseconds < Math.Max(1, device.Config.TimeOut))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bytes = device.Read();
            if (bytes.Length > 0)
            {
                pendente += Encoder.GetString(bytes);
                ultimoDado = tempo.ElapsedMilliseconds;
                if (pendente.Length > 65536) throw new FormatException("Resposta da balança excedeu 64 KiB.");
            }
            if (definicao.Terminadores.Length > 0)
            {
                var fim = definicao.Amostras > 1
                    ? pendente.IndexOfAny(definicao.Terminadores)
                    : pendente.LastIndexOfAny(definicao.Terminadores);
                if (fim >= 0)
                {
                    var resposta = pendente.Substring(0, fim + 1);
                    pendente = pendente.Substring(fim + 1);
                    if (!string.IsNullOrWhiteSpace(resposta)) return resposta;
                }
            }
            else if (pendente.Length > 0 && tempo.ElapsedMilliseconds - ultimoDado >= 50)
            {
                var resposta = pendente;
                pendente = "";
                return resposta;
            }
            if (cancellationToken.WaitHandle.WaitOne(10)) cancellationToken.ThrowIfCancellationRequested();
        }
        UltimaResposta = pendente;
        pendente = "";
        throw new TimeoutException("A balança não retornou um quadro completo no tempo configurado.");
    }
}
