using UnityEngine;
using UnityEditor;
using System;
 
[CustomPropertyDrawer(typeof(NamedArrayAttribute))]
public class NamedArrayDrawer : PropertyDrawer
{
    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        try
        {
            int pos = int.Parse(property.propertyPath.Split('[', ']')[1]);
            //EditorGUI.PropertyField(rect, property, new GUIContent("poggy"));
            EditorGUI.PropertyField(rect, property, new GUIContent(Enum.GetName(((NamedArrayAttribute)attribute).enumType, ((NamedArrayAttribute)attribute).names[pos])));
        }
        catch
        {
            EditorGUI.PropertyField(rect, property, label);
        }
    }
}
