using System;
using UnityEngine;

namespace Explorer.View {

	public class ContextMenu : ReuseableView<Model.ContextMenu> {

		[Header( "UI Display" )]
		[SerializeField] private ContextMenuList _list;

		public override Model.ContextMenu GetState () {
			throw new NotImplementedException();
		}
		protected override void OnInitialize () {

			_list.OnCellClicked.AddListener( cellData => {
				cellData.Action?.Invoke();
			} );
		}
		protected override void OnPopulate ( Model.ContextMenu data ) {

			_list.SetData( data.Actions );
		}
		protected override void OnRecycle () { }
	}
}