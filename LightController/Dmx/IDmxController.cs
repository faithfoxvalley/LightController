using System;
using System.Text;

namespace LightController.Dmx;

public interface IDmxController : IDisposable
{
    bool IsOpen { get; }

    string Name { get; }

    void SetChannel(int channel, byte value);
    void SetChannels(int startAddress, byte[] data);
    void WriteData();
    void WriteDebugInfo(StringBuilder sb, int columns);
    bool TryReadDmxFrame(out byte[] data);

}
