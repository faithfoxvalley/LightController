using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightController.Config
{
    public class Animation
    {
        private List<ValueSet> animationOrder = new List<ValueSet>();

        public IEnumerable<ValueSet> AnimationOrder => animationOrder;

        public double Length
        {
            get => length;
            set
            {
                length = value;
                UpdateStepLength();
            }
        }
        private double length;
        private double stepLength;

        /// <summary>
        /// True = Length defines start to end
        /// False = Length defines start to start of last set
        /// </summary>
        public bool LengthIncludesLastSet 
        { 
            get
            {
                return lengthIncludesLastSet;
            }
            set
            {
                lengthIncludesLastSet = value;
                UpdateStepLength();
            }
        }
        private bool lengthIncludesLastSet = true;


        public Animation()
        {

        }

        public Animation(string animation)
        {
            if (!string.IsNullOrWhiteSpace(animation))
            {
                string[] args = animation.Split(';');
                foreach (string arg in args)
                {
                    if (!string.IsNullOrWhiteSpace(arg))
                    {
                        ValueSet set = new ValueSet(arg.Trim());
                        animationOrder.Add(set);
                    }
                }
            }
        }

        private void UpdateStepLength()
        {
            if (animationOrder.Count == 0)
                stepLength = 0;
            else if (lengthIncludesLastSet)
                stepLength = length / animationOrder.Count;
            else if (animationOrder.Count == 1)
                stepLength = 0;
            else
                stepLength = length / (animationOrder.Count - 1);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (ValueSet set in animationOrder)
                sb.Append(set).Append(';');
            if (sb.Length > 0)
                sb.Length--;
            return sb.ToString();
        }

        public double GetLength(int fixtureId)
        {
            if (animationOrder.Count == 0)
                return Length;
            if (animationOrder.Any(x => x.Contains(fixtureId)))
                return stepLength;
            return Length;
        }

        public double GetDelay(int fixtureId)
        {
            if (animationOrder.Count == 0)
                return 0;
            for (int i = 0; i < animationOrder.Count; i++)
            {
                ValueSet set = animationOrder[i];
                if (set.Contains(fixtureId))
                    return stepLength * i;
            }
            return 0;
        }

        public double GetDelayForSetIndex(int index)
        {
            if (index < 0 || index >= animationOrder.Count)
                return 0;
            return stepLength * index;
        }
    }
}
