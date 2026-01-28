using System;
using System.Text;

namespace LightController.Dmx;

internal class NullDmxController : IDmxController
{
    private readonly byte[] buffer = new byte[513];

    public bool IsOpen => true;

    public void Dispose()
    {
    }

    public void SetChannel(int channel, byte value)
    {

    }

    public void SetChannels(int startChannel, byte[] values)
    {

        int endChannel = startChannel + values.Length;

        if (startChannel < 1 || endChannel > 512)
        {
            throw new ArgumentOutOfRangeException(nameof(startChannel), "Start channel number must be between 1 and 512.");
        }

        Buffer.BlockCopy(values, 0, buffer, startChannel, values.Length);
    }

    public bool TryReadDmxFrame(out byte[] data)
    {
        data = null;
        return false;
    }

    public void WriteData()
    {

    }

    public void WriteDebugInfo(StringBuilder sb, int columns)
    {
        sb.Append("___|_");
        for (int i = 0; i < columns; i++)
        {
            if (i < 100)
                sb.Append('_');
            if (i < 10)
                sb.Append('_');
            sb.Append(i).Append('_');
        }

        int column = 0;
        for (int i = 1; i < 513; i++)
        {
            int dmxChannel = i;
            if (column % columns == 0)
            {
                sb.AppendLine();
                if (dmxChannel < 100)
                    sb.Append(' ');
                if (dmxChannel < 10)
                    sb.Append(' ');
                sb.Append(dmxChannel).Append("| ");
            }

            byte b = buffer[i];
            if (b < 100)
                sb.Append(' ');
            if (b < 10)
                sb.Append(' ');
            sb.Append(b).Append(' ');

            column++;
        }
    }
}
