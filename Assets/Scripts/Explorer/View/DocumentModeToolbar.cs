using Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Explorer.View {


	public class DocumentModeToolbar : View {

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
				initialValue: DocumentModes.Edit,
				onChange: mode => {
					_editIcon.color = mode == DocumentModes.Edit ? Client.Application.Colors.Action : Client.Application.Colors.Foreground;
					_reorderIcon.color = mode == DocumentModes.Reorder ? Client.Application.Colors.Action : Client.Application.Colors.Foreground;
					_readIcon.color = mode == DocumentModes.Read ? Client.Application.Colors.Action : Client.Application.Colors.Foreground;
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