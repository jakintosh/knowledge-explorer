using System;
using UnityEngine;

[CreateAssetMenu( menuName = "Data/Node", fileName = "New Node" )]
public class Node : ScriptableObject {


	/*

		Building block for an atomic unit of information.

		Should have an id, a title, and content.

		Content should be able to link to other things.

	*/

	public string Title;
	public Content Content;
}


[Serializable]
public class Content {

	[TextArea] public string Body;
}