using System.Globalization;
using OpenAC.Net.Devices;

namespace OpenAC.Net.Balanca.Test;

public class ProtocolosTests
{
    // Vetores definidos pelos campos/documentação de ACBrBAL*.pas, sem depender
    // de porta serial. Cada modelo tem ao menos uma resposta de peso conhecida.
    [Test]
    [Arguments(ProtocoloBalanca.Toledo, "\u000201234\u0003", "1.234")]
    [Arguments(ProtocoloBalanca.Filizola, "\u000201234\u0003", "1.234")]
    [Arguments(ProtocoloBalanca.Filizola, "\u000212.345\r", "12.345")]
    [Arguments(ProtocoloBalanca.Filizola, "123456999999", "123.456")]
    [Arguments(ProtocoloBalanca.Toledo2090, "\r\u0002ax 000123400000\r", "1.234")]
    [Arguments(ProtocoloBalanca.Toledo2090N, "\u0002aaa001234000000\u0003", "1.234")]
    [Arguments(ProtocoloBalanca.Toledo2180, "`123.45\r", "123.45")]
    [Arguments(ProtocoloBalanca.ToledoTi420, "`123.45\r", "123.45")]
    [Arguments(ProtocoloBalanca.Urano, "1BB * PESO: 5,10kg", "5.10")]
    [Arguments(ProtocoloBalanca.UranoUDC, "1BB * PESO: 5,10kg", "5.10")]
    [Arguments(ProtocoloBalanca.UranoPOP, "PESO L:  5,100kg", "5.100")]
    [Arguments(ProtocoloBalanca.UranoPOP, "  5,100 kg", "5.100")]
    [Arguments(ProtocoloBalanca.LucasTec, "\u00020000 000001 20/06/05 50.00 kg B  0.60 kg T  49.40 kg L\r", "49.40")]
    [Arguments(ProtocoloBalanca.Magna, "\tP 12,345kg\r", "12.345")]
    [Arguments(ProtocoloBalanca.Digitron, "\u000201234\u0003", "12.34")]
    [Arguments(ProtocoloBalanca.Digitron_UL, "E1234.5", "1234.5")]
    [Arguments(ProtocoloBalanca.Digitron_UL, "D00005", "0.05")]
    [Arguments(ProtocoloBalanca.Magellan, "\u000201234\u0003", "1.234")]
    [Arguments(ProtocoloBalanca.Lider, "\u000100123. E\u0002", "123.0")]
    [Arguments(ProtocoloBalanca.Rinnert, "+0012.34\u0003", "12.34")]
    [Arguments(ProtocoloBalanca.Muller, "+0012.34\u0003", "12.34")]
    [Arguments(ProtocoloBalanca.Saturno, "±².³´µE\r\n", "12.345")]
    [Arguments(ProtocoloBalanca.AFTS, "\u0002543.210\r", "12.345")]
    [Arguments(ProtocoloBalanca.Generica, "PB012345kg\r", "12.345")]
    [Arguments(ProtocoloBalanca.Marte, "PB012345kg\r", "12.345")]
    [Arguments(ProtocoloBalanca.Libratek, "ST,GS,  12.345kg\r\n", "12.345")]
    [Arguments(ProtocoloBalanca.Micheletti, "PB 012.345\r", "12.345")]
    [Arguments(ProtocoloBalanca.Alfa, "ABCD001234", "12.34")]
    [Arguments(ProtocoloBalanca.Toledo9091_8530_8540, "\u0002abc012345000000\r", "12345")]
    [Arguments(ProtocoloBalanca.Toledo9091_8530_8540, "\u0002abc 012345000000\r", "12345")]
    [Arguments(ProtocoloBalanca.WeightechWT1000, "0,010.000,001.000,009.000\r\n", "9.000")]
    [Arguments(ProtocoloBalanca.MarelCG62XL, "\u0002ABCDEFGHIJKLM00012.345\r", "12.345")]
    [Arguments(ProtocoloBalanca.WeightechWT3000_ABS, "ST,NT,+  0.876kg\r\n", "0.876")]
    [Arguments(ProtocoloBalanca.LibratekWT3000IR, "ST,GS,+   0.298  kg\r\n", "0.298")]
    [Arguments(ProtocoloBalanca.WeightechWT3000IR_ABS, "ST,GS,+  300.00  kg\r\n", "300.00")]
    [Arguments(ProtocoloBalanca.ToledoBCS21, "\u0002S+  12.345\r", "12.345")]
    [Arguments(ProtocoloBalanca.Precision, "B 012345\r", "12.345")]
    [Arguments(ProtocoloBalanca.WeightechWT27R_ETH, "EB,kg,B: 001234,T:000000,L: 001234\r\n", "1234")]
    [Arguments(ProtocoloBalanca.WeightechWT27R_ETH, "EB, g,B: 001234,T:000000,L: 001234\r\n", "1.234")]
    [Arguments(ProtocoloBalanca.Capital, "E 12.345k\r", "12.345")]
    [Arguments(ProtocoloBalanca.LenkeLK2500, "\u000200012345EP\r", "12.345")]
    [Arguments(ProtocoloBalanca.WeighTRUTest, "[102.5]", "102.5")]
    [Arguments(ProtocoloBalanca.Siciliano, "\u0002SIC 012345kgB\u0003", "12345")]
    [Arguments(ProtocoloBalanca.Siciliano, "\u0002BR 012345kgTR 000345kg\u0003", "12000")]
    [Arguments(ProtocoloBalanca.Trentin, "D000658\rD000659\r", "0.658")]
    [Arguments(ProtocoloBalanca.Trentin, "\u0002+012.345KG \r", "12.345")]
    [Arguments(ProtocoloBalanca.Ramuza, "\u000201234\u0003", "1.234")]
    public async Task InterpretaPesoIndependenteDaCultura(ProtocoloBalanca protocolo, string resposta, string esperado)
    {
        using var balanca = new OpenBal<SerialConfig> { Protocolo = protocolo, PosicaoInicial = 3, TamanhoResposta = 6 };
        foreach (var cultura in new[] { "pt-BR", "en-US", "de-DE" })
        {
            var anterior = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultura);
                await Assert.That(balanca.InterpretarRespostaPeso(resposta))
                    .IsEqualTo(decimal.Parse(esperado, CultureInfo.InvariantCulture));
            }
            finally { CultureInfo.CurrentCulture = anterior; }
        }
    }

    [Test]
    [Arguments(ProtocoloBalanca.Toledo, "\u0002IIIII\u0003", -1)]
    [Arguments(ProtocoloBalanca.Toledo, "\u0002NNNNN\u0003", -2)]
    [Arguments(ProtocoloBalanca.Toledo, "\u0002SSSSS\u0003", -10)]
    [Arguments(ProtocoloBalanca.Toledo, "\u0002CCCCC\u0003", -11)]
    [Arguments(ProtocoloBalanca.Toledo, "\u0002EEEEE\u0003", -12)]
    [Arguments(ProtocoloBalanca.Toledo, "\u0002+III,III\u0003", -1)]
    [Arguments(ProtocoloBalanca.Toledo, "\u0002-III,III\u0003", -2)]
    [Arguments(ProtocoloBalanca.Toledo, "\u0002+SSS,SSS\u0003", -10)]
    [Arguments(ProtocoloBalanca.Filizola, "\u0002I\u0003", -1)]
    [Arguments(ProtocoloBalanca.Filizola, "\u0002N\r", -2)]
    [Arguments(ProtocoloBalanca.Digitron, "", -9)]
    [Arguments(ProtocoloBalanca.Lider, "\u000100123. I\u0002", -1)]
    [Arguments(ProtocoloBalanca.ToledoBCS21, "\u0002D+  12.345\r", -1)]
    [Arguments(ProtocoloBalanca.ToledoBCS21, "\u0002S-  12.345\r", -2)]
    [Arguments(ProtocoloBalanca.Precision, "A 012345\r", -1)]
    [Arguments(ProtocoloBalanca.Precision, "H 012345\r", -2)]
    [Arguments(ProtocoloBalanca.Precision, "G 000000\r", 0)]
    [Arguments(ProtocoloBalanca.LenkeLK2500, "\u000200012345MP\r", -1)]
    [Arguments(ProtocoloBalanca.LenkeLK2500, "\u000200012345MN\r", -2)]
    [Arguments(ProtocoloBalanca.WeighTRUTest, "[U102.5]", -1)]
    [Arguments(ProtocoloBalanca.WeightechWT3000_ABS, "US,NT,+  0.876kg\r\n", -1)]
    [Arguments(ProtocoloBalanca.LibratekWT3000IR, "OL,GS,-   0.298  kg\r\n", -2)]
    [Arguments(ProtocoloBalanca.WeightechWT3000IR_ABS, "OL,GS,+  300.00  kg\r\n", -10)]
    [Arguments(ProtocoloBalanca.WeightechWT27R_ETH, "IB,kg,B: 001234,T:000000,L: 001234\r\n", -1)]
    [Arguments(ProtocoloBalanca.WeightechWT27R_ETH, "EB,kg,B:-001234,T:000000,L:-001234\r\n", -2)]
    [Arguments(ProtocoloBalanca.WeightechWT27R_ETH, "EB,kg,B: 999999,T:000000,L: 999999\r\n", -10)]
    [Arguments(ProtocoloBalanca.Toledo2090N, "\u0002axa001234000000\u0003", -1)]
    [Arguments(ProtocoloBalanca.Toledo2090N, "\u0002ara001234000000\u0003", -2)]
    [Arguments(ProtocoloBalanca.Toledo2090N, "\u0002asa001234000000\u0003", -10)]
    [Arguments(ProtocoloBalanca.Urano, "1BB   PESO: 5,10kg", -1)]
    [Arguments(ProtocoloBalanca.Urano, "1BB * PESO:-5,10kg", -2)]
    public async Task ReconheceEstados(ProtocoloBalanca protocolo, string resposta, int esperado) =>
        await Assert.That(ProtocolosBalanca.Obter(protocolo).Interpretar(resposta)).IsEqualTo((decimal)esperado);

    [Test]
    public async Task ToledoReconheceVariantesECasasDecimais()
    {
        var interpretar = ProtocolosBalanca.Obter(ProtocoloBalanca.Toledo).Interpretar;
        await Assert.That(interpretar("\u0002a012345\b000000000000\r")).IsEqualTo(123.45M); // A
        await Assert.That(interpretar("\u0002a012345a000000000000\r")).IsEqualTo(12.345M); // não retém divisor anterior
        await Assert.That(interpretar("\u0002\bba012345000000\r")).IsEqualTo(123.45M); // P03
        await Assert.That(interpretar("\u0002aba012345000000\r")).IsEqualTo(12.345M);
        await Assert.That(interpretar("\u0002" + "02abc012345" + new string('0', 48) + "\u0003")).IsEqualTo(12.345M); // Ethernet
        await Assert.That(interpretar("\u000201234\u0003\u000205678\u0003")).IsEqualTo(5.678M);
        await Assert.That(interpretar("\u0002+123,456kg\r")).IsEqualTo(123.456M);
    }

    [Test]
    public async Task LiderPrefereUltimoPesoEstavelEWeightechUltimoQuadro()
    {
        await Assert.That(ProtocolosBalanca.Obter(ProtocoloBalanca.Lider)
            .Interpretar("\u000100123. E\u0002\u000100124. I\u0002")).IsEqualTo(123M);
        await Assert.That(ProtocolosBalanca.Obter(ProtocoloBalanca.WeightechWT3000IR_ABS)
            .Interpretar("ST,GS,+  300.00  kg\r\nST,GS,+  400.00  kg\r\n")).IsEqualTo(400M);
    }

    [Test]
    public async Task TodosModelosToleramRetornosCurtosSemExcecoes()
    {
        foreach (var modelo in Enum.GetValues<ProtocoloBalanca>())
        foreach (var resposta in new[] { "", "\u0002", "\u0003", "\r\n", "?", "ST", "EB", "SIC", "\u0002abc" })
        {
            var valor = ProtocolosBalanca.Obter(modelo).Interpretar(resposta);
            await Assert.That(valor).IsBetween(-12M, 0M);
        }
        await Assert.That((int)Enum.Parse<ProtocoloBalanca>("Toledo")).IsEqualTo(0);
        await Assert.That((int)Enum.Parse<ProtocoloBalanca>("Filizola")).IsEqualTo(1);
        await Assert.That(Enum.GetValues<ProtocoloBalanca>().Length).IsEqualTo(39);
    }
}
