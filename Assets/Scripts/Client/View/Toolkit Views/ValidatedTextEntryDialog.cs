using Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Client.View {

	public class ValidatedTextEntryDialog : ModelHandler<ViewModel.ValidatedTextEntryDialog> {

		public UnityEvent<string> OnConfirm = new UnityEvent<string>();

		// ****** ModelHandler Implementation ******

		protected override string BindingKey => "view.create-resource-dialog";

		protected override void PropogateModel ( ViewModel.ValidatedTextEntryDialog model ) { }
		protected override void HandleNullModel () => throw new System.NotImplementedException();
		protected override void BindViewToOutputs ( ViewModel.ValidatedTextEntryDialog model ) {

			Bind( _titleBinding, toOutput: model.Title );
			Bind( _resourceNameBinding, toOutput: model.ValidatedText );
			Bind( _validBinding, toOutput: model.ValidatedText.IsValid );
		}


		// *********** Private Interface ***********

		[Header( "UI Control" )]
		[SerializeField] private TMP_InputField _nameInputControl;
		[SerializeField] private Button _cancelControl;
		[SerializeField] private Button _confirmControl;

		[Header( "UI Display" )]
		[SerializeField] private TextMeshProUGUI _titleDisplay;
		[SerializeField] private Image _validityDisplay;

		// bindings
		private Output<string>.Binding _titleBinding;
		private Output<string>.Binding _resourceNameBinding;
		private Output<bool>.Binding _validBinding;

		private void Awake () {

			// connect controls
			_cancelControl.onClick.AddListener( () => {
				_model?.Close();
			} );
			_nameInputControl.onValueChanged.AddListener( name => {
				_model?.ValidatedText.Set( name );
			} );
			_nameInputControl.onSubmit.AddListener( _ => {
				Confirm();
			} );
			_confirmControl.onClick.AddListener( () => {
				Confirm();
			} );

			// create bindings
			_titleBinding = new Output<string>.Binding( valueHandler: text => {
				_titleDisplay.SetText( text );
			} );
			_resourceNameBinding = new Output<string>.Binding( valueHandler: resourceName => {
				_nameInputControl.SetTextWithoutNotify( resourceName );
			} );
			_validBinding = new Output<bool>.Binding( valueHandler: isValid => {
				_validityDisplay.color = isValid ? Color.green : Color.red;
				_confirmControl.interactable = isValid;
			} );
		}

		private void Confirm () {

			OnConfirm.Invoke( _model?.ValidatedText.Get() );
			_model?.Close();
		}
	}

}
