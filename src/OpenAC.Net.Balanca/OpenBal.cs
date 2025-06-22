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
using System.Threading;
using System.Threading.Tasks;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.Devices;

namespace OpenAC.Net.Balanca;

/// <summary>
/// Classe base abstrata para comunicação com balanças.
/// </summary>
public abstract class OpenBal : OpenDisposable
{
    #region Fields

    /// <summary>
    /// Protocolo de comunicação utilizado.
    /// </summary>
    private ProtocoloBalanca protocolo;

    /// <summary>
    /// Token para cancelamento do monitoramento.
    /// </summary>
    private CancellationTokenSource cancelamento;

    /// <summary>
    /// Stream do dispositivo conectado.
    /// </summary>
    private OpenDeviceStream device;

    /// <summary>
    /// Instância do protocolo base utilizado.
    /// </summary>
    private ProtocoloBase bal;

    #endregion Fields

    #region Eventos

    /// <summary>
    /// Evento lançado ao ler o peso.
    /// </summary>
    public event EventHandler<BalancaEventArgs> AoLerPeso;

    #endregion Eventos

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="OpenBal"/>.
    /// </summary>
    /// <param name="device">Configuração do dispositivo.</param>
    internal OpenBal(IDeviceConfig device)
    {
        Protocolo = ProtocoloBalanca.Toledo;
        Device = device;
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Configuração do dispositivo.
    /// </summary>
    public IDeviceConfig Device { get; }

    /// <summary>
    /// Protocolo de comunicação utilizado.
    /// </summary>
    public ProtocoloBalanca Protocolo
    {
        get => protocolo;
        set
        {
            if (Conectado) throw new OpenException("Não pode mudar o protocolo quando esta conectado.");
            protocolo = value;
        }
    }

    /// <summary>
    /// Indica se o monitoramento está ativo.
    /// </summary>
    public bool IsMonitorar { get; set; }

    /// <summary>
    /// Delay entre as leituras de monitoramento (em milissegundos).
    /// </summary>
    public int DelayMonitoramento { get; set; }

    /// <summary>
    /// Obtém se está ou não conectado na balança.
    /// </summary>
    public bool Conectado => device != null && device.Conectado;

    #endregion Properties

    #region Methods

    /// <summary>
    /// Método para conectar na balança.
    /// </summary>
    /// <exception cref="OpenException">Lançada se a porta já estiver aberta ou protocolo não suportado.</exception>
    public void Conectar()
    {
        if (Conectado) throw new OpenException("A porta já está aberta");
        if (!Enum.IsDefined(typeof(ProtocoloBalanca), Protocolo)) throw new OpenException(@"Protocolo não suportado.");

        // Controle de porta não serve para balança.
        Device.ControlePorta = false;

        device = OpenDeviceFactory.Create(Device);
        device.Open();

        switch (Protocolo)
        {
            case ProtocoloBalanca.Toledo:
                bal = new ProtocoloToledo(device);
                break;

            case ProtocoloBalanca.Filizola:
                bal = new ProtocoloFilizola(device);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        cancelamento = new CancellationTokenSource();
        Monitorar();
    }

    /// <summary>
    /// Método para desconectar da balança.
    /// </summary>
    /// <exception cref="OpenException">Lançada se a porta não estiver aberta.</exception>
    public void Desconectar()
    {
        if (!Conectado) throw new OpenException("A porta não está aberta");

        cancelamento?.Cancel();
        device?.Close();
        device?.Dispose();
        device = null;
        bal = null;
    }

    /// <summary>
    /// Solicita a leitura do peso à balança.
    /// </summary>
    /// <returns>Peso lido.</returns>
    /// <exception cref="OpenException">Lançada se a conexão não estiver ativa.</exception>
    public decimal LerPeso()
    {
        if (device == null) throw new OpenException("A conexão não esta ativa.");

        var monitorando = IsMonitorar;

        try
        {
            IsMonitorar = false;
            bal.LePeso();
            AoLerPeso?.Raise(this, new BalancaEventArgs(bal.UltimaResposta, bal.UltimoPesoLido));
        }
        catch (Exception ex)
        {
            AoLerPeso?.Raise(this, new BalancaEventArgs(bal.UltimaResposta, ex));
        }
        finally
        {
            IsMonitorar = monitorando;
        }

        return bal.UltimoPesoLido;
    }

    /// <summary>
    /// Inicia o monitoramento assíncrono da balança.
    /// </summary>
    private async void Monitorar()
    {
        await Task.Run(async () =>
        {
            while (!cancelamento.IsCancellationRequested)
            {
                //Não o while mesmo com o IsMonitorar false, porque pode ser uma para momentânea para uma requisição manual
                if (!IsMonitorar) continue;

                try
                {
                    bal.LeSerial();
                    AoLerPeso?.Raise(this, new BalancaEventArgs(bal.UltimaResposta, bal.UltimoPesoLido));
                }
                catch (Exception ex)
                {
                    AoLerPeso?.Raise(this, new BalancaEventArgs(bal.UltimaResposta, ex));
                }
                finally
                {
                    await Task.Delay(DelayMonitoramento);
                }
            }
        }, cancelamento.Token);
    }

    /// <inheritdoc />
    protected override void DisposeManaged()
    {
        if (Conectado)
            Desconectar();
    }

    #endregion Methods
}