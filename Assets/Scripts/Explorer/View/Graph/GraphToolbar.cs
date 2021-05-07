using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Explorer.View {

	public class GraphToolbar : View {

		// ********** Private Interface **********

		public UnityEvent OnNewItem = new UnityEvent();
		public UnityEvent OnSave = new UnityEvent();

		// ********** Private Interface **********

		[SerializeField] private Button _addButton;
		[SerializeField] private Button _saveButton;


		protected override void Init () {

			// connect controls
			_addButton.onClick.AddListener( () => {
				OnNewItem?.Invoke();
			} );
			_saveButton.onClick.AddListener( () => {
				OnSave?.Invoke();
			} );
		}
	}

}