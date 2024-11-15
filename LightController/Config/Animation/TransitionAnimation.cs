namespace LightController.Config.Animation
{
    public class TransitionAnimation
    {
        public double Length
        {
            get => length; 
            set
            {
                length = value;
                Init();
            }
        }
        public AnimationOrder Order
        {
            get => order;
            set
            {
                order = value;
                Init();
            }
        }

        private double stepLength = 0;
        private double length = 0;
        private AnimationOrder order = new AnimationOrder();

        public TransitionAnimation()
        {

        }

        public TransitionAnimation(double length, AnimationOrder order)
        {
            this.length = length;
            this.order = order;
            Init();
        }

        private void Init()
        {
            int count = Order.Count;
            if (count == 0)
                stepLength = Length;
            else
                stepLength = Length / count;
        }

        public void GetMixDetails(int fixtureId, out double mixLength, out double mixDelay)
        {
            mixLength = length;
            mixDelay = 0;

            if (Order.Count <= 1)
                return;

            if (Order.TryGetStepForFixture(fixtureId, out int step))
            {
                mixLength = stepLength;
                mixDelay = step * stepLength;
            }
        }
    }
}
