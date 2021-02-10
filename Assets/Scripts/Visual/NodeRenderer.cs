using Model;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NodeRenderer : MonoBehaviour {

	/*

	visualize the node information with the given style information

	save data when it changes

	*/

	[Header( "Data" )]
	[SerializeField] private Node _node;
	[SerializeField] private Style _style;

	[Header( "Object References" )]
	[SerializeField] private Transform _connectorL;
	[SerializeField] private Transform _connectorR;

	[Header( "UI Components" )]
	[SerializeField] private Transform _root;
	[SerializeField] private RectTransform _canvasRoot;
	[SerializeField] private TMP_InputField _titleInputField;
	[SerializeField] private ContentRenderer _content;
	[SerializeField] private StyleRenderer[] _styleRenderers;

	private void Awake () {

		SetStyle( _style );
		SetNode( _node );
	}
	private void Update () {

		AnimateMinimization();
		FitCanvas();
		PositionConnectors();
	}

	private void FitCanvas () {

		// always resize the canvas to be based off of node cube size
		_canvasRoot.sizeDelta = new Vector2( _root.transform.localScale.x * 100, _root.transform.localScale.y * 100 );
		if ( _root.transform.hasChanged ) {
			LayoutRebuilder.ForceRebuildLayoutImmediate( _canvasRoot );
			_root.transform.hasChanged = false;
		}
	}
	private void AnimateMinimization () {

		if ( _minimizationTimer.IsRunning ) {

			var startScale = !_isMinimized ? new Vector3( x: 2f, y: 0.75f, z: 0.2f ) : new Vector3( x: 4f, y: 6f, z: 0.2f );
			var targetScale = _isMinimized ? new Vector3( x: 2f, y: 0.75f, z: 0.2f ) : new Vector3( x: 4f, y: 6f, z: 0.2f );
			_root.transform.localScale = Vector3.Lerp( startScale, targetScale, _minimizationTimer.Percentage );

			_root.transform.localPosition = new Vector3( _root.transform.localScale.x / 2f, _root.transform.localScale.y / -2f, 0f );

			// delete timer when complete
			if ( _minimizationTimer.IsComplete ) {
				_minimizationTimer.Stop(); ;
			}
		}
	}
	private void PositionConnectors () {

		_connectorL.localPosition = new Vector3( 0f, -0.75f / 2f, 0f );
		_connectorR.localPosition = new Vector3( _root.transform.localScale.x, -0.75f / 2f, 0f );
	}

	public void SetStyle ( Style style ) {

		foreach ( var styleRenderer in _styleRenderers ) {
			styleRenderer.SetStyle( _style );
		}
		_content.Style = style;
	}
	public void SetNode ( Node node ) {

		_titleInputField.text = node.Title;
		_content.SetContent(
			new View.Content(
				content: new Model.Content( userString: node.Body ),
				style: _style
			)
		);

		LayoutRebuilder.ForceRebuildLayoutImmediate( _canvasRoot );
	}
	public void SetMinimized ( bool isMinimized ) {

		_isMinimized = isMinimized;
	}
	public void SetEditMode ( bool isEditing ) {


	}

	private Timer _minimizationTimer;
	private bool __isMinimized = false;
	private bool _isMinimized {
		get => __isMinimized;
		set {
			if ( __isMinimized != value ) {
				__isMinimized = value;

				_minimizationTimer = new Timer( duration: 0.25f );
				_minimizationTimer.Start();
			}
		}
	}
}