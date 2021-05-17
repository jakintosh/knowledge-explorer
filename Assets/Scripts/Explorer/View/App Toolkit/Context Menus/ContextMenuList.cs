using UnityEngine;

namespace Explorer.View {

	public class ContextMenuList : Framework.UI.List<Model.ContextAction, ContextMenuCell> {

		protected override float GetSpacing () => 2f;
		protected override RectOffset GetPadding ()
			=> new RectOffset(
				left: 0,
				right: 0,
				top: 0,
				bottom: 0
			);
	}

}