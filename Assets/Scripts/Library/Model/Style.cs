using UnityEngine;

namespace Library.Model {

	[CreateAssetMenu( menuName = "Data/Style", fileName = "New Style" )]
	public class Style : ScriptableObject {

		public Color Foreground;
		public Color Background;
		public Color Accent;

		public static Style Default {
			get {
				var style = CreateInstance<Style>();
				style.Accent = Color.blue;
				style.Foreground = Color.white;
				style.Background = Color.black;
				return style;
			}
		}
	}
}