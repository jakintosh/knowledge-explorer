using Jakintosh.Observable;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Explorer.View {

	public class WorkspaceCreateTab : View {

		// *********** Public Interface ***********

		public UnityEvent OnDismiss = new UnityEvent();


		// *********** Private Interface ***********

		[Header( "UI Control" )]
		[SerializeField] private Button _cancelButton;
		[SerializeField] private Button _confirmButton;
		[SerializeField] private TextEdit.Text _input;

		[Header( "UI Display" )]
		[SerializeField] private Image _validIndicator;

		// view model
		private ValidatedObservable<string> _name;

		protected override void OnInitialize () {

			_input.Init();

			// init observables
			_name = new ValidatedObservable<string>(
				initialValue: "",
				onChange: name => {
					_input.SetText( name );
				},
				onValid: isValid => {
					_validIndicator.color = isValid ? Client.Colors.Action : Client.Colors.Error;
					_confirmButton.interactable = isValid;
				},
				validators: Client.Resources.Workspaces.ValidateName
			);

			// sub to controls
			_confirmButton.onClick.AddListener( () => {
				ConfirmNewWorkspace( _name.Get() );
			} );
			_cancelButton.onClick.AddListener( () => {
				_name.Set( "" );
				OnDismiss?.Invoke();
			} );
			_input.OnTextChanged.AddListener( name => {
				_name.Set( name );
			} );
			_input.OnSubmit.AddListener( () => {
				if ( _name.IsValid ) {
					ConfirmNewWorkspace( _name.Get() );
				}
			} );
		}
		protected override void OnCleanup () { }

		private void ConfirmNewWorkspace ( string name ) {

			Client.Resources.Workspaces.New( name );
			OnDismiss?.Invoke();
		}

	}
}