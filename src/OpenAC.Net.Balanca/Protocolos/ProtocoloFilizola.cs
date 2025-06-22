// ***********************************************************************
// Assembly         : OpenAC.Net.Balanca
// Author           : Marcos Gerene
// Created          : 11-09-2021
//
// Last Modified By : RFTD
// Last Modified On : 11-09-2021
// ***********************************************************************
// <copyright file="ProtocoloFilizola.cs" company="OpenAC .Net">
//		        		   The MIT License (MIT)
//	     		    Copyright (c) 2014-2022 Projeto OpenAC .Net
//
//	 Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//	 The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//	 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary></summary>
// ***********************************************************************

using OpenAC.Net.Core.Extensions;
using OpenAC.Net.Devices;

namespace OpenAC.Net.Balanca;

/// <summary>
/// Implementa o protocolo de comunicação para balanças Filizola.
/// </summary>
internal sealed class ProtocoloFilizola : ProtocoloBase
{
    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="ProtocoloFilizola"/>.
    /// </summary>
    /// <param name="device">O dispositivo de comunicação.</param>
    public ProtocoloFilizola(OpenDeviceStream device) : base(device)
    {
    }

    #endregion Constructors

    #region Methods

    /// <summary>
    /// Lê o peso da balança Filizola, aguardando estabilização se necessário.
    /// </summary>
    /// <returns>O peso lido ou um valor negativo indicando erro/condição especial.</returns>
    public override decimal LePeso()
    {
        // A Filizola pode responder com Instavel inicalmente, mas depois ela poderia
        // estabilizar... Portanto utilizará a função AguardarRespostaPeso
        return AguardarRespostaPeso(true);
    }

    /// <summary>
    /// Interpreta a resposta recebida da balança Filizola e retorna o peso correspondente.
    /// </summary>
    /// <returns>
    /// O peso lido, ou valores negativos para indicar condições especiais:
    /// -1 para peso instável, -2 para peso negativo, -10 para sobrecarga.
    /// </returns>
    protected override decimal InterpretarRepostaPeso()
    {
        if (UltimaResposta!.IsEmpty()) return 0;
        var response = UltimaResposta!.Substring(UltimaResposta.Length - 5);

        return response[0] switch
        {
            // Peso instável
            'I' => -1,
            // Peso negativo
            'N' => -2,
            // Sobrecarga
            'S' => -10,
            _ => decimal.Parse(response) / 1000M
        };
    }

    #endregion Methods
}