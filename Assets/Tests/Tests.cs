using NUnit.Framework;

using Graph = Explorer.Model.Graph;

public class Graph_Tests {

	[Test]
	public void Graph_CreateNode () {

		var graph = new Graph();
		var testNodeUID = graph.CreateNode();
		var count = Graph.Query
			.WithGraph( graph )
			.FromNode( testNodeUID )
			.ResultCount();

		Assert.AreEqual( count, 1 );
	}

	[Test]
	public void Graph_CreateNode_InferTypeFromNull () {

		var graph = new Graph();
		var testNode = graph.CreateNode<string>( null );
		var testNode2 = graph.CreateNode( (string)null );

		Assert.NotNull( testNode );
		Assert.NotNull( testNode2 );
	}
}