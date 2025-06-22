// ***********************************************************************
// Assembly         : OpenAC.Net.Balanca
// Author           : Marcos Gerene
// Created          : 11-09-2021
//
// Last Modified By : RFTD
// Last Modified On : 11-09-2021
// ***********************************************************************
// <copyright file="ProtocoloBase.cs" company="OpenAC .Net">
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
using System.Text;
using System.Threading;
using OpenAC.Net.Core.Logging;
using OpenAC.Net.Devices;

namespace OpenAC.Net.Balanca;

/// <summary>
/// Classe base abstrata para protocolos de comunicação com balanças.
/// </summary>
internal abstract class ProtocoloBase : IOpenLog
{
    #region Fields

    /// <summary>
    /// Instância do dispositivo de comunicação.
    /// </summary>
    private readonly OpenDeviceStream device;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="ProtocoloBase"/>.
    /// </summary>
    /// <param name="device">Dispositivo de comunicação serial.</param>
    protected ProtocoloBase(OpenDeviceStream device)
    {
        this.device = device;
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Codificação utilizada para comunicação.
    /// </summary>
    public Encoding? Encoder { get; set; }

    /// <summary>
    /// Última resposta recebida da balança.
    /// </summary>
    public string? UltimaResposta { get; protected set; }

    /// <summary>
    /// Último peso lido da balança.
    /// </summary>
    public decimal UltimoPesoLido { get; protected set; }

    #endregion Properties

    #region Methods

    /// <summary>
    /// Lê o peso da balança.
    /// </summary>
    /// <returns>Peso lido ou valores negativos para representar erros.</returns>
    public virtual decimal LePeso()
    {
        SolicitarPeso();
        Thread.Sleep(200);
        LeSerial();
        return UltimoPesoLido;
    }

    /// <summary>
    /// Lê os dados da porta serial.
    /// </summary>
    public virtual void LeSerial()
    {
        UltimoPesoLido = 0;
        UltimaResposta = "";

        try
        {
            UltimaResposta = Encoding.UTF8.GetString(device.Read());
            this.Log().Info($"Protocolo: {GetType().Name} - TX: [{UltimaResposta}]");

            UltimoPesoLido = InterpretarRepostaPeso();
        }
        catch
        {
            UltimoPesoLido = -9;
            throw;
        }

        this.Log().Info($"Protocolo: {GetType().Name} - UltimoPesoLido: {UltimoPesoLido:N6} - Resposta: [{UltimaResposta}]");
    }

    /// <summary>
    /// Envia comando padrão para solicitar o peso.
    /// As classes filhas podem sobrescrever se necessário.
    /// </summary>
    protected virtual void SolicitarPeso()
    {
        this.Log().Info($"Protocolo: {GetType().Name} - TX: [0x05]");
        device.Limpar();
        device.Write([0x05]);
    }

    /// <summary>
    /// Aguarda a resposta do peso, reenviando a solicitação se necessário.
    /// </summary>
    /// <param name="aReenviarSolicitarPeso">Indica se deve reenviar a solicitação de peso.</param>
    /// <returns>Peso lido ou -1 em caso de falha.</returns>
    protected decimal AguardarRespostaPeso(bool aReenviarSolicitarPeso)
    {
        var ret = -1M;
        var wFinal = DateTime.Now.AddSeconds(3);
        while (ret == -1 && wFinal > DateTime.Now)
        {
            if (aReenviarSolicitarPeso)
            {
                SolicitarPeso();
                Thread.Sleep(200);
            }

            LeSerial();
            ret = UltimoPesoLido;
        }

        return ret;
    }

    /// <summary>
    /// Interpreta a resposta recebida da serial e converte em peso.
    /// </summary>
    /// <returns>Peso lido.</returns>
    protected abstract decimal InterpretarRepostaPeso();

    #endregion Methods
}