using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace SFramework.Configs.Runtime
{
    [Preserve]
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