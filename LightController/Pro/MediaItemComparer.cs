using System.Collections.Generic;

namespace LightController.Pro;

public class MediaItemComparer : IComparer<ProMediaItem>
{
    public int Compare(ProMediaItem x, ProMediaItem y)
    {
        if(x.Id.HasValue)
        {
            if (y.Id.HasValue)
            {
                if(x.Id.Value == y.Id.Value)
                {
                    if (x.Name == y.Name)
                        return 1;
                    return x.Name.CompareTo(y.Name);
                }
                return x.Id.Value.CompareTo(y.Id.Value);
            }
            return -1;
        }
        else
        {
            if(y.Id.HasValue)
                return 1;
            if (x.Name == y.Name)
                return 1;
            return x.Name.CompareTo(y.Name);
        }
    }
}
