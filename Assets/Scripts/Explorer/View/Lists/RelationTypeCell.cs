using Framework;
using Graph;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Explorer.View {

	[Serializable]
	public struct RelationTypeCellData : IEquatable<RelationTypeCellData> {

		public string UID;
		public string Name;
		public string Color;

		public RelationTypeCellData ( string uid, string name, string colorString ) {

			UID = uid;
			Name = name;
			Color = colorString;
		}

		bool IEquatable<RelationTypeCellData>.Equals ( RelationTypeCellData other ) =>
			EqualityComparer<string>.Default.Equals( UID, other.UID ) &&
			EqualityComparer<string>.Default.Equals( Name, other.Name ) &&
			EqualityComparer<string>.Default.Equals( Color, other.Color );
	}

	public class RelationTypeCell : Framework.UI.Cell<RelationTypeCellData> {

		[Header( "UI Display" )]
		[SerializeField] private TextMeshProUGUI _nameText;
		[SerializeField] private Image _typeImage;
		[SerializeField] private Image _colorImage;

		[Header( "UI Assets" )]
		[SerializeField] private Sprite _textTypeSprite;
		[SerializeField] private Sprite _numberTypeSprite;

		// model data
		private Observable<string> _name;
		private Observable<Color> _color;
		private Observable<NodeDataTypes> _dataType;

		protected override void Awake () {

			base.Awake();

			// init observables
			_name = new Observable<string>(
				initialValue: "{Uninitialized}",
				onChange: name => {
					_nameText.text = name;
				}
			);
			_dataType = new Observable<NodeDataTypes>(
				initialValue: NodeDataTypes.Invalid,
				onChange: type => {
					_typeImage.sprite = type switch {
						NodeDataTypes.Integer => _numberTypeSprite,
						NodeDataTypes.String => _textTypeSprite,
						_ => null
					};
				}
			);
			_color = new Observable<Color>(
				initialValue: Color.magenta,
				onChange: color => {
					_colorImage.color = color;
				}
			);
		}

		protected override void ReceiveData ( RelationTypeCellData data ) {

			_name.Set( data.Name );

			// parse color
			if ( ColorUtility.TryParseHtmlString( data.Color, out var color ) ) {
				_color.Set( color );
			};

		}
	}
}