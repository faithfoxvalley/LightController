namespace LightController.Config.Input
{
    public abstract class InputBase
    {
        public ValueRange Channels { get; set; }

        protected InputBase() { }

        public InputBase(ValueRange channels)
        {
            Channels = channels;
        }
    }
}
