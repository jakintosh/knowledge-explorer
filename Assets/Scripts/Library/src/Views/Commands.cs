using Jakintosh.View;
using System.Collections.Generic;
using UnityEngine;

namespace Library.Views {

	public class Commands : ReuseableView<List<KeyValueListData>> {

		public override List<KeyValueListData> GetState () => _listData;

		[Header( "UI Configuration" )]
		[SerializeField] private int _maxRowsPerColumn;

		[Header( "UI Display" )]
		[SerializeField] private RectTransform _keyBindingsContainer;

		[Header( "UI Assets" )]
		[SerializeField] private KeyValueList _keyValueListPrefab;
		[SerializeField] private GameObject _dividerPrefab;

		private List<KeyValueListData> _listData;

		protected override void OnInitialize () {

		}
		protected override void OnPopulate ( List<KeyValueListData> listData ) {

			_listData = listData;

			_listData
				.Subdivide( maxLength: _maxRowsPerColumn )
				.ForEach( column => {
					var list = Instantiate(
						original: _keyValueListPrefab,
						parent: _keyBindingsContainer,
						worldPositionStays: false
					);
					list.InitWith( column );

					Instantiate(
						original: _dividerPrefab,
						parent: _keyBindingsContainer,
						worldPositionStays: false
					);

				} );

			// destroy extra divider lol
			Destroy( _keyBindingsContainer.GetChild( _keyBindingsContainer.childCount - 1 ).gameObject );
		}

		protected override void OnRecycle () {

			// TODO: inefficient but whatever
			for ( int i = _keyBindingsContainer.childCount - 1; i >= 0; i-- ) {
				Destroy( _keyBindingsContainer.GetChild( i ) );
			}
		}
		protected override void OnCleanup () { }

	}
}
