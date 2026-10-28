using System;
using System.Linq;
using System.Text.RegularExpressions;
using static OpenAC.Net.Balanca.RespostaPeso;

namespace OpenAC.Net.Balanca;

internal static class ProtocolosBalanca
{
    internal static DefinicaoProtocolo Obter(ProtocoloBalanca protocolo, int posicao = 0, int tamanho = 0) => protocolo switch
    {
        ProtocoloBalanca.Toledo => new DefinicaoProtocolo(s => Toledo(s)),
        ProtocoloBalanca.Filizola => new DefinicaoProtocolo(Filizola, estabilizar: true),
        ProtocoloBalanca.Toledo2090 => new DefinicaoProtocolo(Toledo2090, limpar: false),
        ProtocoloBalanca.Toledo2090N => new DefinicaoProtocolo(Toledo2090N),
        ProtocoloBalanca.Toledo2180 or ProtocoloBalanca.ToledoTi420 =>
            new DefinicaoProtocolo(s => s.IndexOf('`') < 0 ? 0 : Numero(Campo(s, s.IndexOf('`') + 1, 6)), amostras: 5, terminadores: "\r"),
        ProtocoloBalanca.Urano => new DefinicaoProtocolo(Urano, estabilizar: true),
        ProtocoloBalanca.UranoUDC => new DefinicaoProtocolo(Urano, "\u0004", estabilizar: true),
        ProtocoloBalanca.UranoPOP => new DefinicaoProtocolo(UranoPOP, espera: 600),
        ProtocoloBalanca.LucasTec => new DefinicaoProtocolo(LucasTec, amostras: 4, terminadores: "\r"),
        ProtocoloBalanca.Magna => new DefinicaoProtocolo(Magna, "P"),
        ProtocoloBalanca.Digitron => new DefinicaoProtocolo(s => Estado(Quadro(Campo(s, 0, 10)), 100, -9)),
        ProtocoloBalanca.Digitron_UL => new DefinicaoProtocolo(s => Estado(Campo(s, 0, 8).TrimStart('D', 'E', 'F', '@'), 100, -9), terminadores: ""),
        ProtocoloBalanca.Magellan => new DefinicaoProtocolo(s => Toledo(s), "W"),
        ProtocoloBalanca.Lider => new DefinicaoProtocolo(Lider, "", estabilizar: true, terminadores: "\u0002"),
        ProtocoloBalanca.Rinnert => new DefinicaoProtocolo(s => Rinnert(s, false), estabilizar: true),
        ProtocoloBalanca.Muller => new DefinicaoProtocolo(s => Rinnert(s, true), estabilizar: true),
        ProtocoloBalanca.Saturno => new DefinicaoProtocolo(Saturno),
        ProtocoloBalanca.AFTS => new DefinicaoProtocolo(s => Estado(new string([.. Campo(s, 1, 7).Trim().Reverse()]), 1)),
        ProtocoloBalanca.Generica or ProtocoloBalanca.Marte =>
            new DefinicaoProtocolo(s => posicao > 0 && tamanho > 0 ? Estado(Campo(s, posicao - 1, tamanho)) : 0,
                protocolo == ProtocoloBalanca.Marte ? "D05" : "\u0005", estabilizar: true),
        ProtocoloBalanca.Libratek => new DefinicaoProtocolo(s => StatusST(s, 6), "", estabilizar: true),
        ProtocoloBalanca.Micheletti => new DefinicaoProtocolo(s => Numero(Campo(s, 3, 7))),
        ProtocoloBalanca.Alfa => new DefinicaoProtocolo(Alfa),
        ProtocoloBalanca.Toledo9091_8530_8540 => new DefinicaoProtocolo(Toledo9091),
        ProtocoloBalanca.WeightechWT1000 => new DefinicaoProtocolo(WeightechWT1000, estabilizar: true),
        ProtocoloBalanca.MarelCG62XL => new DefinicaoProtocolo(s => Estado(Campo(s, s.LastIndexOf('\u0002') + 14, 9), 1, -9), estabilizar: true),
        ProtocoloBalanca.WeightechWT3000_ABS => new DefinicaoProtocolo(s => StatusST(s, 7), estabilizar: true),
        ProtocoloBalanca.LibratekWT3000IR => new DefinicaoProtocolo(s => StatusST(s, 7)),
        ProtocoloBalanca.WeightechWT3000IR_ABS => new DefinicaoProtocolo(WeightechWT3000IR),
        ProtocoloBalanca.ToledoBCS21 => new DefinicaoProtocolo(BCS21),
        ProtocoloBalanca.Precision => new DefinicaoProtocolo(Precision, ""),
        ProtocoloBalanca.WeightechWT27R_ETH => new DefinicaoProtocolo(WeightechWT27R),
        ProtocoloBalanca.Capital => new DefinicaoProtocolo(s => Numero(Quadro(s, 'E').TrimEnd('k', 'g', ' '))),
        ProtocoloBalanca.LenkeLK2500 => new DefinicaoProtocolo(Lenke, ""),
        ProtocoloBalanca.WeighTRUTest => new DefinicaoProtocolo(s => Estado(UltimaLinha(s).Trim('[', ']')
            .Replace("U", "I"), 1, -9), "{RW}", estabilizar: true, terminadores: "]"),
        ProtocoloBalanca.Siciliano => new DefinicaoProtocolo(Siciliano),
        ProtocoloBalanca.Trentin => new DefinicaoProtocolo(Trentin),
        ProtocoloBalanca.Ramuza => new DefinicaoProtocolo(s => Toledo(s, true)),
        _ => throw new ArgumentOutOfRangeException(nameof(protocolo), protocolo, "Protocolo não suportado.")
    };

