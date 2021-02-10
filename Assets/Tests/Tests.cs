using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class Tests {

	// A Test behaves as an ordinary method
	[Test]
	public void TestsSimplePasses () {

		Assert.Pass();
	}

	// A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
	// `yield return null;` to skip a frame.
	[UnityTest]
	public IEnumerator TestsWithEnumeratorPasses () {
		// Use the Assert class to test conditions.
		// Use yield to skip a frame.
		yield return null;
	}
}

public class Data_Tests {

	[Test]
	public void ModelStringTest () {

		var inputString = "Hey this is a [[user]] editable string to test how a [[link]] works.";
		var contentModel = new Model.Content( inputString );

		var userID = Model.NodeManager.Instance.GetIDForTitle( "user" );
		var linkID = Model.NodeManager.Instance.GetIDForTitle( "link" );
		var expectedModelString = $"Hey this is a ${userID} editable string to test how a ${linkID} works.";

		Assert.AreEqual( inputString, contentModel.UserEditableString );
		Assert.AreEqual( expectedModelString, contentModel.ModelString );
	}

	[Test]
	public void ViewStringText () {

		var inputString = "Hey this is a [[user]] editable string to test how a [[link]] works.";
		var contentModel = new Model.Content( inputString );

		var userID = Model.NodeManager.Instance.GetIDForTitle( "user" );
		var linkID = Model.NodeManager.Instance.GetIDForTitle( "link" );
		var expectedModelString = $"Hey this is a ${userID} editable string to test how a ${linkID} works.";

		Assert.AreEqual( expectedModelString, contentModel.ModelString );

		var contentView = new View.Content( content: contentModel, style: Model.Style.Default );
		var expectedContentString = "Hey this is a <#0000FFFF><u>user</u></color> editable string to test how a <#0000FFFF><u>link</u></color> works.";

		Assert.AreEqual( expectedContentString, contentView.TMPString );
	}
}