using Framework;
using System;
using UnityEngine;
using UnityEngine.UI;

using Sizes = Client.ViewModel.Presence.Sizes;
using Contexts = Client.ViewModel.Presence.Contexts;

namespace Client.View {

	public class PresenceControl : ModelHandler<ViewModel.Presence> {


		// ****** ModelHandler Implementation ******

		protected override string BindingKey => "view.presence-control";
		protected override void PropogateModel ( ViewModel.Presence model ) { }
		protected override void HandleNullModel () => throw new System.NotImplementedException();
		protected override void BindViewToOutputs ( ViewModel.Presence model ) {

			Bind( _sizeBinding, toOutput: model.Size );
			Bind( _contextBinding, toOutput: model.Context );
		}


		// ********** Public Interface **********

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

		// bindings
		private Output<Sizes>.Binding _sizeBinding;
		private Output<Contexts>.Binding _contextBinding;

		// lifecycle
		private void Awake () {

			// create bindings
			_sizeBinding = new Output<Sizes>.Binding( valueHandler: size => {
				_sizeButtonImage.sprite = size switch {
					Sizes.Compact => _expandedSizeSprite,
					Sizes.Expanded => _compactSizeSprite,
					_ => throw new ArgumentOutOfRangeException( nameof( size ) )
				};
			} );
			_contextBinding = new Output<Contexts>.Binding( valueHandler: context => {
				_contextButtonImage.sprite = context switch {
					Contexts.Floating => _focusedContextSprite,
					Contexts.Focused => _floatingContextSprite,
					_ => throw new ArgumentOutOfRangeException( nameof( context ) )
				};
			} );

			// connect controls to model inputs
			_closeButton.onClick.AddListener( () => _model?.Close() );
			_sizeButton.onClick.AddListener( () => _model?.CycleSize() );
			_contextButton.onClick.AddListener( () => _model?.CycleContext() );
		}
	}

}
