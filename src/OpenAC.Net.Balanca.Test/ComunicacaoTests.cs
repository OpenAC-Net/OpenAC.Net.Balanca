using System.Text;

namespace OpenAC.Net.Balanca.Test;

public class ComunicacaoTests
{
    [Test]
    [Arguments(ProtocoloBalanca.Toledo, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Filizola, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Toledo2090, "\u0005", false)]
    [Arguments(ProtocoloBalanca.Toledo2180, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Urano, "\u0005", true)]
    [Arguments(ProtocoloBalanca.LucasTec, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Magna, "P", true)]
    [Arguments(ProtocoloBalanca.Digitron, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Magellan, "W", true)]
    [Arguments(ProtocoloBalanca.UranoPOP, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Lider, "", false)]
    [Arguments(ProtocoloBalanca.Rinnert, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Muller, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Saturno, "\u0005", true)]
    [Arguments(ProtocoloBalanca.AFTS, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Generica, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Libratek, "", false)]
    [Arguments(ProtocoloBalanca.Micheletti, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Alfa, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Toledo9091_8530_8540, "\u0005", true)]
    [Arguments(ProtocoloBalanca.WeightechWT1000, "\u0005", true)]
    [Arguments(ProtocoloBalanca.MarelCG62XL, "\u0005", true)]
    [Arguments(ProtocoloBalanca.WeightechWT3000_ABS, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Toledo2090N, "\u0005", true)]
    [Arguments(ProtocoloBalanca.ToledoBCS21, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Precision, "", false)]
    [Arguments(ProtocoloBalanca.Digitron_UL, "\u0005", true)]
    [Arguments(ProtocoloBalanca.LibratekWT3000IR, "\u0005", true)]
    [Arguments(ProtocoloBalanca.ToledoTi420, "\u0005", true)]
    [Arguments(ProtocoloBalanca.WeightechWT27R_ETH, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Capital, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Marte, "D05", true)]
    [Arguments(ProtocoloBalanca.LenkeLK2500, "", false)]
    [Arguments(ProtocoloBalanca.WeighTRUTest, "{RW}", true)]
    [Arguments(ProtocoloBalanca.UranoUDC, "\u0004", true)]
    [Arguments(ProtocoloBalanca.Siciliano, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Trentin, "\u0005", true)]
    [Arguments(ProtocoloBalanca.WeightechWT3000IR_ABS, "\u0005", true)]
    [Arguments(ProtocoloBalanca.Ramuza, "\u0005", true)]
    public async Task EnviaComandoCorreto(ProtocoloBalanca protocolo, string comando, bool limpar)
    {
        using var device = new FakeDevice();
        new ProtocoloBase(device, ProtocolosBalanca.Obter(protocolo)).SolicitarPeso();
        await Assert.That(device.Limpezas).IsEqualTo(limpar ? 1 : 0);
        await Assert.That(device.Envios.Count).IsEqualTo(comando.Length == 0 ? 0 : 1);
        if (comando.Length > 0)
            await Assert.That(Convert.ToHexString(device.Envios[0])).IsEqualTo(Convert.ToHexString(Encoding.ASCII.GetBytes(comando)));
    }

    [Test]
    public async Task JuntaFragmentosEPreservaProximoQuadro()
    {
        using var device = new FakeDevice();
        device.Enfileirar("\u000201");
        device.Enfileirar("234\u0003\u000205");
        device.Enfileirar("678\u0003");
        var protocolo = new ProtocoloBase(device, ProtocolosBalanca.Obter(ProtocoloBalanca.Toledo));
        protocolo.LeSerial();
        await Assert.That(protocolo.UltimoPesoLido).IsEqualTo(1.234M);
        protocolo.LeSerial();
        await Assert.That(protocolo.UltimoPesoLido).IsEqualTo(5.678M);
    }

    [Test]
    public async Task UsaUltimoQuadroCompletoDoBuffer()
    {
        using var device = new FakeDevice();
        device.Enfileirar("\u000201234\u0003\u000205678\u0003\u000200");
        var protocolo = new ProtocoloBase(device, ProtocolosBalanca.Obter(ProtocoloBalanca.Toledo));
        protocolo.LeSerial();
        await Assert.That(protocolo.UltimoPesoLido).IsEqualTo(5.678M);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task ConfereTresUltimasAmostrasToledo(bool estavel)
    {
        using var device = new FakeDevice();
        device.Enfileirar("`111.11\r`222.22\r`123.45\r`123.45\r" + (estavel ? "`123.45\r" : "`123.46\r"));
        var protocolo = new ProtocoloBase(device, ProtocolosBalanca.Obter(ProtocoloBalanca.Toledo2180));
        protocolo.LeSerial();
        await Assert.That(protocolo.UltimoPesoLido).IsEqualTo(estavel ? 123.45M : -1M);
    }

    [Test]
    public async Task FilizolaReenviaAteEstabilizar()
    {
        using var device = new FakeDevice();
        device.Config.TimeOut = 1000;
        device.AoEnviar = () => device.Enfileirar(device.Envios.Count == 1 ? "\u0002I\u0003" : "\u000201234\u0003");
        var protocolo = new ProtocoloBase(device, ProtocolosBalanca.Obter(ProtocoloBalanca.Filizola));
        await Assert.That(protocolo.LePeso()).IsEqualTo(1.234M);
        await Assert.That(device.Envios.Count).IsEqualTo(2);
    }

    [Test]
    public async Task TimeoutMantemRespostaParcial()
    {
        using var device = new FakeDevice();
        device.Enfileirar("\u000201");
        var protocolo = new ProtocoloBase(device, ProtocolosBalanca.Obter(ProtocoloBalanca.Toledo));
        await Assert.That(() => protocolo.LeSerial()).Throws<TimeoutException>();
        await Assert.That(protocolo.UltimoPesoLido).IsEqualTo(-9M);
        await Assert.That(protocolo.UltimaResposta).IsEqualTo("\u000201");
    }

    [Test]
    public async Task SaturnoPreservaBytesLatin1()
    {
        using var device = new FakeDevice();
        device.Enfileirar("±².³´µE\r");
        var protocolo = new ProtocoloBase(device, ProtocolosBalanca.Obter(ProtocoloBalanca.Saturno));
        protocolo.LeSerial();
        await Assert.That(protocolo.UltimoPesoLido).IsEqualTo(12.345M);
    }
}
