using System;

namespace OpenAC.Net.Balanca;

internal sealed class DefinicaoProtocolo
{
    internal DefinicaoProtocolo(Func<string, decimal> interpretar, string comando = "\u0005",
        bool limpar = true, bool estabilizar = false, int amostras = 1,
        string terminadores = "\u0003\r\n", int espera = 200)
    {
        Interpretar = interpretar;
        Comando = comando;
        Limpar = limpar;
        Estabilizar = estabilizar;
        Amostras = amostras;
        Terminadores = terminadores.ToCharArray();
        Espera = espera;
    }

    internal Func<string, decimal> Interpretar { get; }
    internal string Comando { get; }
    internal bool Limpar { get; }
    internal bool Estabilizar { get; }
    internal int Amostras { get; }
    internal char[] Terminadores { get; }
    internal int Espera { get; }
}
