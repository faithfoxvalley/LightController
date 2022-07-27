namespace LightController.Config
{
    public class ValueRange
    {
        public ValueRange() { }

        public ValueRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        public int Start { get; private set; }
        public int End { get; private set; }
    }
}
