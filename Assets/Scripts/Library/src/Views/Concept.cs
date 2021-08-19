using Jakintosh.View;
using UnityEngine;

using ConceptViewModel = Library.ViewModel.Concept;

namespace Library.Views {

	public class Concept : ReuseableView<ConceptViewModel> {

		// ********** Public Interface **********

		public override ConceptViewModel GetState () {
			return new ConceptViewModel(
				uid: _nodeUid,
				position: Float3.From( transform.localPosition )
			);
		}

		public void Save () {

			var graph = App.Graphs.Default;

			var title = _title.GetText();
			if ( _savedTitle != title ) { graph.UpdateConceptTitle( _nodeUid, title ); }

			var body = _body.GetText();
			if ( _savedBody != body ) { graph.UpdateConceptBody( _nodeUid, body ); }
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
		protected override void OnPopulate ( ConceptViewModel data ) {

			_nodeUid = data.NodeUID;

			var graph = App.Graphs.Default;
			_savedTitle = graph.GetConceptTitle( _nodeUid );
			_savedBody = graph.GetConceptBody( _nodeUid );

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

