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
    public abstract class OpenBal : OpenDisposable
    {
        #region Fields

        private ProtocoloBalanca protocolo;
        private CancellationTokenSource cancelamento;
        private OpenDeviceStream device;
        private ProtocoloBase bal;

        #endregion Fields

        #region Eventos

        /// <summary>
        /// Evento lançando ao ler o peso.
        /// </summary>
        public event EventHandler<BalancaEventArgs> AoLerPeso;

        #endregion Eventos

        #region Constructors

        internal OpenBal(IDeviceConfig device)
        {
            Protocolo = ProtocoloBalanca.Toledo;
            Device = device;
        }

        #endregion Constructors

        #region Properties

        public IDeviceConfig Device { get; }

        public ProtocoloBalanca Protocolo
        {
            get => protocolo;
            set
            {
                if (Conectado) throw new OpenException("Não pode mudar o protocolo quando esta conectado.");
                protocolo = value;
            }
        }

        public bool IsMonitorar { get; set; }

        public int DelayMonitoramento { get; set; }

        /// <summary>
        /// Obtem se esta ou não conectado na balança.
        /// </summary>
        public bool Conectado => device != null && device.Conectado;

        #endregion Properties

        #region Methods

        /// <summary>
        /// Metodo para conectar na balança.
        /// </summary>
        /// <exception cref="OpenException"></exception>
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
        /// Metodo para desconectar da balança.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
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
        /// Solicita a leitura do peso a balança.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public decimal LerPeso(int timeOut = 3000)
        {
            if (device == null) throw new OpenException("A conexão não esta ativa.");

            var monitorando = IsMonitorar;

            try
            {
                IsMonitorar = false;
                bal.LePeso(timeOut);
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
        /// <inheritdoc />
        protected override void DisposeManaged()
        {
            if (Conectado)
                Desconectar();
        }

        #endregion Methods
    }
}