using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Explorer.View {

	public class GraphToolbar : View {

		// ********** Public Interface **********

		public UnityEvent OnNewItem = new UnityEvent();
		public UnityEvent OnDeleteItem = new UnityEvent();
		public UnityEvent OnSave = new UnityEvent();


		// ********** Private Interface **********

		[Header( "UI Control" )]
		[SerializeField] private Button _addButton;
		[SerializeField] private Button _deleteButton;
		[SerializeField] private Button _saveButton;


		protected override void OnInitialize () {

			_addButton.onClick.AddListener( () => {
				OnNewItem?.Invoke();
			} );
			_deleteButton.onClick.AddListener( () => {
				OnDeleteItem?.Invoke();
			} );
			_saveButton.onClick.AddListener( () => {
				OnSave?.Invoke();
			} );
		}
		protected override void OnCleanup () { }
	}

}