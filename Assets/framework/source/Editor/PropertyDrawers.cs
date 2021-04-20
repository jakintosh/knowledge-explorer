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

[CustomPropertyDrawer( typeof( Float3 ) )]
public class Float3PropertyDrawer : PropertyDrawer {

	public override void OnGUI ( Rect position, SerializedProperty property, GUIContent label ) {

		EditorGUI.BeginProperty( position, label, property );
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		position = EditorGUI.PrefixLabel( position, label );

		var spacing = 4f;
		var x = position.x;
		var y = position.y;
		var w = ( position.width - spacing * 2 ) / 3;
		var h = position.height;

		var lw = EditorGUIUtility.labelWidth;
		EditorGUIUtility.labelWidth = 12f;
		var properties = new System.Collections.Generic.List<string> { "x", "y", "z" };
		foreach ( var prop in properties ) {

			EditorGUI.PropertyField(
				position: new Rect( x: x, y: y, width: w, height: h ),
				property: property.FindPropertyRelative( prop ),
				label: new GUIContent( prop.ToUpper() )
			);
			x += w + spacing;
		}

		EditorGUIUtility.labelWidth = lw;
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