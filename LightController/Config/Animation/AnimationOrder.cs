using System.Collections.Generic;
using System.Linq;

namespace LightController.Config.Animation;

public class AnimationOrder
{
    private List<ValueSet> animationOrder = new List<ValueSet>();
    private string animationString;

    public IEnumerable<ValueSet> Values => animationOrder;

    public int Count => animationOrder.Count;

    public AnimationOrder()
    {

    }

    public AnimationOrder(string animation)
    {
        if (string.IsNullOrWhiteSpace(animation))
        {
            animationString = null;
            return;
        }
        animationString = animation;
        string[] steps = animation.Split(';');
        foreach (string step in steps)
        {
            if (string.IsNullOrWhiteSpace(step))
                continue;

            if (step[step.Length - 1] == ')')
            {
                string groupStep = step.TrimStart();
                int numStart = groupStep.IndexOf('(');
                if (numStart > 0)
                {
                    numStart++;
                    int numLength = groupStep.Length - (numStart + 1);
                    int groupCount = int.Parse(groupStep.Substring(numStart, numLength));
                    if (groupCount > 0)
                    {
                        ValueSet groupSet = new ValueSet(groupStep.Substring(0, groupStep.Length - (numLength + 2)));
                        int i = 0;
                        ValueSet currentSet = new ValueSet();
                        animationOrder.Add(currentSet);
                        foreach (int id in groupSet.EnumerateValues())
                        {
                            if (i == groupCount)
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
            }

            string[] concurrentGroups = step.Split("|");
            ValueSet[] groupSets = concurrentGroups
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => new ValueSet(x.Trim()))
                .ToArray();
            if (groupSets.Length == 0)
                continue;
            if (groupSets.Length == 1)
            {
                animationOrder.Add(groupSets[0]);
                continue;
            }

            List<ValueSet> resultSets = new List<ValueSet>();
            foreach (ValueSet group in groupSets)
            {
                int i = 0;
                foreach (int id in group.EnumerateValues())
                {
                    ValueSet resultSet;
                    if (i == resultSets.Count)
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

    public override string ToString()
    {
        return animationString;
    }

    public bool TryGetStepForFixture(int fixtureId, out int stepIndex)
    {
        stepIndex = 0;
        for (int i = 0; i < animationOrder.Count; i++)
        {
            ValueSet set = animationOrder[i];
            if (set.Contains(fixtureId))
            {
                stepIndex = i;
                return true;
            }
        }
        return false;
    }

}
