using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace View {

	public class SearchResultCell : Framework.UI.Cell<Library.Model.Node>, IPointerClickHandler {


		// ************ Pulic Interface ************

		protected override void ReceiveData ( Library.Model.Node node ) {

			_id = node.ID;
			_nodeTitleText.text = node.Title;
		}

		// ********** Private Interface **********

		[Header( "UI Components" )]
		[SerializeField] private TextMeshProUGUI _nodeTitleText;

		// data
		private string _id;

		// ***** IPointerClick Implementation *****

		void IPointerClickHandler.OnPointerClick ( PointerEventData eventData ) {

			Library.Model.Workspace.Instance.OpenNode( _id );
		}
	}
}