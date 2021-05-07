using System.Collections.Generic;

namespace Graph {

	/*

	Idea for what Node Query Returns

	NODE
	- [Name] [Type] [Value Type] [Value]
	- [Name] [Type] [Value Type] [Value]
	- [Name] [Type] [Value Type] [Value]

	*/

	public class Query {

		// data
		private Database _graph;
		private HashSet<string> _nodes;

		protected Query ( Database graph ) {

			_graph = graph;
			_nodes = new HashSet<string>();
		}

		public static Query WithGraph ( Database graph ) {

			return new Query( graph );
		}
		public Query FromNode ( string uid ) {

			_nodes.Clear();
			_nodes.Add( uid );
			return this;
		}
		public Query FromRelationType ( string relationTypeUID, bool inverse = false ) {

			var linkUIDs = _graph.GetLinkUIDsOfRelationType( relationTypeUID );
			var links = _graph.GetLinks( linkUIDs );
			var nodeUIDs = links.Convert( link => inverse ? link.FromUID : link.ToUID );

			_nodes.Clear();
			_nodes.UnionWith( nodeUIDs );

			return this;
		}
		public Query Duplicate () {

			var query = new Query( _graph );
			query._nodes = new HashSet<string>( _nodes );
			return query;
		}
		public Query FilterNeighbors ( string relationTypeUID, bool inverse = false ) {

			var linkUIDs = new List<string>();
			_graph.GetNodes( _nodes ).ForEach( node => {
				linkUIDs.AddRange( inverse ? node.BacklinkUIDs : node.LinkUIDs );
			} );

			var links = _graph.GetLinks( linkUIDs )
				.Filter( r => r.TypeUID == relationTypeUID )
				.Convert( r => r.ToUID );

			_nodes.Clear();
			_nodes.UnionWith( links );

			return this;
		}
		public int ResultCount () {

			return _graph.GetNodes( _nodes ).Count;
		}
		public List<T> ResultsOfType<T> () {

			var result = _graph
				.GetNodes( _nodes )
				.Filter( node => ( node as Node<T> ) != null )
				.Convert( node => ( node as Node<T> ).Value );

			return new List<T>( result );
		}
	}
}