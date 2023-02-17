using System;
using UnityEngine;

namespace SFramework.Repositories.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SFIdAttribute : PropertyAttribute
    {
        public SFIdAttribute(Type type, int indent = -1)
        {
            Type = type;
            Indent = indent;
        }
        
        public readonly Type Type;
        public readonly int Indent;
    }
}