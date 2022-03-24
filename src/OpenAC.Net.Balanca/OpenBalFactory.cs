using System;
using OpenAC.Net.Core;
using OpenAC.Net.Devices;

namespace OpenAC.Net.Balanca
{
    public static class OpenBalFactory
    {
        public static OpenBal<TConfig> Create<TConfig>(ProtocoloBalanca protocolo, TConfig config) where TConfig : IDeviceConfig
        {
            return new OpenBal<TConfig>(protocolo, config);
        }

        public static OpenBal<SerialConfig> CreateSerial(ProtocoloBalanca protocolo, Action<OpenBal<SerialConfig>> config = null)
        {
            var serialConfig = new SerialConfig();
            var ret = new OpenBal<SerialConfig>(protocolo, serialConfig)
            {
                Encoder = OpenEncoding.IBM850
            };

            config?.Invoke(ret);
            return ret;
        }

        public static OpenBal<TCPConfig> CreateTCP(ProtocoloBalanca protocolo, Action<OpenBal<TCPConfig>> config = null)
        {
            var tcpConfig = new TCPConfig("", 9100);
            var ret = new OpenBal<TCPConfig>(protocolo, tcpConfig)
            {
                Encoder = OpenEncoding.IBM850
            };

            config?.Invoke(ret);
            return ret;
        }
    }
}