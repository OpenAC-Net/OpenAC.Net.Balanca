// ***********************************************************************
// Assembly         : OpenAC.Net.Balanca
// Author           : Marcos Gerene
// Created          : 11-09-2021
//
// Last Modified By : RFTD
// Last Modified On : 11-09-2021
// ***********************************************************************
// <copyright file="OpenBal.cs" company="OpenAC .Net">
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
using OpenAC.Net.Devices;

namespace OpenAC.Net.Balanca;

/// <summary>
/// Implementa uma versão genérica de <see cref="OpenBal"/> para configuração fortemente tipada.
/// </summary>
/// <typeparam name="TConfig">Tipo da configuração do dispositivo, que implementa <see cref="IDeviceConfig"/>.</typeparam>
public sealed class OpenBal<TConfig> : OpenBal where TConfig : IDeviceConfig
{
    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância de <see cref="OpenBal{TConfig}"/> usando uma configuração padrão.
    /// </summary>
    public OpenBal() : base(Activator.CreateInstance<TConfig>())
    {
    }

    /// <summary>
    /// Inicializa uma nova instância de <see cref="OpenBal{TConfig}"/> usando a configuração informada.
    /// </summary>
    /// <param name="device">Configuração do dispositivo.</param>
    public OpenBal(TConfig device) : base(device)
    {
    }

    #endregion Constructors

    #region properties

    /// <summary>
    /// Configurações de comunicação com a impressora.
    /// </summary>
    public new TConfig Device => (TConfig)base.Device;

    #endregion properties
}