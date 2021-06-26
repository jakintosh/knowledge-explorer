using System;
using UnityEngine;
using UnityEngine.Events;

using UnityColor = UnityEngine.Color;

namespace Explorer.Client {

	public enum ColorMode {
		Light,
		Dark
	}

	[Serializable]
	public struct Color {

		public UnityColor Default => Light;
		[SerializeField] public UnityColor Light;
		[SerializeField] public UnityColor Dark;

		public UnityColor ColorForMode ( ColorMode colorMode )
			=> colorMode switch {
				ColorMode.Light => Light,
				ColorMode.Dark => Dark,
				_ => Default
			};
	}

	public enum DefinedColors {
		Foreground,
		Secondary,
		Tertiaty,
		Quaternary,
		Background,
		Background2,
		Link,
		Action,
		Error
	}

	public class Colors : MonoBehaviour {

		private static Colors _instance;
		public static Colors Instance {
			get {
				if ( _instance == null ) {
					_instance = GameObject.FindObjectOfType<Colors>();
				}
				return _instance;
			}
		}
		private void Awake () {
			if ( _instance != null && _instance != this ) {
				Destroy( this.gameObject );
			} else {
				_instance = this;
			}
		}

		// events
		public UnityEvent<ColorMode> OnColorModeChanged = new UnityEvent<ColorMode>();

		// properties
		public static UnityColor Foreground => Instance._foreground.ColorForMode( Instance._colorMode );
		public static UnityColor Secondary => Instance._secondary.ColorForMode( Instance._colorMode );
		public static UnityColor Tertiaty => Instance._tertiaty.ColorForMode( Instance._colorMode );
		public static UnityColor Quaternary => Instance._quaternary.ColorForMode( Instance._colorMode );
		public static UnityColor Background => Instance._background.ColorForMode( Instance._colorMode );
		public static UnityColor Background2 => Instance._background2.ColorForMode( Instance._colorMode );
		public static UnityColor Link => Instance._link.ColorForMode( Instance._colorMode );
		public static UnityColor Action => Instance._action.ColorForMode( Instance._colorMode );
		public static UnityColor Error => Instance._error.ColorForMode( Instance._colorMode );


		// methods
		public static UnityColor GetDefinedColor ( DefinedColors color )
			=> color switch {
				DefinedColors.Foreground => Foreground,
				DefinedColors.Secondary => Secondary,
				DefinedColors.Tertiaty => Tertiaty,
				DefinedColors.Quaternary => Quaternary,
				DefinedColors.Background => Background,
				DefinedColors.Background2 => Background2,
				DefinedColors.Link => Link,
				DefinedColors.Action => Action,
				_ => throw new IndexOutOfRangeException()
			};

		public static void SetColorMode ( ColorMode colorMode ) {

			Instance._colorMode = colorMode;
			Instance.OnColorModeChanged?.Invoke( Instance._colorMode );
		}

		// data
		[SerializeField] private ColorMode _colorMode;

		[SerializeField] private Color _foreground;
		[SerializeField] private Color _secondary;
		[SerializeField] private Color _tertiaty;
		[SerializeField] private Color _quaternary;

		[SerializeField] private Color _background;
		[SerializeField] private Color _background2;

		[SerializeField] private Color _link;
		[SerializeField] private Color _action;
		[SerializeField] private Color _error;
	}
}