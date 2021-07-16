using Jakintosh.Graph;
using Jakintosh.Observable;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Explorer.View {

	public class RelationTypeEditor : ReuseableView<RelationType> {

		public UnityEvent<string, string> OnNameChanged = new UnityEvent<string, string>();
		public UnityEvent<string, string> OnColorStringChanged = new UnityEvent<string, string>();

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

		// view lifecycle
		public override RelationType GetState () {

			return new RelationType(
				uid: _activeID,
				name: _name.Get()
			);
		}
		protected override void OnInitialize () {

			_name = new Observable<string>(
				initialValue: "{Uninitialized}",
				onChange: name => {
					_nameInputField.SetTextWithoutNotify( name );
				}
			);

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
		protected override void OnPopulate ( RelationType relationType ) {

			_activeID = relationType?.UID ?? null;
			_name.Set( relationType?.Name ?? "" );

			var interactive = relationType != null;
			_nameInputField.interactable = interactive;
			_redColorToggle.interactable = interactive;
			_purpleColorToggle.interactable = interactive;
			_indigoColorToggle.interactable = interactive;
			_blueColorToggle.interactable = interactive;
			_greenColorToggle.interactable = interactive;
			_yellowColorToggle.interactable = interactive;
			_deleteButton.interactable = interactive;
		}
		protected override void OnRecycle () { }
		protected override void OnCleanup () { }
	}

}