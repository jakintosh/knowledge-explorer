using Explorer.Model.Presence;
using Framework;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace Explorer.View {

	public class PresenceControl : View {

		// ********** Public Interface **********

		// // data types
		// public enum Contexts {
		// 	Floating,
		// 	Focused
		// }
		// public enum Sizes {
		// 	Expanded,
		// 	Compact
		// }

		// events
		public UnityEvent OnClosed = new UnityEvent();
		public UnityEvent<Sizes> OnSizeChanged = new UnityEvent<Sizes>();
		public UnityEvent<Contexts> OnContextChanged = new UnityEvent<Contexts>();

		// properties
		public Sizes Size => _size.Get();
		public Contexts Context => _context.Get();

		// methods
		public void SetInteractive ( bool close, bool size, bool context ) {

			_closeButton.interactable = close;
			_sizeButton.interactable = size;
			_contextButton.interactable = context;
		}
		public void SetEnabled ( bool close, bool size, bool context ) {

			_closeButton.gameObject.SetActive( close );
			_sizeButton.gameObject.SetActive( size );
			_contextButton.gameObject.SetActive( context );
		}
		public void Force ( bool? close = null, Sizes? size = null, Contexts? context = null ) {

			if ( close.HasValue && close.Value ) { Close(); }
			if ( size.HasValue ) { _size.Set( size.Value ); }
			if ( context.HasValue ) { _context.Set( context.Value ); }
		}


		// ********** Private Interface **********

		[Header( "UI Control" )]
		[SerializeField] private Button _closeButton;
		[SerializeField] private Button _sizeButton;
		[SerializeField] private Button _contextButton;

		[Header( "UI Display" )]
		[SerializeField] private Image _sizeButtonImage;
		[SerializeField] private Image _contextButtonImage;
		[Space( 8 )]
		[SerializeField] private Sprite _compactSizeSprite;
		[SerializeField] private Sprite _expandedSizeSprite;
		[Space( 8 )]
		[SerializeField] private Sprite _floatingContextSprite;
		[SerializeField] private Sprite _focusedContextSprite;


		// model data
		private Observable<Sizes> _size;
		private Observable<Contexts> _context;


		// lifecycle
		protected override void Init () {

			// init observables
			_size = new Observable<Sizes>(
				initialValue: Sizes.Expanded,
				onChange: size => {
					_sizeButtonImage.sprite = size switch {
						Sizes.Compact => _expandedSizeSprite,
						Sizes.Expanded => _compactSizeSprite,
						_ => throw new ArgumentOutOfRangeException( nameof( size ) )
					};
					OnSizeChanged?.Invoke( size );
				}
			);
			_context = new Observable<Contexts>(
				initialValue: Contexts.Floating,
				onChange: context => {
					_contextButtonImage.sprite = context switch {
						Contexts.Floating => _focusedContextSprite,
						Contexts.Focused => _floatingContextSprite,
						_ => throw new ArgumentOutOfRangeException( nameof( context ) )
					};
					OnContextChanged?.Invoke( context );
				}
			);

			// connect controls to model inputs
			_closeButton.onClick.AddListener( Close );
			_sizeButton.onClick.AddListener( CycleSize );
			_contextButton.onClick.AddListener( CycleContext );
		}

		private void Close () {

			OnClosed?.Invoke();
		}
		private void CycleSize () {

			_size.Set(
				_size.Get() switch {
					Sizes.Compact => Sizes.Expanded,
					Sizes.Expanded => Sizes.Compact,
					_ => throw new ArgumentOutOfRangeException( nameof( _size ) )
				}
			);
		}
		private void CycleContext () {

			_context.Set(
				_context.Get() switch {
					Contexts.Floating => Contexts.Focused,
					Contexts.Focused => Contexts.Floating,
					_ => throw new ArgumentOutOfRangeException( nameof( _context ) )
				}
			);
		}
	}

}
