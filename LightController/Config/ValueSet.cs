using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightController.Config
{
    // Range of positive values
    public class ValueSet
    {
        private List<Range> ranges;

        public bool Empty => ranges.Count == 0;

        private ValueSet() 
        {
            ranges = new List<Range>();
        }

        public ValueSet(string values)
        {
            string[] strings = values.Split(',');
            ranges = new List<Range>(strings.Length);
            foreach (string s in strings)
            {
                string value = s.Trim();
                if (!string.IsNullOrWhiteSpace(s))
                    ranges.Add(new Range(value));
            }
        }

        public ValueSet(int start, int end)
        {
            ranges = new List<Range>();
            ranges.Add(new Range(start, end));
        }

        public bool GetOverlap(ValueSet other, out ValueSet result)
        {
            result = new ValueSet();

            foreach(Range myRange in ranges)
            {
                foreach(Range otherRange in other.ranges)
                {
                    if (myRange.GetOverlap(otherRange, out Range newRange))
                        result.ranges.Add(newRange);
                }
            }

            return !result.Empty;
        }

        public bool Contains(int value)
        {
            foreach (Range range in ranges)
            {
                if (range.Contains(value))
                    return true;
            }
            return false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ranges.Count; i++)
            {
                Range range = ranges[i];
                range.AppendString(sb);
                if (i < ranges.Count - 1)
                    sb.Append(',');
            }    
            return sb.ToString();
        }

        public IEnumerable<int> EnumerateValues()
        {
            foreach(Range range in ranges)
            {
                foreach (int i in range)
                    yield return i;
            }
        }

        public int Min()
        {
            int min = int.MaxValue;
            foreach (Range range in ranges)
            {
                if (range.Min < min)
                    min = range.Min;
            }
            return min;
        }

        public int Max()
        {
            int max = int.MinValue;
            foreach (Range range in ranges)
            {
                if (range.Max > max)
                    max = range.Max;
            }
            return max;
        }


        private class Range : IEnumerable<int>
        {
            private int start;
            private int end;
            private bool reversed = false;

            public int Min => start;
            public int Max => end;

            public Range(string range)
            {
                if(range.Contains('-'))
                {
                    string[] split = range.Split(new[] { '-' }, 2);
                    start = int.Parse(split[0].Trim());
                    end = int.Parse(split[1].Trim());
                    if(start > end)
                    {
                        int temp = start;
                        start = end;
                        end = temp;
                        reversed = true;
                    }
                }
                else
                {
                    start = int.Parse(range);
                    end = start;
                }
            }

            public Range(int start, int end)
            {
                this.start = start;
                this.end = end;
            }

            public bool Contains(int value)
            {
                return value >= start && value <= end;
            }

            public bool GetOverlap(Range other, out Range result)
            {
                result = null;
                if (other.end < this.start)
                    return false;
                if (other.start > this.end)
                    return false;
                int start = Math.Max(this.start, other.start);
                int end = Math.Min(this.end, other.end);
                result = new Range(start, end);
                return true;
            }

            public void AppendString(StringBuilder sb)
            {
                if (start == end)
                    sb.Append(start);
                else
                    sb.Append(start).Append('-').Append(end);
            }

            public override string ToString()
            {
                if (start == end)
                    return start.ToString();
                else
                    return $"{start}-{end}";
            }

            public IEnumerator<int> GetEnumerator()
            {
                var enumerable = Enumerable.Range(start, (end - start) + 1);
                if (reversed)
                    enumerable = enumerable.Reverse();
                return enumerable.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                var enumerable = Enumerable.Range(start, (end - start) + 1);
                if (reversed)
                    enumerable = enumerable.Reverse();
                return enumerable.GetEnumerator();
            }
        }
    }
}
