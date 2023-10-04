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
                string[] steps = animation.Split(';');
                foreach (string step in steps)
                {
                    if (string.IsNullOrWhiteSpace(step))
                        continue;


                    if (step[step.Length - 1] == ')')
                    {
                        string groupStep = step.TrimStart();
                        int numStart = groupStep.IndexOf('(');
                        if(numStart > 0)
                        {
                            numStart++;
                            int numLength = groupStep.Length - (numStart + 1);
                            int groupCount = int.Parse(groupStep.Substring(numStart, numLength));
                            ValueSet groupSet = new ValueSet(groupStep.Substring(0, groupStep.Length - (numLength + 2)));
                            int i = 0;
                            ValueSet currentSet = new ValueSet();
                            animationOrder.Add(currentSet);
                            foreach(int id in groupSet.EnumerateValues())
                            {
                                if(i == groupCount)
                                {
                                    currentSet = new ValueSet();
                                    animationOrder.Add(currentSet);
                                    i = 0;
                                }
                                currentSet.AddValue(id);
                                i++;
                            }
                            continue;
                        }
                    }

                    string[] concurrentGroups = step.Split("|");
                    ValueSet[] groupSets = concurrentGroups
                        .Where(x => !string.IsNullOrWhiteSpace (x))
                        .Select(x => new ValueSet(x.Trim()))
                        .ToArray();
                    if (groupSets.Length == 0)
                        continue;
                    if(groupSets.Length == 1)
                    {
                        animationOrder.Add(groupSets[0]);
                        continue;
                    }

                    List<ValueSet> resultSets = new List<ValueSet>();
                    foreach (ValueSet group in groupSets)
                    {
                        int i = 0;
                        foreach(int id in group.EnumerateValues())
                        {
                            ValueSet resultSet;
                            if(i == resultSets.Count)
                            {
                                resultSet = new ValueSet();
                                resultSets.Add(resultSet);
                                animationOrder.Add(resultSet);
                            }
                            else
                            {
                                resultSet = resultSets[i];
                            }

                            resultSet.AddValue(id);

                            i++;
                        }
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
