namespace LightController.Pro.Packet;

public struct Presentation
{
    public Data presentation;

    public struct Data
    {
        public ItemId id;
        public bool has_timeline;
        public string presentation_path;
        public string destination;
    }
}