    private static decimal Toledo2090(string texto)
    {
        var linhas = texto.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var quadro = linhas.LastOrDefault(s => (s.Length == 16 || s.Length == 17) &&
            (s[0] == '\u0002' || s[1] == '\u0002'));
        if (quadro == null) return -9;
        var peso = Numero(Campo(quadro, 4, 7), quadro.Length == 16 ? 1000 : 100, -9);
        return char.ToLowerInvariant(quadro[2]) switch { 'r' => -peso, 's' => -10, _ => peso };
    }

    private static decimal Toledo2090N(string texto)
    {
        var quadro = texto.Substring(texto.LastIndexOf('\u0002') + 1);
        if (quadro.Length < 15) return 0;
        var peso = Numero(Campo(quadro, 3, 6), 1000);
        var tara = Numero(Campo(quadro, 9, 6));
        return tara <= 0
            ? quadro[1] switch { 'x' => -1, 'r' => -2, 's' => -10, _ => peso }
            : quadro[1] switch { 'y' => -1, 's' => 0, _ => peso };
    }

    private static decimal Toledo9091(string texto)
    {
        var quadro = texto.Substring(texto.LastIndexOf('\u0002') + 1);
        if (quadro.Length < 15) return -9;
        // Campos relativos ao STX; os comentários de ACBrBALToledo9091_8530_8540
        // descrevem as variantes com e sem espaço antes do peso.
        return Numero(Campo(quadro, quadro[3] == ' ' ? 4 : 3, 6), invalido: -9);
    }

    private static decimal BCS21(string texto)
    {
        var quadro = Quadro(texto);
        return Campo(quadro, 0, 2) switch
        {
            "S+" => Numero(Campo(quadro, 2, 8), invalido: -9),
            "S-" => -2, "D+" or "D-" => -1, _ => 0
        };
    }

    private static decimal Urano(string texto)
    {
        var pos = texto.IndexOf("PESO", StringComparison.Ordinal);
        if (pos >= 2 && texto[pos - 2] == ' ') return -1;
        var inicio = texto.IndexOf(':');
        if (inicio < 0) inicio = texto.IndexOf("N0", StringComparison.Ordinal) + 1;
        var fim = texto.IndexOf("kg", Math.Max(0, inicio), StringComparison.OrdinalIgnoreCase);
        if (inicio < 0 || fim <= inicio) return 0;
        var campo = texto.Substring(inicio + 1, fim - inicio - 1).Trim();
        return campo.StartsWith("-", StringComparison.Ordinal) ? -2 : Estado(campo);
    }

    private static decimal UranoPOP(string texto)
    {
        var inicio = texto.IndexOf("PESO L:", StringComparison.Ordinal);
        if (inicio >= 0) return Urano(texto.Substring(inicio));
        if (texto.Contains("TARA:")) return 0;
        var fim = texto.IndexOf("kg", StringComparison.OrdinalIgnoreCase);
        return fim < 7 ? 0 : Estado(Campo(texto, fim - 7, 6));
    }

    private static decimal LucasTec(string texto)
    {
        decimal Peso(string unidade)
        {
            var match = Regex.Match(texto, @"([+-]?\d+(?:[.,]\d+)?)\s+kg\s+" + unidade);
            return match.Success ? Numero(match.Groups[1].Value, 1000) : 0;
        }
        var bruto = Peso("B");
        var liquido = Peso("L");
        return Peso("T") > bruto ? -2 : liquido > 0 ? liquido : bruto;
    }

    private static decimal Magna(string texto)
    {
        var quadro = texto.Substring(texto.LastIndexOf('\t') + 1).Trim();
        var inicio = quadro.IndexOf(' ');
        if (inicio >= 0) quadro = quadro.Substring(inicio).Trim();
        var fim = quadro.IndexOf('k');
        if (fim >= 0) quadro = quadro.Substring(0, fim);
        return Estado(quadro.TrimEnd('\r', '\n'), 1000);
    }

