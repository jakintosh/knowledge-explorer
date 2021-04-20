using Framework;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Explorer.View {

	[Serializable]
	public struct RelationshipTypeCellData {

		public string UID;
		public Model.Graph.RelationshipType RelationshipType;

		public RelationshipTypeCellData ( string uid, Model.Graph.RelationshipType relationshipType ) {

			UID = uid;
			RelationshipType = relationshipType;
		}
	}

	public class RelationshipTypeCell : Framework.UI.Cell<RelationshipTypeCellData> {

		[Header( "UI Control" )]
		[SerializeField] private Button _editButton;
		[SerializeField] private Button _deleteButton;
		[Space]
		[SerializeField] private TMP_InputField _nameInputField;
		[SerializeField] private Toggle _textTypeToggle;
		[SerializeField] private Toggle _numberTypeToggle;
		[SerializeField] private Toggle _redColorToggle;
		[SerializeField] private Toggle _purpleColorToggle;
		[SerializeField] private Toggle _indigoColorToggle;
		[SerializeField] private Toggle _blueColorToggle;
		[SerializeField] private Toggle _greenColorToggle;
		[SerializeField] private Toggle _yellowColorToggle;
		[Space]
		[SerializeField] private Button _saveButton;
		[SerializeField] private Button _cancelButton;

		[Header( "UI Display" )]
		[SerializeField] private TextMeshProUGUI _nameText;
		[SerializeField] private GameObject _editContainer;
		[SerializeField] private Image _typeImage;
		[SerializeField] private Image _colorImage;

		[Header( "UI Assets" )]
		[SerializeField] private Sprite _textTypeSprite;
		[SerializeField] private Sprite _numberTypeSprite;

		// model data
		private Observable<bool> _isEditing;
		private Observable<string> _name;
		private Observable<Model.Graph.NodeDataTypes> _dataType;

		protected override void Awake () {

			base.Awake();

			// init observables
			_isEditing = new Observable<bool>(
				initialValue: false,
				onChange: isEditing => {
					_editContainer.SetActive( isEditing );
					LayoutRebuilder.ForceRebuildLayoutImmediate( transform.parent as RectTransform );
				}
			);
			_name = new Observable<string>(
				initialValue: "{Uninitialized}",
				onChange: name => {
					_nameText.text = name;
					_nameInputField.SetTextWithoutNotify( name );
				}
			);
			_dataType = new Observable<Model.Graph.NodeDataTypes>(
				initialValue: Model.Graph.NodeDataTypes.Invalid,
				onChange: type => {
					_typeImage.sprite = type switch {
						Model.Graph.NodeDataTypes.Integer => _numberTypeSprite,
						Model.Graph.NodeDataTypes.String => _textTypeSprite,
						_ => null
					};
				}
			);

			// subscribe to controls
			_editButton.onClick.AddListener( () => {
				_isEditing.Set( true );
			} );
			_saveButton.onClick.AddListener( () => {
				_isEditing.Set( false );
			} );
			_textTypeToggle.onValueChanged.AddListener( isOn => {
				if ( isOn ) { _dataType.Set( Model.Graph.NodeDataTypes.String ); }
			} );
			_numberTypeToggle.onValueChanged.AddListener( isOn => {
				if ( isOn ) { _dataType.Set( Model.Graph.NodeDataTypes.Integer ); }
			} );
		}

		protected override void ReceiveData ( RelationshipTypeCellData data ) {

			_name.Set( data.RelationshipType.Name );
			_dataType.Set( data.RelationshipType.DataType );
		}
	}
}