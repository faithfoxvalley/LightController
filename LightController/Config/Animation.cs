using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Config
{
    public class Animation
    {
        private List<ValueSet> animationOrder = new List<ValueSet>();

        public double Length
        {
            get => length;
            set
            {
                length = value;
                if(animationOrder.Count > 0)
                    stepLength = value / animationOrder.Count;
            }
        }
        private double length;
        private double stepLength;

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
    }
}
