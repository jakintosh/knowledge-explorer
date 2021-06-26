using Jakintosh.Observable;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Explorer.View {


	public class DocumentModeToolbar : View {


		// *********** Public Interface ***********

		public UnityEvent<DocumentModes> OnDocumentModeChanged = new UnityEvent<DocumentModes>();

		public DocumentModes Mode => _mode.Get();

		// *********** Private Interface ***********

		[Header( "UI Control" )]
		[SerializeField] private Button _editButton;
		[SerializeField] private Button _reorderButton;
		[SerializeField] private Button _readButton;

		// observables
		private Observable<DocumentModes> _mode;

		private Image _editIcon;
		private Image _reorderIcon;
		private Image _readIcon;

		protected override void OnInitialize () {

			// get other components
			_editIcon = _editButton.transform.GetChild( 0 ).GetComponent<Image>();
			_reorderIcon = _reorderButton.transform.GetChild( 0 ).GetComponent<Image>();
			_readIcon = _readButton.transform.GetChild( 0 ).GetComponent<Image>();

			// init observables
			_mode = new Observable<DocumentModes>(
				initialValue: DocumentModes.Read,
				onChange: mode => {
					_editIcon.color = mode == DocumentModes.Edit ? Client.Colors.Action : Client.Colors.Foreground;
					_reorderIcon.color = mode == DocumentModes.Reorder ? Client.Colors.Action : Client.Colors.Foreground;
					_readIcon.color = mode == DocumentModes.Read ? Client.Colors.Action : Client.Colors.Foreground;
					OnDocumentModeChanged?.Invoke( mode );
				}
			);

			// subscribe to controls
			_editButton.onClick.AddListener( () => {
				_mode.Set( DocumentModes.Edit );
			} );
			_reorderButton.onClick.AddListener( () => {
				_mode.Set( DocumentModes.Reorder );
			} );
			_readButton.onClick.AddListener( () => {
				_mode.Set( DocumentModes.Read );
			} );
		}
		protected override void OnCleanup () {

		}

	}
}