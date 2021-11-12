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

using System.Threading;
using OpenAC.Net.Core.Logging;
using OpenAC.Net.Devices;

namespace OpenAC.Net.Balanca
{
    internal abstract class ProtocoloBase : IOpenLog
    {
        #region Fields

        private readonly OpenDeviceStream device;

        #endregion Fields

        #region Constructors

        /// <summary>
        ///
        /// </summary>
        /// <param name="device"></param>
        protected ProtocoloBase(OpenDeviceStream device)
        {
            this.device = device;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Ultima resposta recebida.
        /// </summary>
        public string UltimaResposta { get; protected set; }

        /// <summary>
        /// Ultimo peso lido.
        /// </summary>
        public decimal UltimoPesoLido { get; protected set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Função para ler o peso da balança
        /// </summary>
        /// <returns>Peso lido ou valores negativos para representarem erros.</returns>
        public virtual decimal LePeso()
        {
            SolicitarPeso();
            Thread.Sleep(200);
            LeSerial();
            return UltimoPesoLido;
        }

        /// <summary>
        /// Envia comando Padrão para solicitar Peso.
        /// As classes filhas podem reescrever se necessário.
        /// </summary>
        protected virtual void SolicitarPeso()
        {
            this.Log().Info($"Protocolo: {GetType().Name} - TX: [0x05]");
            device.Limpar();
            device.Write(new byte[] { 0x05 });
        }

        /// <summary>
        /// Le os dados da porta serial.
        /// </summary>
        internal virtual void LeSerial()
        {
            UltimoPesoLido = 0;
            UltimaResposta = "";

            try
            {
                UltimaResposta = device.ReadString();
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
        /// Le a resposta que veio da serial e transforma em peso.
        /// </summary>
        /// <returns></returns>
        protected abstract decimal InterpretarRepostaPeso();

        #endregion Methods
    }
}