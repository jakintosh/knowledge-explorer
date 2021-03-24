using Client.Model;
using Framework;
using UnityEngine;

namespace Client.View {

	public class Window : ModelHandler<ViewModel.Window> {


		// ****** ModelHandler Implementation ******

		protected override string BindingKey => "view.window";
		protected override void PropogateModel ( ViewModel.Window model ) {

			_header?.SetModel( model );
		}
		protected override void HandleNullModel () => throw new System.NotImplementedException();
		protected override void BindViewToOutputs ( ViewModel.Window model ) {

			Bind( _sizeBinding, toOutput: model.Frame.Size );
			Bind( _positionBinding, toOutput: model.Frame.Position );
		}


		// ********** Private Interface **********

		// static data
		private static Vector3 COMPACT_SIZE = new Vector3( 2f, 0.64f, 0.1f );
		private static Vector3 EXPANDED_SIZE = new Vector3( 4f, 6f, 0.1f );


		[Header( "View Components" )]
		[SerializeField] private Header _header;
		[SerializeField] private Transform _background;

		// bindings
		private Output<Vector3>.Binding _sizeBinding;
		private Output<Vector3>.Binding _positionBinding;
		private Output<ViewModel.Presence.Sizes>.Binding _presenceSizeBinding;


		private void Awake () {

			// create bindings
			_sizeBinding = new Output<Vector3>.Binding( valueHandler: size => {
				if ( _background == null ) { return; }
				_background.transform.localScale = size;
				_background.transform.localPosition = new Vector3( size.x / 2f, -size.y / 2f, 0f );
			} );

			_positionBinding = new Output<Vector3>.Binding( valueHandler: position => {
				transform.position = position;
			} );

			_presenceSizeBinding = new Output<ViewModel.Presence.Sizes>.Binding( valueHandler: presenceSize => {
				_model?.Frame.Size.Set(
					presenceSize switch {
						ViewModel.Presence.Sizes.Compact => COMPACT_SIZE,
						ViewModel.Presence.Sizes.Expanded => EXPANDED_SIZE,
						_ => Vector3.one
					}
				);
			} );
		}
	}

}