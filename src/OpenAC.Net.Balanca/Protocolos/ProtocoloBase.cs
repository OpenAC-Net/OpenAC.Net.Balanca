using System;
using System.Threading;
using OpenAC.Net.Core.Logging;
using OpenAC.Net.Devices;

namespace OpenAC.Net.Balanca
{
    public abstract class ProtocoloBase : IOpenLog
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