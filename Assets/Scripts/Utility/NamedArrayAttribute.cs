using UnityEngine;
using System;

public class NamedArrayAttribute : PropertyAttribute
{
    public readonly Type enumType;
    public readonly int[] names;

    public NamedArrayAttribute(Type enumType, int[] names) { this.enumType = enumType; this.names = names; }
}
