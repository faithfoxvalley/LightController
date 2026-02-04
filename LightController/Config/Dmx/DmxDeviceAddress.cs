namespace LightController.Config.Dmx;

public class DmxDeviceAddress
{
    public string Name { get; set; }
    public int StartAddress { get; set; }
    public int Count { get; set; } = 1;
    public int Universe { get; set; } = 1;

    public DmxDeviceAddress() { }
}
