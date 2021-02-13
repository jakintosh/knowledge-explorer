using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace View {

	public class SearchResultCell : MonoBehaviour, IPointerClickHandler {

		[SerializeField] private TextMeshProUGUI _nodeTitleText;

		[SerializeField] private string _id;

		public void SetNode ( Model.Node node ) {

			_id = node.ID;
			_nodeTitleText.text = node.Title;
		}

		void IPointerClickHandler.OnPointerClick ( PointerEventData eventData ) {

			Model.Workspace.Instance.OpenNode( _id );
		}
	}
}