using System;
using System.Globalization;
using System.Linq;

namespace OpenAC.Net.Balanca;

// Campos e estados conferidos com as unidades ACBrBAL*.pas do ACBrSerial.
// A conversão é independente da cultura e não utiliza separadores de milhares.
internal static class RespostaPeso
{
    internal static string Campo(string texto, int inicio, int tamanho) =>
        inicio < 0 || inicio >= texto.Length ? "" : texto.Substring(inicio, Math.Min(tamanho, texto.Length - inicio));

    internal static decimal Numero(string texto, decimal divisor = 1, decimal invalido = 0)
    {
        texto = texto.Trim().Replace(',', '.');
        return decimal.TryParse(texto, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var peso)
            ? peso / (texto.Contains('.') ? 1 : divisor)
            : invalido;
    }

    internal static decimal Estado(string texto, decimal divisor = 1000, decimal invalido = 0)
    {
        texto = texto.Trim();
        if (texto.Length == 0) return invalido;
        return texto[0] switch
        {
            'I' => -1, 'N' => -2, 'S' => -10, 'C' => -11, 'E' => -12,
            _ => Numero(texto, divisor, invalido)
        };
    }

    internal static string Quadro(string texto, char inicio = '\u0002')
    {
        var pos = texto.LastIndexOf(inicio);
        texto = texto.Substring(pos + 1);
        var fim = texto.IndexOfAny(['\u0003', '\r', '\n']);
        return (fim < 0 ? texto : texto.Substring(0, fim)).Trim();
    }

    internal static string UltimaLinha(string texto) =>
        texto.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim() ?? "";

    internal static decimal Toledo(string texto, bool ramuza = false)
    {
        var inicio = texto.LastIndexOf('\u0002');
        var quadro = texto.Substring(inicio + 1);
        var divisor = 1000M;
        string peso;
        if (!ramuza && quadro.StartsWith("02", StringComparison.Ordinal) && Campo(quadro, 59, 1) == "\u0003")
            peso = Campo(quadro, 5, 6);
        else if (!ramuza && Campo(quadro, 20, 1) == "\r")
        {
            divisor = (quadro[7] & 8) != 0 ? 100 : 1000;
            peso = Campo(quadro, 1, 6);
        }
        else if (!ramuza && Campo(quadro, 15, 1) == "\r" && !quadro.Contains('\u0003'))
        {
            divisor = (quadro[0] & 8) != 0 ? 100 : 1000;
            peso = Campo(quadro, 3, 6);
        }
        else peso = Quadro(texto).Replace("kg", "").Trim();

        peso = peso.Replace("+III,III", "I").Replace("-III,III", "N")
            .Replace("+SSS,SSS", "S");
        return Estado(peso, divisor);
    }

    internal static decimal Filizola(string texto)
    {
        var peso = Quadro(texto);
        return peso.Length > 10 ? Numero(Campo(peso, 0, 6), 1000) : Estado(peso);
    }

    internal static decimal StatusST(string texto, int inicioPeso)
    {
        texto = UltimaLinha(texto);
        return Campo(texto, 0, 2) switch
        {
            "ST" => Numero(Campo(texto, inicioPeso, 8).TrimEnd('k', 'g', ' ')),
            "US" => -1,
            "OL" => Campo(texto, 6, 1) == "-" ? -2 : -10,
            _ => -9
        };
    }
}
