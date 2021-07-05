using System;

namespace Tom.PropertyGroups.Runtime
{
    /// <summary>
    /// Attribute used to put a specified field into a toggleable Property Group.
    /// Created by Tomasz Galka | E-Mail: tommy.galk@gmail.com | Github: GomysTalka
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class PropertyGroupAttribute : Attribute
    {
        public string GroupLabel { get; private set; }

        public PropertyGroupAttribute(string groupLabel) => GroupLabel = groupLabel;
    }
}
