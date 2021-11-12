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
//	     		    Copyright (c) 2016 Projeto OpenAC .Net
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

namespace OpenAC.Net.Balanca
{
    public sealed class OpenBal : OpenComponent
    {
        #region Fields

        private CancellationTokenSource cancelamento;
        private OpenDeviceStream device;
        private ProtocoloBase bal;

        #endregion Fields

        #region Eventos

        public event EventHandler<BalancaEventArgs> AoLerPeso;

        #endregion Eventos

        #region Properties

        /// <summary>
        /// Configurações da classe.
        /// </summary>
        public OpenBalConfig Config { get; private set; }

        /// <summary>
        /// Obtem se esta ou não conectado na balança.
        /// </summary>
        public bool IsConectado => device != null && device.Conectado;

        #endregion Properties

        #region Methods

        /// <summary>
        /// Metodo para conectar na balança.
        /// </summary>
        /// <exception cref="OpenException"></exception>
        public void Conectar()
        {
            if (IsConectado) throw new OpenException("A porta já está aberta");
            if (!Enum.IsDefined(typeof(ProtocoloBalanca), Config.Protocolo)) throw new OpenException(@"Protocolo não suportado.");

            device = OpenDeviceManager.GetCommunication(Config);
            device.Open();

            switch (Config.Protocolo)
            {
                case ProtocoloBalanca.Toledo:
                    bal = new ProtocoloToledo(device);
                    break;

                case ProtocoloBalanca.Filizola:
                    bal = new ProtocoloFilizola(device);
                    break;
            }

            cancelamento = new CancellationTokenSource();
            Monitorar();
        }

        /// <summary>
        /// Metodo para desconectar da balança.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public void Desconectar()
        {
            if (!IsConectado) throw new OpenException("A porta não está aberta");

            cancelamento?.Cancel();
            device?.Close();
            device?.Dispose();
            device = null;
            bal = null;
        }

        /// <summary>
        /// Solicita a leitura do peso a balança.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public decimal LerPeso()
        {
            if (device == null) throw new OpenException("A conexão não esta ativa.");

            var monitorando = Config.IsMonitorar;

            try
            {
                Config.IsMonitorar = false;
                bal.LePeso();
                AoLerPeso?.Raise(this, new BalancaEventArgs(bal.UltimaResposta, bal.UltimoPesoLido));
            }
            catch (Exception ex)
            {
                AoLerPeso?.Raise(this, new BalancaEventArgs(bal.UltimaResposta, ex));
            }
            finally
            {
                Config.IsMonitorar = monitorando;
            }

            return bal.UltimoPesoLido;
        }

        private async void Monitorar()
        {
            await Task.Run(async () =>
            {
                while (!cancelamento.IsCancellationRequested)
                {
                    //Não o while mesmo com o IsMonitorar false, porque pode ser uma para momentânea para uma requisição manual
                    if (!Config.IsMonitorar) continue;

                    try
                    {
                        bal.LeSerial();
                        AoLerPeso?.Raise(this, new BalancaEventArgs(bal.UltimaResposta, bal.UltimoPesoLido));
                    }
                    catch
                    {
                        //Não é necessário o tratamento no monitoramento, o LerPeso() faz o tratamento e lança a excessão tratada ao usuário
                    }
                    finally
                    {
                        await Task.Delay(Config.DelayMonitoramento);
                    }
                }
            }, cancelamento.Token);
        }

        /// <inheritdoc />
        protected override void OnInitialize()
        {
            Config = new OpenBalConfig(this);
        }

        /// <inheritdoc />
        protected override void OnDisposing()
        {
            //Para o monitoramento
            cancelamento?.Cancel();

            //Libera a porta serial
            if (!IsConectado) return;

            Desconectar();
        }

        #endregion Methods
    }
}