using Jakintosh.Observable;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Explorer.View {

	public class ToggleControl : View {

		// ********** Public Interface **********

		public UnityEvent<bool> OnToggled = new UnityEvent<bool>();


		// ********** Private Interface **********

		[Header( "UI Control" )]
		[SerializeField] private Button _button;

		[Header( "UI Display" )]
		[SerializeField] private Image _offImage;
		[SerializeField] private Image _onImage;
		[SerializeField] private Image _selectionImage;
		[SerializeField] private Image _backgroundImage;

		[Header( "UI Assets" )]
		[SerializeField] private Color _backgroundColor;
		[SerializeField] private Color _foregroundColor;
		[SerializeField] private Color _accentColor;
		[SerializeField] private Sprite _offSprite;
		[SerializeField] private Sprite _onSprite;

		// static values
		private static int SELECTION_OFFSET = 10;
		private static Vector3 OFF_IMAGE_SCALE = new Vector3( 0.6f, 0.6f, 0.6f );
		private static Vector3 ON_IMAGE_SCALE = new Vector3( 1f, 1f, 1f );

		// model data
		private Observable<bool> _isOn;

		protected override void OnInitialize () {

			_backgroundImage.color = _backgroundColor;
			_offImage.sprite = _offSprite;
			_onImage.sprite = _onSprite;

			// init observables
			_isOn = new Observable<bool>(
				initialValue: false,
				onChange: isOn => {
					_selectionImage.rectTransform.localPosition = new Vector2( isOn ? SELECTION_OFFSET : -SELECTION_OFFSET, 0 );
					_offImage.rectTransform.localScale = !isOn ? ON_IMAGE_SCALE : OFF_IMAGE_SCALE;
					_onImage.rectTransform.localScale = isOn ? ON_IMAGE_SCALE : OFF_IMAGE_SCALE;
					_offImage.color = !isOn ? _backgroundColor : _accentColor;
					_onImage.color = isOn ? _backgroundColor : _foregroundColor;
					_selectionImage.color = isOn ? _accentColor : _foregroundColor;
					OnToggled?.Invoke( isOn );
				}
			);

			// sub to owned controls
			_button.onClick.AddListener( () => {
				_isOn.Set( !_isOn.Get() );
			} );
		}
		protected override void OnCleanup () { }
	}

}

