using NUnit.Framework;

using Graph = Server.Graph;

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

public class Data_Tests {

	[Test]
	public void ModelStringTest () {

		var inputString = "Hey this is a [[user]] editable string to test how a [[link]] works.";
		var contentModel = new Library.Model.Content( inputString );

		var bucket = new Library.Model.Bucket();
		var userID = bucket.GetIDForTitle( "user" );
		var linkID = bucket.GetIDForTitle( "link" );
		var expectedModelString = $"Hey this is a ${userID} editable string to test how a ${linkID} works.";

		Assert.AreEqual( inputString, contentModel.UserEditableString );
		Assert.AreEqual( expectedModelString, contentModel.ModelString );
	}

	[Test]
	public void ViewStringText () {

		var inputString = "Hey this is a [[user]] editable string to test how a [[link]] works.";
		var contentModel = new Library.Model.Content( inputString );

		var bucket = new Library.Model.Bucket();
		var userID = bucket.GetIDForTitle( "user" );
		var linkID = bucket.GetIDForTitle( "link" );
		var expectedModelString = $"Hey this is a ${userID} editable string to test how a ${linkID} works.";

		Assert.AreEqual( expectedModelString, contentModel.ModelString );

		var contentView = new View.Content( content: contentModel, style: Library.Model.Style.Default );
		var expectedContentString = "Hey this is a <#0000FFFF><u>user</u></color> editable string to test how a <#0000FFFF><u>link</u></color> works.";

		Assert.AreEqual( expectedContentString, contentView.TMPString );
	}
}