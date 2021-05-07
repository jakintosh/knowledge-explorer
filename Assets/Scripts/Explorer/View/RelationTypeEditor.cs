using Framework;
using Graph;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Explorer.View {

	public class RelationTypeEditor : View<RelationType> {

		public UnityEvent<string, string> OnNameChanged = new UnityEvent<string, string>();
		public UnityEvent<string, string> OnColorStringChanged = new UnityEvent<string, string>();

		public void SetRelationType ( RelationType relType ) {

			_activeID = relType?.UID ?? null;
			_nameInputField.SetTextWithoutNotify( relType?.Name ?? "" );

			var interactive = relType != null;
			_nameInputField.interactable = interactive;
			_redColorToggle.interactable = interactive;
			_purpleColorToggle.interactable = interactive;
			_indigoColorToggle.interactable = interactive;
			_blueColorToggle.interactable = interactive;
			_greenColorToggle.interactable = interactive;
			_yellowColorToggle.interactable = interactive;
			_deleteButton.interactable = interactive;
		}

		[Header( "UI Control" )]
		[SerializeField] private Button _deleteButton;
		[Space]
		[SerializeField] private TMP_InputField _nameInputField;
		[Space]
		[SerializeField] private Toggle _redColorToggle;
		[SerializeField] private Toggle _purpleColorToggle;
		[SerializeField] private Toggle _indigoColorToggle;
		[SerializeField] private Toggle _blueColorToggle;
		[SerializeField] private Toggle _greenColorToggle;
		[SerializeField] private Toggle _yellowColorToggle;

		private string _activeID;

		// model data
		private Observable<string> _name;

		protected override void Init () {

			// init observables
			_name = new Observable<string>(
				initialValue: "{Uninitialized}",
				onChange: name => {
					_nameInputField.SetTextWithoutNotify( name );
				}
			);

			// subscribe to controls
			_nameInputField.onEndEdit.AddListener( name => {
				OnNameChanged?.Invoke( _activeID, name );
			} );

			_redColorToggle.onValueChanged.AddListener( isOn => {
				if ( isOn ) { OnColorStringChanged?.Invoke( _activeID, "#" + ColorUtility.ToHtmlStringRGBA( _redColorToggle.transform.Find( "Color" ).GetComponent<Image>().color ) ); }
			} );
			_purpleColorToggle.onValueChanged.AddListener( isOn => {
				if ( isOn ) { OnColorStringChanged?.Invoke( _activeID, "#" + ColorUtility.ToHtmlStringRGBA( _purpleColorToggle.transform.Find( "Color" ).GetComponent<Image>().color ) ); }
			} );
			_indigoColorToggle.onValueChanged.AddListener( isOn => {
				if ( isOn ) { OnColorStringChanged?.Invoke( _activeID, "#" + ColorUtility.ToHtmlStringRGBA( _indigoColorToggle.transform.Find( "Color" ).GetComponent<Image>().color ) ); }
			} );
			_blueColorToggle.onValueChanged.AddListener( isOn => {
				if ( isOn ) { OnColorStringChanged?.Invoke( _activeID, "#" + ColorUtility.ToHtmlStringRGBA( _blueColorToggle.transform.Find( "Color" ).GetComponent<Image>().color ) ); }
			} );
			_greenColorToggle.onValueChanged.AddListener( isOn => {
				if ( isOn ) { OnColorStringChanged?.Invoke( _activeID, "#" + ColorUtility.ToHtmlStringRGBA( _greenColorToggle.transform.Find( "Color" ).GetComponent<Image>().color ) ); }
			} );
			_yellowColorToggle.onValueChanged.AddListener( isOn => {
				if ( isOn ) { OnColorStringChanged?.Invoke( _activeID, "#" + ColorUtility.ToHtmlStringRGBA( _yellowColorToggle.transform.Find( "Color" ).GetComponent<Image>().color ) ); }
			} );
		}

		protected override void InitFrom ( RelationType data ) {
			throw new System.NotImplementedException();
		}

		public override RelationType GetInitData () {
			throw new System.NotImplementedException();
		}
	}

}