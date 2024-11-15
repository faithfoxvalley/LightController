using System.Text;

namespace LightController.Dmx
{
    public interface IDmxController
    {
        bool IsOpen { get; }

        void SetChannel(int channel, byte value);
        void SetChannels(int startAddress, byte[] data);
        void WriteData();
        void WriteDebugInfo(StringBuilder sb, int columns);
    }
}
