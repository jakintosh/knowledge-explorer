using Jakintosh.View;
using System.Collections.Generic;
using UnityEngine;

namespace Library.Views {

	public class SystemOverlay : View {

		[SerializeField] private DebugView _debugView;
		[SerializeField] private Commands _commandsView;
		[SerializeField] private ActionHistory _actionHistory;

		protected override void OnInitialize () {

			_commandsView.InitWith(
				new List<KeyValueListData>{
					new KeyValueListData( key: "Cmd + N", value: "Create New" ),
					new KeyValueListData( key: "Cmd + O", value: "Open Node" ),
					new KeyValueListData( key: "Cmd + Shift + S", value: "Save As" ),
					new KeyValueListData( key: "Cmd + N", value: "Create New" ),
					new KeyValueListData( key: "Cmd + O", value: "Open Node" ),
					new KeyValueListData( key: "Cmd + Shift + S", value: "Save As" ),
					new KeyValueListData( key: "Cmd + Shift + S", value: "Save As" ),
					new KeyValueListData( key: "Cmd + N", value: "Create New" ),
					new KeyValueListData( key: "Cmd + O", value: "Open Node" ),
					new KeyValueListData( key: "Cmd + Shift + S", value: "Save As" ),
					new KeyValueListData( key: "Cmd + Shift + S", value: "Save As" ),
					new KeyValueListData( key: "Cmd + N", value: "Create New" ),
					new KeyValueListData( key: "Cmd + O", value: "Open Node" ),
					new KeyValueListData( key: "Cmd + Shift + S", value: "Save As" ),
					new KeyValueListData( key: "Cmd + N", value: "Create New" ),
					new KeyValueListData( key: "Cmd + O", value: "Open Node" ),
					new KeyValueListData( key: "Cmd + Shift + S", value: "Save As" )
				}
			);

			_actionHistory.InitWith( Library.App.History );

			_debugView.Init();
		}
		protected override void OnCleanup () { }

		private bool _debugEnabled = false;
		private float _commandTimer = 0f;
		private void Update () {

			var commandDown = ( Input.GetKey( KeyCode.LeftControl ) || Input.GetKey( KeyCode.RightControl ) );

			_commandTimer = commandDown ? _commandTimer + Time.deltaTime : 0f;

			if ( commandDown ) {

				if ( Input.GetKeyDown( KeyCode.Z ) ) {
					if ( Input.GetKey( KeyCode.LeftShift ) || Input.GetKey( KeyCode.RightShift ) ) {
						Library.App.History.Redo();
					} else {
						Library.App.History.Undo();
					}
				}

				// enable debug mode
				if ( Input.GetKeyDown( KeyCode.Comma ) ) {
					_debugEnabled = _debugEnabled.Toggled();
				}
			}


			_commandsView.gameObject.SetActive( _commandTimer > 0.66f );
			_debugView.gameObject.SetActive( _debugEnabled );
		}
	}
}