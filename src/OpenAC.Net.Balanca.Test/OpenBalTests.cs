using OpenAC.Net.Core;

namespace OpenAC.Net.Balanca.Test;

public class OpenBalTests
{
    [Test]
    public async Task LeituraPublicaDisparaEventoERetornaPeso()
    {
        using var device = new FakeDevice();
        device.AoEnviar = () => device.Enfileirar("\u000201234\u0003");
        using var balanca = new TestScale(device);
        BalancaEventArgs? evento = null;
        balanca.AoLerPeso += (_, e) => evento = e;
        balanca.Conectar();
        await Assert.That(balanca.LerPeso()).IsEqualTo(1.234M);
        await Assert.That(evento!.Leitura).IsEqualTo("\u000201234\u0003");
        await Assert.That(evento.Peso).IsEqualTo(1.234M);
        await Assert.That(evento.Excecao).IsNull();
        await Assert.That(device.Config.ControlePorta).IsFalse();
    }

    [Test]
    public async Task ErroDeComunicacaoNaoRetornaPesoAnterior()
    {
        using var device = new FakeDevice();
        device.AoEnviar = () => device.Enfileirar("\u000201234\u0003");
        using var balanca = new TestScale(device);
        balanca.Conectar();
        balanca.LerPeso();
        device.FalharLeitura = true;
        BalancaEventArgs? erro = null;
        balanca.AoLerPeso += (_, e) => erro = e;
        await Assert.That(balanca.LerPeso()).IsEqualTo(-9M);
        await Assert.That(erro!.Excecao).IsTypeOf<IOException>();
        await Assert.That(erro.Peso).IsNull();
    }

    [Test]
    public async Task ValidaEstadoEParametros()
    {
        using var device = new FakeDevice();
        using var balanca = new TestScale(device);
        await Assert.That(() => balanca.LerPeso()).Throws<OpenException>();
        await Assert.That(() => balanca.Desconectar()).Throws<OpenException>();
        await Assert.That(() => balanca.Protocolo = (ProtocoloBalanca)999).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => balanca.DelayMonitoramento = 0).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => balanca.PosicaoInicial = -1).Throws<ArgumentOutOfRangeException>();
        balanca.Conectar();
        await Assert.That(() => balanca.Protocolo = ProtocoloBalanca.Filizola).Throws<OpenException>();
        await Assert.That(() => balanca.Conectar()).Throws<OpenException>();
        balanca.Desconectar();
        await Assert.That(device.Liberado).IsTrue();
        await Assert.That(balanca.Conectado).IsFalse();
    }

    [Test]
    public async Task FalhaNaAberturaLiberaDispositivoEPermiteNovaTentativa()
    {
        var falho = new FakeDevice { FalharAbertura = true };
        using var novo = new FakeDevice();
        var tentativa = 0;
        using var balanca = new TestScale(() => tentativa++ == 0 ? falho : novo);
        await Assert.That(() => balanca.Conectar()).Throws<OpenException>();
        await Assert.That(falho.Liberado).IsTrue();
        balanca.Conectar();
        await Assert.That(balanca.Conectado).IsTrue();
    }

    [Test]
    public async Task MonitorDesativadoNaoLeDispositivo()
    {
        using var device = new FakeDevice();
        using var balanca = new TestScale(device) { DelayMonitoramento = 10 };
        balanca.Conectar();
        await Task.Delay(60);
        await Assert.That(Volatile.Read(ref device.Leituras)).IsEqualTo(0);
        balanca.Desconectar();
    }

    [Test]
    public async Task MonitorPodeDesconectarDentroDoEvento()
    {
        using var device = new FakeDevice();
        device.Enfileirar("\u000201234\u0003");
        using var balanca = new TestScale(device) { DelayMonitoramento = 10, IsMonitorar = true };
        var resultado = new TaskCompletionSource<BalancaEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        balanca.AoLerPeso += (_, e) =>
        {
            balanca.Desconectar();
            resultado.TrySetResult(e);
        };
        balanca.Conectar();
        var evento = await resultado.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await Assert.That(evento.Peso).IsEqualTo(1.234M);
        await Assert.That(device.Liberado).IsTrue();
        await Assert.That(balanca.Conectado).IsFalse();
    }

    [Test]
    public async Task ReconexaoNaoReutilizaMonitorOuDispositivoAnterior()
    {
        using var primeiro = new FakeDevice();
        using var segundo = new FakeDevice();
        segundo.Enfileirar("\u000205678\u0003");
        var tentativa = 0;
        using var balanca = new TestScale(() => tentativa++ == 0 ? primeiro : segundo) { DelayMonitoramento = 10 };
        balanca.Conectar();
        balanca.Desconectar();
        var resultado = new TaskCompletionSource<BalancaEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        balanca.AoLerPeso += (_, e) => { balanca.IsMonitorar = false; resultado.TrySetResult(e); };
        balanca.IsMonitorar = true;
        balanca.Conectar();
        var evento = await resultado.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await Assert.That(evento.Peso).IsEqualTo(5.678M);
        await Assert.That(Volatile.Read(ref primeiro.Leituras)).IsEqualTo(0);
    }
}
