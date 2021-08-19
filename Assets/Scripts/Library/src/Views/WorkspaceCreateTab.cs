using Jakintosh.Observable;
using Jakintosh.View;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Library.Views {

	public class WorkspaceCreateTab : View {

		// *********** Public Interface ***********

		public UnityEvent OnDismiss = new UnityEvent();


		// *********** Private Interface ***********

		[Header( "UI Control" )]
		[SerializeField] private ValidatedTextInput _nameInput;
		[SerializeField] private Button _cancelButton;
		[SerializeField] private Button _confirmButton;

		// view model
		private Observable<string> _name;
		private Observable<bool> _canSubmit;

		protected override void OnInitialize () {

			// init subviews
			_nameInput.Init();
			_nameInput.SetValidators( App.Workspaces.ValidateName );

			// init observables
			_name = new Observable<string>(
				initialValue: "",
				onChange: name => {
					_nameInput.SetText( name );
				}
			);
			_canSubmit = new Observable<bool>(
				initialValue: false,
				onChange: canSubmit => {
					_confirmButton.interactable = canSubmit;
				}
			);

			// sub to controls
			_nameInput.OnTextChanged.AddListener( validatedName => {
				_name.Set( validatedName.text );
				_canSubmit.Set( validatedName.isValid );
			} );
			_nameInput.OnSubmit.AddListener( () => {
				ConfirmNewWorkspace( _name.Get() );
			} );
			_confirmButton.onClick.AddListener( () => {
				ConfirmNewWorkspace( _name.Get() );
			} );
			_cancelButton.onClick.AddListener( () => {
				Dismiss();
			} );
		}
		protected override void OnCleanup () { }

		private void ConfirmNewWorkspace ( string name ) {

			Library.App.History.ExecuteAction(
				new Actions.Workspace.Create( name: name, shouldOpen: true )
			);
			Dismiss();
		}
		private void Dismiss () {

			_name.Set( "" );
			OnDismiss?.Invoke();
		}

	}
}