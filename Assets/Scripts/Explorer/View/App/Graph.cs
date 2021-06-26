using Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Explorer.View {

	public class Graph : View {

		[Header( "UI Control" )]
		[SerializeField] private WorkspaceMenu _workspaceMenu;

		protected override void OnInitialize () {

			_workspaceMenu.Init();
		}
		protected override void OnCleanup () {

		}
	}

}