using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer( typeof( Timer ) )]
public class TimerPropertyDrawer : PropertyDrawer {

	public override void OnGUI ( Rect position, SerializedProperty property, GUIContent label ) {

		EditorGUI.BeginProperty( position, label, property );
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		position = EditorGUI.PrefixLabel( position, label );
		EditorGUI.PropertyField( position, property.FindPropertyRelative( "_duration" ), GUIContent.none );

		EditorGUI.indentLevel = indent;
		EditorGUI.EndProperty();
	}
}

namespace Framework {

	[CustomPropertyDrawer( typeof( Output<bool> ) )]
	public class OutputPropertyDrawer : PropertyDrawer {

		public override void OnGUI ( Rect position, SerializedProperty property, GUIContent label ) {

			EditorGUI.BeginProperty( position, label, property );
			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			position = EditorGUI.PrefixLabel( position, label );
			EditorGUI.PropertyField( position, property.FindPropertyRelative( "value" ), GUIContent.none );

			EditorGUI.indentLevel = indent;
			EditorGUI.EndProperty();
		}
	}
}