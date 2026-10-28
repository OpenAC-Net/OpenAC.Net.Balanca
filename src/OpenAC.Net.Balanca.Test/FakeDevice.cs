using System.Collections.Concurrent;
using System.Text;
using OpenAC.Net.Devices;

namespace OpenAC.Net.Balanca.Test;

internal sealed class FakeDevice : OpenDeviceStream
{
    internal FakeDevice() : base(new SerialConfig { TimeOut = 100 }) { }
    internal ConcurrentQueue<byte[]> Respostas { get; } = new();
    internal List<byte[]> Envios { get; } = [];
    internal int Limpezas { get; private set; }
    internal int Leituras;
    internal bool Liberado { get; private set; }
    internal bool FalharAbertura { get; set; }
    internal bool FalharLeitura { get; set; }
    internal Action? AoEnviar { get; set; }
    protected override int Available => 0;
    protected override bool OpenInternal() => !FalharAbertura;
    protected override bool CloseInternal() => true;
    public override void Limpar() => Limpezas++;
    public override void Write(byte[] dados) { Envios.Add(dados); AoEnviar?.Invoke(); }
    public override byte[] Read()
    {
        Interlocked.Increment(ref Leituras);
        if (FalharLeitura) throw new IOException("Falha de leitura simulada.");
        return Respostas.TryDequeue(out var dados) ? dados : [];
    }
    internal void Enfileirar(string texto) => Respostas.Enqueue(Encoding.GetEncoding(28591).GetBytes(texto));
    protected override void DisposeManaged() { Liberado = true; base.DisposeManaged(); }
}

internal sealed class TestScale : OpenBal
{
    internal TestScale(FakeDevice device) : base(device.Config, _ => device) { }
    internal TestScale(Func<OpenDeviceStream> factory) : base(new SerialConfig(), _ => factory()) { }
}
