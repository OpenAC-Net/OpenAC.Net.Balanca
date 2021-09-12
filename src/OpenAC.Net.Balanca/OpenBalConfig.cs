using OpenAC.Net.Core;
using OpenAC.Net.Devices;

namespace OpenAC.Net.Balanca
{
    public sealed class OpenBalConfig : OpenDeviceConfig
    {
        #region Fields

        private ProtocoloBalanca protocolo;
        private readonly OpenBal owner;

        #endregion Fields

        #region Constructors

        public OpenBalConfig(OpenBal bal)
        {
            owner = bal;
            protocolo = ProtocoloBalanca.Toledo;
        }

        #endregion Constructors

        #region Properties

        public ProtocoloBalanca Protocolo
        {
            get => protocolo;
            set
            {
                if (owner.IsConectado) throw new OpenException("Não pode mudar o protocolo quando esta conectado.");
                protocolo = value;
            }
        }

        public bool IsMonitorar { get; set; }

        public int DelayMonitoramento { get; set; }

        #endregion Properties
    }
}