using Jakintosh.Observable;
using Jakintosh.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace Explorer.View {

	public class WorkspaceSettingsTab : ReuseableView<string> {

		// *********** Public Interface ***********

		public UnityEvent OnDismiss = new UnityEvent();

		public override string GetState () {
			return _workspaceUID.Get();
		}


		// *********** Private Interface ***********

		[Header( "UI Control" )]
		[SerializeField] private ValidatedTextInput _nameInput;
		[SerializeField] private Button _saveButton;
		[SerializeField] private Button _deleteButton;

		[Header( "UI Display" )]
		[SerializeField] private TextMeshProUGUI _nameAsterisk;

		// view model
		private Observable<string> _workspaceUID;
		private Observable<string> _workspaceName;
		private Observable<(string name, bool isValid)> _validatedName;
		private Observable<bool> _hasOpenWorkspace;
		private Observable<bool> _nameHasChanges;
		private Observable<bool> _isNameSaveable;

		private bool GetNameFieldHasChanges () => _validatedName?.Get().name != _workspaceName?.Get();
		private bool GetNameFieldIsSaveable () => ( _validatedName?.Get().isValid ?? false ) && GetNameFieldHasChanges();

		protected override void OnInitialize () {

			// init subviews
			_nameInput.Init();
			_nameInput.SetValidators( Client.Resources.Workspaces.ValidateName );

			// init observables
			_hasOpenWorkspace = new Observable<bool>(
				initialValue: false,
				onChange: hasOpenWorkspace => {
					_nameInput.SetInteractable( hasOpenWorkspace );
					_deleteButton.interactable = hasOpenWorkspace;
				}
			);
			_isNameSaveable = new Observable<bool>(
				initialValue: false,
				onChange: saveable => {
					_saveButton.interactable = saveable;
				}
			);
			_nameHasChanges = new Observable<bool>(
				initialValue: false,
				onChange: hasChanges => {
					_nameAsterisk.gameObject.SetActive( hasChanges );
				}
			);
			_workspaceName = new Observable<string>(
				initialValue: null,
				onChange: name => {

					_nameInput.SetText( name );
					_nameInput.SetValidationExceptions( name );

					_nameHasChanges.Set( GetNameFieldHasChanges() );
					_isNameSaveable.Set( GetNameFieldIsSaveable() );
				}
			);
			_workspaceUID = new Observable<string>(
				initialValue: null,
				onChange: uid => {
					_hasOpenWorkspace.Set( !uid.IsNullOrEmpty() );
					_workspaceName.Set( Client.Resources.Workspaces.Get( uid )?.Name );
				}
			);
			_validatedName = new Observable<(string name, bool isValid)>(
				initialValue: (null, true),
				onChange: validatedName => {
					_nameHasChanges.Set( GetNameFieldHasChanges() );
					_isNameSaveable.Set( GetNameFieldIsSaveable() );
				}
			);

			// sub to controls
			_nameInput.OnTextChanged.AddListener( validatedName => {
				_validatedName.Set( validatedName );
			} );
			_nameInput.OnSubmit.AddListener( () => {
				Save();
			} );
			_saveButton.onClick.AddListener( () => {
				Save();
			} );
			_deleteButton.onClick.AddListener( () => {
				Client.Resources.Workspaces.Delete( _workspaceUID.Get() );
				Client.Contexts.Current.SetWorkspace( null );
				OnDismiss?.Invoke();
			} );

			// sub to app notifications
			Client.Resources.Workspaces.OnMetadataUpdated += HandleMetadataUpdated;
		}
		protected override void OnPopulate ( string workspaceUID ) {

			_workspaceUID.Set( workspaceUID );
		}
		protected override void OnRecycle () { }
		protected override void OnCleanup () {

			Client.Resources.Workspaces.OnMetadataUpdated -= HandleMetadataUpdated;
		}

		// private functions
		private void Save () {

			Client.Resources.Workspaces.Rename( _workspaceUID.Get(), _validatedName.Get().name );
		}

		// event handlers
		private void HandleMetadataUpdated ( Metadata metadata ) {

			// abort if not our active workspace
			if ( metadata.UID != _workspaceUID.Get() ) {
				return;
			}

			_workspaceName.Set( metadata.Name );
		}
	}
}
