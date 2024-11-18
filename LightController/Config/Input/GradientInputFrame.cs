using LightController.Color;
using YamlDotNet.Serialization;

namespace LightController.Config.Input
{
    public class GradientInputFrame
    {
        private Percent location = new Percent(0);
        private readonly SerializableColorHSV color = new SerializableColorHSV(new ColorHSV(0, 1, 1));

        public GradientInputFrame()
        {

        }

        public GradientInputFrame(GradientInputFrame frame)
        {
            location = new Percent(frame.location);
            color = new SerializableColorHSV(frame.Color);
        }

        [YamlIgnore]
        public double Location
        {
            get => location.Value;
            set => location = new Percent(value);
        }

        [YamlMember(Alias = "Location")]
        public string LocationString
        {
            get => location.ToString();
            set => location = Percent.Parse(value, 0);
        }


        [YamlMember(Alias = "Hue")]
        public double Hue
        {
            get => color.Hue;
            set => color.Hue = value;
        }

        [YamlMember(Alias = "Saturation")]
        public string Saturation
        {
            get => color.Saturation;
            set => color.Saturation = value;
        }

        [YamlIgnore]
        public ColorHSV Color => color.Color;

        [YamlMember(Alias = "Intensity")]
        public string IntensityMode
        {
            get => color.Value;
            set => color.Value = value;
        }
    }
}
