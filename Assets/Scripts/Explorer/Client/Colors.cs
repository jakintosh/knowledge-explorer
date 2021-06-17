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
		Action
	}

	public class Colors : MonoBehaviour {

		// events
		public UnityEvent<ColorMode> OnColorModeChanged = new UnityEvent<ColorMode>();

		// properties
		public UnityColor Foreground => _foreground.ColorForMode( _colorMode );
		public UnityColor Secondary => _secondary.ColorForMode( _colorMode );
		public UnityColor Tertiaty => _tertiaty.ColorForMode( _colorMode );
		public UnityColor Quaternary => _quaternary.ColorForMode( _colorMode );
		public UnityColor Background => _background.ColorForMode( _colorMode );
		public UnityColor Background2 => _background2.ColorForMode( _colorMode );
		public UnityColor Link => _link.ColorForMode( _colorMode );
		public UnityColor Action => _action.ColorForMode( _colorMode );


		// methods
		public UnityColor GetDefinedColor ( DefinedColors color )
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

		public void SetColorMode ( ColorMode colorMode ) {

			_colorMode = colorMode;
			OnColorModeChanged?.Invoke( _colorMode );
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
	}
}