    private static decimal Lider(string texto)
    {
        var fim = texto.LastIndexOf("E\u0002", StringComparison.Ordinal);
        if (fim < 0) fim = texto.LastIndexOf("I\u0002", StringComparison.Ordinal);
        if (fim < 0) return 0;
        var inicio = texto.LastIndexOf('\u0001', fim);
        var quadro = texto.Substring(inicio + 1, fim - inicio - 1).Replace(' ', '0');
        return texto[fim] == 'I' ? -1 : Numero(quadro);
    }

    private static decimal Rinnert(string texto, bool muller)
    {
        var pos = texto.IndexOf('+');
        var quadro = pos >= 0 ? Campo(texto, pos + 1, 7) : texto;
        if (quadro.EndsWith("\u0003", StringComparison.Ordinal)) quadro = quadro.TrimEnd('\u0003');
        else if (muller && pos < 0) quadro = new string([.. quadro.Where(c => c is >= '0' and <= '9')]);
        return Estado(quadro, 1);
    }

    private static decimal Saturno(string texto)
    {
        const string codificados = "°±²³´µ¶·¸¹";
        for (var i = 0; i < codificados.Length; i++) texto = texto.Replace(codificados[i], (char)('0' + i));
        texto = texto.Replace("[CR]", "").Replace("[LF]", "").Trim();
        var fim = texto.IndexOfAny(['E', 'O']);
        return Estado(fim >= 0 ? texto.Substring(0, fim) : Campo(texto, 0, 9), 1);
    }

    private static decimal Alfa(string texto)
    {
        var campo = Campo(texto, 4, 6).Trim();
        if (!campo.Contains('.') && !campo.Contains(','))
            campo = new string([.. campo.Where(c => c is >= '0' and <= '9')]);
        return Numero(campo, 100, Estado(texto));
    }

    private static decimal Precision(string texto)
    {
        var quadro = Quadro(texto);
        if (quadro.Length == 0) return 0;
        return quadro[0] switch
        {
            'G' or 'F' => 0, 'A' or '@' => -1,
            'H' or 'T' or 'M' or 'L' or 'I' => -2,
            _ => Numero(Campo(quadro, 2, 6), 1000)
        };
    }

    private static decimal Lenke(string texto)
    {
        var quadro = Quadro(texto);
        if (quadro.Contains("MN")) return -2;
        if (quadro.Contains("MP")) return -1;
        return Numero(Campo(quadro, 0, 8), 1000);
    }

    private static decimal WeightechWT1000(string texto)
    {
        var quadro = UltimaLinha(texto);
        if (quadro.Length != 25) return 0;
        return quadro[0] == '1' ? -1 : Numero(Campo(quadro, 18, 7));
    }

    private static decimal WeightechWT3000IR(string texto)
    {
        var quadro = UltimaLinha(texto);
        if (quadro.Length != 19 || !new[] { "GS", "NT", "TR" }.Contains(Campo(quadro, 3, 2)) ||
            (quadro[6] != '+' && quadro[6] != '-')) return 0;
        return StatusST(quadro, 7);
    }

    private static decimal WeightechWT27R(string texto)
    {
        var quadro = UltimaLinha(texto);
        if (quadro.StartsWith("I", StringComparison.Ordinal)) return -1;
        if (Campo(quadro, 8, 1) == "-") return -2;
        var peso = Campo(quadro, 9, 6);
        if (peso.Length != 6) return 0;
        if (peso == "999999") return -10;
        return Numero(peso, Campo(quadro, 3, 2).Equals("kg", StringComparison.OrdinalIgnoreCase) ? 1 : 1000);
    }

    private static decimal Siciliano(string texto)
    {
        var quadro = Quadro(texto);
        if (quadro.StartsWith("SIC", StringComparison.Ordinal))
            return Campo(quadro, 3, 1) == "-" ? -2 : Numero(Campo(quadro, 4, 6));
        if (!quadro.StartsWith("BR", StringComparison.Ordinal)) return 0;
        if (Campo(quadro, 2, 1) == "-") return -2;
        var bruto = Numero(Campo(quadro, 3, 6));
        var tara = Numero(Campo(quadro, 14, 6));
        return tara > bruto ? -2 : bruto > tara ? bruto - tara : bruto;
    }

    private static decimal Trentin(string texto)
    {
        if (texto.Contains('\u0002'))
            return Estado(Campo(Quadro(texto), 1, 7));
        var fim = texto.IndexOf('\r');
        if (fim < 7) return 0;
        return Numero(Campo(texto, fim - 6, 6), 1000);
    }
}
