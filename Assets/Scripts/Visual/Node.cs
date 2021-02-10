using Model;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace View {


	public class Node : MonoBehaviour {

		// data types
		public enum EditStates {
			Edit,
			View
		}
		public enum WindowStates {
			Minimized,
			Maximized
		}


		// properties
		public EditStates EditState {
			get => _editState;
			set {
				if ( _editState == value ) { return; }
				_editState = value;

				switch ( _editState ) {

					case EditStates.View:
						SetCaretRaycastTarget( isTarget: false );
						_titleInputField.interactable = false;
						_contentInputField.interactable = false;
						break;

					case EditStates.Edit:
						SetCaretRaycastTarget( isTarget: true );
						_titleInputField.interactable = true;
						_contentInputField.interactable = true;
						break;
				}
			}
		}
		public WindowStates WindowState {
			get => _windowState;
			set {
				if ( _windowState == value ) { return; }
				_windowState = value;

				_editToggle.gameObject.SetActive( _windowState == WindowStates.Maximized );
				_minimizationTimer.Start();
			}
		}
		public Model.Node NodeModel {
			get => _data;
			set {
				if ( _data == value ) { return; }
				_data = value;
				// do something
				_titleInputField.text = _data.Title;
				_contentInputField.text = new View.Content( content: new Model.Content( _data.Body ), style: _style ).TMPString;
			}
		}
		public Model.Style Style {
			get => _style;
			set {
				if ( _style == value ) { return; }
				_style = value;
				_styleRenderer.SetStyle( _style );
			}
		}


		// mono lifecycle
		private void Awake () {

			// set initial data
			NodeModel = _data;

			// set initial style
			_styleRenderer.SetStyle( _style );

			// read initial toggle values
			HandleEditToggle( _editToggle.isOn );
			HandleMinimizeToggle( _minimizeToggle.isOn );

			// subscribe to toggle changes
			_editToggle.onValueChanged.AddListener( HandleEditToggle );
			_minimizeToggle.onValueChanged.AddListener( HandleMinimizeToggle );
		}
		private void Start () {

			_carets = new List<MaskableGraphic>( GetComponentsInChildren<TMP_SelectionCaret>() );
			SetCaretRaycastTarget( isTarget: false );
		}
		private void Update () {

			AnimateMinimization();
		}

		// sizing
		private void AnimateMinimization () {

			if ( _minimizationTimer.IsRunning ) {

				var startSize = WindowState switch {
					WindowStates.Minimized => new Vector3( x: 4f, y: 6f, z: 0.2f ),
					WindowStates.Maximized => new Vector3( x: 3f, y: 0.9f, z: 0.2f ),
					_ => Vector3.zero
				};
				var targetSize = WindowState switch {
					WindowStates.Minimized => new Vector3( x: 3f, y: 0.9f, z: 0.2f ),
					WindowStates.Maximized => new Vector3( x: 4f, y: 6f, z: 0.2f ),
					_ => Vector3.zero
				};

				_size = Vector3.Lerp( startSize, targetSize, _minimizationTimer.Percentage );

				// delete timer when complete
				if ( _minimizationTimer.IsComplete ) {
					_minimizationTimer.Stop(); ;
				}
			}
		}
		private void SetBackgroundSize ( Vector2 size ) {

			_backgroundRoot.transform.localScale = new Vector3( size.x, size.y, 0.2f );
			_backgroundRoot.transform.localPosition = new Vector3( size.x / 2f, size.y / -2f, 0f );
		}
		private void FitCanvas ( Vector2 size ) {

			_canvasRoot.sizeDelta = new Vector2( size.x * 100, size.y * 100 );
			LayoutRebuilder.ForceRebuildLayoutImmediate( _canvasRoot );
		}
		private void PositionConnectors ( Vector2 size ) {

			_connectorL.localPosition = new Vector3( 0f, -0.75f / 2f, 0f );
			_connectorR.localPosition = new Vector3( size.x, -0.75f / 2f, 0f );
		}

		// helpers
		private void SetCaretRaycastTarget ( bool isTarget ) {

			_carets.ForEach( caret => caret.raycastTarget = isTarget );
		}

		// ui event handlers
		private void HandleEditToggle ( bool isOn ) {

			EditState = isOn ? EditStates.Edit : EditStates.View;
		}
		private void HandleMinimizeToggle ( bool isOn ) {

			WindowState = isOn ? WindowStates.Minimized : WindowStates.Maximized;
		}



		[Header( "Data" )]
		[SerializeField] private EditStates _editState;
		[SerializeField] private WindowStates _windowState;
		[SerializeField] private Model.Node _data;
		[SerializeField] private Model.Style _style;

		[Header( "UI Content" )]
		[SerializeField] private StyleRenderer _styleRenderer;
		[SerializeField] private TMP_InputField _titleInputField;
		[SerializeField] private TMP_InputField _contentInputField;

		[Header( "UI Component References" )]
		[SerializeField] private Toggle _editToggle;
		[SerializeField] private Toggle _minimizeToggle;
		[SerializeField] private RectTransform _canvasRoot;
		[SerializeField] private Transform _backgroundRoot;
		[SerializeField] private Transform _connectorL;
		[SerializeField] private Transform _connectorR;


		private Timer _minimizationTimer = new Timer( 0.3f );
		private List<MaskableGraphic> _carets;
		private Vector2 __size;
		private Vector2 _size {
			get => __size;
			set {
				if ( __size == value ) { return; }
				__size = value;
				// size changed
				SetBackgroundSize( __size );
				FitCanvas( __size );
				PositionConnectors( __size );
			}
		}

	}
}