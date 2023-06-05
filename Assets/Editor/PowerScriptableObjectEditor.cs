using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PowerScriptableObject))]
public class PowerScriptableObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PowerScriptableObject powerScriptableObject = (PowerScriptableObject)target;

        // Draw the default inspector
        base.OnInspectorGUI();

        // Backup the GUI state
        bool previousGUIState = GUI.enabled;

        if (powerScriptableObject.onHitVelocitySetActive)
        {
            GUI.enabled = true;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onHitVelocitySetMagnitude"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onHitVelocitySetDirectionDeg"), true);
        }
        else
        {
            GUI.enabled = false;
            serializedObject.FindProperty("onHitVelocitySetMagnitude").floatValue = 0f;
            serializedObject.FindProperty("onHitVelocitySetDirectionDeg").floatValue = 0f;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onHitVelocitySetMagnitude"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onHitVelocitySetDirectionDeg"), true);
            //GUI.enabled = guiEnabled && hasValueProp.boolValue;
            //EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("v"), GUIContent.none);
            //GUI.enabled = guiEnabled;
        }

        // Restore the GUI state
        GUI.enabled = previousGUIState;

        serializedObject.ApplyModifiedProperties();
    }
}
