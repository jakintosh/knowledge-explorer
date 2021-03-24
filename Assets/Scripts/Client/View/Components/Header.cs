using Framework;
using UnityEngine;
using TMPro;

namespace Client.View {

	public class Header : ModelHandler<ViewModel.Window> {

		protected override string BindingKey => "view.header";
		protected override void PropogateModel ( ViewModel.Window model ) {

			_presenceControl?.SetModel( model?.Presence );
		}
		protected override void BindViewToOutputs ( ViewModel.Window model ) {

			Bind( _sizeBinding, toOutput: model.Frame.Size );
		}
		protected override void HandleNullModel () => throw new System.NotImplementedException();

		// ********** Private Interface **********

		[Header( "UI Controls" )]
		[SerializeField] private DraggableControl _draggableControl;
		[SerializeField] private PresenceControl _presenceControl;

		[Header( "UI Display" )]
		[SerializeField] private TextMeshProUGUI _titleTextMesh;

		// bindings
		private Output<Vector3>.Binding _sizeBinding;
		private Output<Vector3>.Binding _positionBinding;

		private void Awake () {

			// create bindings
			_sizeBinding = new Output<Vector3>.Binding( valueHandler: size => {
				var rt = ( transform as RectTransform );
				var sizeDelta = rt.sizeDelta;
				sizeDelta.x = size.x * 100f;
				rt.sizeDelta = sizeDelta;
			} );


			// controls
			_draggableControl.OnDragDelta += delta => {
				if ( _model != null ) {
					var pos = _model.Frame.Position.Get();
					_model.Frame.Position.Set( pos + delta );
				}
			};
		}

	}

}
