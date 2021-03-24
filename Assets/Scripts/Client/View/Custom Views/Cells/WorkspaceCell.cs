using System;
using UnityEngine;
using TMPro;

using ResourceMetadata = Framework.Data.Metadata.Resource;

namespace Client.View {

	[Serializable]
	public struct WorkspaceCellData {

		public string Title;
		public ResourceMetadata WorkspaceMetadata;

		public WorkspaceCellData ( string title, ResourceMetadata metadata ) {

			Title = title;
			WorkspaceMetadata = metadata;
		}
	}

	public class WorkspaceCell : Framework.UI.Cell<WorkspaceCellData> {

		[Header( "UI Display" )]
		[SerializeField] private TextMeshProUGUI _titleText;

		protected override void ReceiveData ( WorkspaceCellData data ) {

			_titleText.text = data.Title;
		}
	}
}

