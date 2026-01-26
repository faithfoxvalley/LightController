using System;

namespace LightController.Config;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class YamlTagAttribute : Attribute
{
    public string Tag { get; }

    public YamlTagAttribute(string tag)
    {
        this.Tag = tag;
    }
}
