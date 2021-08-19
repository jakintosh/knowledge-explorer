using Jakintosh.List;
using System;
using UnityEngine;
using TMPro;

using ResourceMetadata = Jakintosh.Resources.Metadata;

namespace Explorer.View {

	[Serializable]
	public struct WorkspaceCellData {

		public string Title { get; private set; }
		public bool Active { get; private set; }
		public ResourceMetadata WorkspaceMetadata { get; private set; }

		public WorkspaceCellData ( string title, bool active, ResourceMetadata metadata ) {

			Title = title;
			Active = active;
			WorkspaceMetadata = metadata;
		}
	}

	public class WorkspaceCell : Cell<WorkspaceCellData> {

		[Header( "UI Display" )]
		[SerializeField] private TextMeshProUGUI _titleText;
		[SerializeField] private GameObject _activeIndicator;

		protected override void ReceiveData ( WorkspaceCellData data ) {

			_titleText.text = data.Title;
			_activeIndicator?.SetActive( data.Active );
		}
	}
}