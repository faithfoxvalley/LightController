namespace LightController.Pro.Packet
{
    public struct TransportLayerStatus
    {
        public bool is_playing;
        public StringValue uuid;
        public string name;
        public string artist;
        public bool audio_only;
        public float duration;

        public struct StringValue
        {
            public string @string;

            public static implicit operator string(StringValue s) => s.@string;
        }
    }
}
