using UnityEngine;
using TMPro;

namespace Explorer.View {

	public class ContextMenuCell : Framework.UI.Cell<Model.ContextAction> {

		[Header( "UI Display" )]
		[SerializeField] private TextMeshProUGUI _titleText;

		protected override void ReceiveData ( Model.ContextAction data ) {
			_titleText.text = data.Name;
		}
	}
}