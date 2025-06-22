// ***********************************************************************
// Assembly         : OpenAC.Net.Balanca
// Author           : Marcos Gerene
// Created          : 11-09-2021
//
// Last Modified By : RFTD
// Last Modified On : 11-09-2021
// ***********************************************************************
// <copyright file="BalancaEventArgs.cs" company="OpenAC .Net">
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

using System;

namespace OpenAC.Net.Balanca;

/// <summary>
/// Fornece dados para eventos relacionados à balança.
/// </summary>
public class BalancaEventArgs : EventArgs
{
    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BalancaEventArgs"/> com a leitura e o peso.
    /// </summary>
    /// <param name="leitura">A leitura bruta recebida da balança.</param>
    /// <param name="peso">O peso lido da balança.</param>
    public BalancaEventArgs(string leitura, decimal peso)
    {
        Leitura = leitura;
        Peso = peso;
    }

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BalancaEventArgs"/> com a leitura e uma exceção.
    /// </summary>
    /// <param name="leitura">A leitura bruta recebida da balança.</param>
    /// <param name="exception">A exceção ocorrida durante a leitura.</param>
    public BalancaEventArgs(string leitura, Exception exception)
    {
        Leitura = leitura;
        Excecao = exception;
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Obtém ou define a leitura bruta recebida da balança.
    /// </summary>
    public string Leitura { get; set; }

    /// <summary>
    /// Obtém o peso lido da balança, se disponível.
    /// </summary>
    public decimal? Peso { get; private set; }

    /// <summary>
    /// Obtém ou define a exceção ocorrida durante a leitura, se houver.
    /// </summary>
    public Exception? Excecao { get; set; }

    #endregion Properties
}