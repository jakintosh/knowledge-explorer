using Jakintosh.View;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Library.Views {

	public struct KeyValueListData {

		public string Key { get; private set; }
		public string Value { get; private set; }

		public KeyValueListData ( string key, string value ) {
			Key = key;
			Value = value;
		}
	}

	public class KeyValueList : ReuseableView<List<KeyValueListData>> {

		public override List<KeyValueListData> GetState () => _listData;

		[Header( "UI Display" )]
		[SerializeField] private RectTransform _keyContainer;
		[SerializeField] private RectTransform _valueContainer;

		[Header( "UI Assets" )]
		[SerializeField] private TextMeshProUGUI _keyTextPrefab;
		[SerializeField] private TextMeshProUGUI _valueTextPrefab;

		private List<KeyValueListData> _listData;

		protected override void OnInitialize () { }
		protected override void OnPopulate ( List<KeyValueListData> listData ) {

			_listData = listData;

			if ( _keyTextPrefab != null && _valueTextPrefab != null ) {

				_listData.ForEach( row => {

					// TODO: inefficient but whatever
					var key = Instantiate( _keyTextPrefab, _keyContainer, false );
					key.name = "Key Text";
					key.text = row.Key;

					var value = Instantiate( _valueTextPrefab, _valueContainer, false );
					value.name = "Value Text";
					value.text = row.Value;
				} );

			} else {
				Debug.Log( "KeyValueList cannot populate, missing text prefabs." );
				return;
			}

		}
		protected override void OnRecycle () {

			// TODO: inefficient but whatever
			for ( int i = _keyContainer.childCount - 1; i >= 0; i-- ) { Destroy( _keyContainer.GetChild( i ) ); }
			for ( int i = _valueContainer.childCount - 1; i >= 0; i-- ) { Destroy( _valueContainer.GetChild( i ) ); }
		}
		protected override void OnCleanup () { }
	}
}