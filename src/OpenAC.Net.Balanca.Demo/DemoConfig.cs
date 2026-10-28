using System.IO.Ports;

namespace OpenAC.Net.Balanca.Demo;

internal sealed class DemoConfig
{
    public ProtocoloBalanca Protocolo { get; set; }
    public bool Tcp { get; set; }
    public string Porta { get; set; } = OperatingSystem.IsWindows() ? "COM1" : "/dev/ttyUSB0";
    public int Baud { get; set; } = 9600;
    public int DataBits { get; set; } = 8;
    public Parity Parity { get; set; }
    public StopBits StopBits { get; set; } = StopBits.One;
    public Handshake Handshake { get; set; }
    public string Host { get; set; } = "127.0.0.1";
    public int TcpPort { get; set; } = 9000;
    public int Timeout { get; set; } = 3000;
    public int Intervalo { get; set; } = 200;
    public int Posicao { get; set; }
    public int Tamanho { get; set; }
}
