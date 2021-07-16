using Explorer.View;
using UnityEngine;

using ConceptModel = Explorer.View.Model.Concept;
using KnowledgeGraph = Jakintosh.Knowledge.Graph;

namespace App.View {

	public class Concept : ReuseableView<ConceptModel> {

		// ********** Public Interface **********

		public override ConceptModel GetState () {
			return new ConceptModel(
				_graph.UID,
				_nodeUid,
				Float3.From( transform.localPosition ),
				PresenceControl.Sizes.Expanded
			);
		}

		public void Save () {

			var title = _title.GetText();
			if ( _savedTitle != title ) { _graph.UpdateConceptTitle( _nodeUid, title ); }

			var body = _body.GetText();
			if ( _savedBody != body ) { _graph.UpdateConceptBody( _nodeUid, body ); }
		}

		// ********** Private Interface **********

		[Header( "UI Control" )]
		[SerializeField] private ToggleControl _editToggle;
		[SerializeField] private Draggable3DControl _dragTarget;

		[Header( "UI Display" )]
		[SerializeField] private Panel _panel;
		[SerializeField] private TextEdit.Text _title;
		[SerializeField] private TextEdit.Text _body;
		[SerializeField] private GameObject _outline;

		private string _nodeUid;
		private KnowledgeGraph _graph;
		private string _savedTitle;
		private string _savedBody;

		protected override void OnInitialize () {

			// init subviews
			_panel.Init();
			_editToggle.Init();
			_title.Init();
			_body.Init();

			// subscribe to controls
			_editToggle.OnToggled.AddListener( isEditable => {
				_title.SetEditable( isEditable );
				_body.SetEditable( isEditable );
			} );
			_dragTarget.OnDragBegin.AddListener( eventData => {
				_outline.SetActive( true );
			} );
			_dragTarget.OnDragDelta.AddListener( eventData => {
				transform.position += eventData.Delta;
			} );
			_dragTarget.OnDragEnd.AddListener( eventData => {
				_outline.SetActive( false );
			} );
		}
		protected override void OnPopulate ( ConceptModel data ) {

			_nodeUid = data.NodeUID;
			_graph = Explorer.Client.Resources.Graphs.Get( data.GraphUID );

			_savedTitle = _graph?.GetConceptTitle( _nodeUid );
			_savedBody = _graph?.GetConceptBody( _nodeUid );

			_title.SetText( _savedTitle ?? "{{ Loading error }}" );
			_body.SetText( _savedBody ?? "{{ Loading error }}" );

			transform.localPosition = data.Position.ToVector3();
		}
		protected override void OnRecycle () {

			_title.SetText( "" );
			_body.SetText( "" );
		}
		protected override void OnCleanup () { }

	}
}

