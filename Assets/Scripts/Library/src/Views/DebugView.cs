using Jakintosh.Observable;
using Jakintosh.View;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Library.Views {

	public class DebugView : View {

		[Header( "UI Control" )]
		[SerializeField] private ToggleGroup _modeToggleGroup;

		[Header( "UI Display" )]
		[SerializeField] private Transform _tabBar;
		[SerializeField] private TextMeshProUGUI _content;

		[Header( "UI Assets" )]
		[SerializeField] private GameObject tabItemPrefab;

		private enum Mode {
			Graph
		}
		private Observable<Mode> _currentMode;

		protected override void OnInitialize () {

			_currentMode = new Observable<Mode>(
				initialValue: 0,
				onChange: mode => {
					if ( _currentMode != null ) {
						ExitMode( _currentMode.Previous() );
					}
					EnterMode( mode );
				}
			);

			foreach ( Mode mode in System.Enum.GetValues( typeof( Mode ) ) ) {
				var tab = Instantiate( tabItemPrefab, _tabBar, false );
				tab.SetActive( true );
				tab.GetComponentInChildren<TextMeshProUGUI>()?.SetText( mode.ToString() );
				tab.GetComponentInChildren<Toggle>().onValueChanged.AddListener( isOn => {
					if ( isOn ) { _currentMode.Set( mode ); }
				} );
			}
		}
		protected override void OnCleanup () {

		}

		private void EnterMode ( Mode mode ) {

			switch ( mode ) {
				case Mode.Graph:
					FillContentWithGraph();
					App.Graphs.Default.OnNodeEvent.AddListener( HandleGraphEvent );
					App.Graphs.Default.OnLinkEvent.AddListener( HandleGraphEvent );
					break;

				default:
					Debug.Log( "DebugView: Toggled mode that is not supported" );
					break;
			}
		}
		private void ExitMode ( Mode mode ) {

			switch ( mode ) {
				case Mode.Graph:
					App.Graphs.Default.OnNodeEvent.RemoveListener( HandleGraphEvent );
					App.Graphs.Default.OnLinkEvent.RemoveListener( HandleGraphEvent );
					break;

				default:
					Debug.Log( "DebugView: Toggled mode that is not supported" );
					break;
			}
		}

		private void HandleGraphEvent ( Jakintosh.Knowledge.Graph.ResourceEventData e )
			=> FillContentWithGraph();
		private void FillContentWithGraph () {

			var graph = App.Graphs.Default;
			var nodes = graph.AllNodes;
			var links = graph.AllLinks;
			var invalidatedNodes = graph.AllInvalidNodes;
			var invalidatedLinks = graph.AllInvalidLinks;

			var sb = new StringBuilder();

			sb.AppendLine( "### NODES ###" );
			sb.AppendLine();
			nodes.ForEach( node => {
				// header
				var invalidNode = invalidatedNodes.Contains( node.Identifier );
				sb.AppendLine( $"{( invalidNode ? "<color=\"red\">" : "" )}node {node.Identifier} (type: {node.Type.ToString()}){( invalidNode ? "</color>" : "" )}" );

				// links
				sb.Append( "    -link: " );
				node.LinkUIDs.ForEach( link => {
					var invalid = invalidatedLinks.Contains( link );
					sb.Append( $"{( invalid ? "<color=\"red\">" : "" )}{link} |{( invalid ? "</color=\"red\">" : "" )}" );
				} );
				sb.Append( "\n" );

				// backlinks
				sb.Append( "    -backlink: " );
				node.BacklinkUIDs.ForEach( backlink => {
					var invalid = invalidatedLinks.Contains( backlink );
					sb.Append( $"{( invalid ? "<color=\"red\">" : "" )}{backlink} |{( invalid ? "</color=\"red\">" : "" )}" );
				} );
				sb.Append( "\n" );
			} );
			sb.AppendLine();

			sb.AppendLine( "### LINKS ###" );
			sb.AppendLine();
			links.ForEach( link => {

				// header
				var invalidLink = invalidatedLinks.Contains( link.Identifier );
				sb.AppendLine( $"{( invalidLink ? "<color=\"red\">" : "" )}link {link.Identifier} (type: {link.TypeUID}){( invalidLink ? "</color>" : "" )}" );

				var invalidFromLink = invalidatedNodes.Contains( link.FromUID );
				sb.AppendLine( $"{( invalidFromLink ? "<color=\"red\">" : "" )}- from: {link.FromUID}{( invalidFromLink ? "</color=\"red\">" : "" )}" );

				var invalidToLink = invalidatedNodes.Contains( link.ToUID );
				sb.AppendLine( $"{( invalidToLink ? "<color=\"red\">" : "" )}-to: {link.ToUID}{( invalidToLink ? "</color=\"red\">" : "" )}" );
			} );

			_content.text = sb.ToString();
		}
	}
}
