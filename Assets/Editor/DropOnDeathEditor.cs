#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DropOnDeath.Drop))]
public class DropDrawer : PropertyDrawer
{
    public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
    {
        var prefabProp = prop.FindPropertyRelative("prefab");
        string name = prefabProp.objectReferenceValue != null
            ? prefabProp.objectReferenceValue.name
            : label.text;

        EditorGUI.PropertyField(pos, prop, new GUIContent(name), true);
    }

    public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) =>
        EditorGUI.GetPropertyHeight(prop, label, true);
}
#endif
