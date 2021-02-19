using Model;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace View {

	public class Node : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPositionChangeHandler {

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
		public Model.Node Data {
			get => _data;
			set {
				if ( _data == value ) { return; }

				if ( _data != null ) {
					_data.OnTitleChanged -= HandleTitleChange;
					_data.OnBodyChanged -= HandleBodyChanged;
				}
				_data = value;
				if ( _data != null ) {
					_data.OnTitleChanged += HandleTitleChange;
					_data.OnBodyChanged += HandleBodyChanged;
				}

				HandleTitleChange( null, _data.Title );
				HandleBodyChanged( _data.Body );
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

		public Vector3 LinkInPosition => _connectorL.position + _connectorL.right * -0.2f;
		public Vector3 LinkOutPosition => _connectorR.position + _connectorR.right * 0.2f;

		// events
		public delegate void PositionChangedEvent ( string nodeID, Vector3 newPosition );
		public event PositionChangedEvent OnPositionChanged;


		[Header( "Data" )]
		[SerializeField] private EditStates _editState;
		[SerializeField] private WindowStates _windowState;
		[SerializeField] private Model.Node _data;
		[SerializeField] private Model.Style _style;

		[SerializeField] private Sprite _viewSprite;
		[SerializeField] private Sprite _editSprite;

		[Header( "UI Content" )]
		[SerializeField] private StyleRenderer _styleRenderer;
		[SerializeField] private TMP_InputField _titleInputField;
		[SerializeField] private TMP_InputField _contentInputField;

		[Header( "UI Component References" )]
		[SerializeField] private Toggle _editToggle;
		[SerializeField] private Toggle _minimizeToggle;
		[SerializeField] private Button _closeButton;
		[SerializeField] private Image _editImage;
		[SerializeField] private RectTransform _canvasRoot;
		[SerializeField] private Transform _backgroundRoot;
		[SerializeField] private Transform _connectorL;
		[SerializeField] private Transform _connectorR;

		// instance variables
		private Timer _minimizationTimer = new Timer( 0.3f );
		private List<MaskableGraphic> _carets = new List<MaskableGraphic>();
		private View.Content _content;
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


		// mono lifecycle
		private void Awake () {

			// set initial style
			_styleRenderer.SetStyle( _style );

			// set content
			_content = new Content( null, _style );

			// set initial data
			if ( _data != null ) {

				HandleTitleChange( null, _data.Title );
				HandleBodyChanged( _data.Body );

				// handle data changes
				_data.OnTitleChanged += HandleTitleChange;
				_data.OnBodyChanged += HandleBodyChanged;
			}

			// read initial toggle values
			HandleEditToggle( _editToggle.isOn );
			HandleMinimizeToggle( _minimizeToggle.isOn );

			// subscribe to toggle changes
			_closeButton.onClick.AddListener( HandleCloseButton );
			_editToggle.onValueChanged.AddListener( HandleEditToggle );
			_minimizeToggle.onValueChanged.AddListener( HandleMinimizeToggle );
		}
		private void Start () {

			_carets = new List<MaskableGraphic>( GetComponentsInChildren<TMP_SelectionCaret>() );
			SetCaretRaycastTarget( isTarget: false );
		}
		private void Update () {

			AnimateMinimization();
			CheckHover();
		}

		// sizing
		private void AnimateMinimization () {

			if ( _minimizationTimer.IsRunning ) {

				var startSize = _windowState switch {
					WindowStates.Minimized => new Vector3( x: 4f, y: 6f, z: 0.2f ),
					WindowStates.Maximized => new Vector3( x: 3f, y: 0.8f, z: 0.2f ),
					_ => Vector3.zero
				};
				var targetSize = _windowState switch {
					WindowStates.Minimized => new Vector3( x: 3f, y: 0.8f, z: 0.2f ),
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

		// internal state management
		private void SetEditState ( EditStates editState ) {

			if ( _editState == editState ) { return; }
			_editState = editState;

			switch ( _editState ) {

				case EditStates.View:

					// update interactable stuff
					SetCaretRaycastTarget( isTarget: false );
					_titleInputField.interactable = false;
					_contentInputField.interactable = false;
					_editImage.sprite = _viewSprite;

					// save data
					_data.Title = _titleInputField.text;
					_data.Body = _contentInputField.text;

					// reformat body
					var content = new Model.Content( userString: _data.Body );
					_content.SetContentModel( content );
					_contentInputField.text = _content.TMPString;
					break;

				case EditStates.Edit:

					// update interactable stuff
					SetCaretRaycastTarget( isTarget: true );
					_titleInputField.interactable = true;
					_contentInputField.interactable = true;
					_editImage.sprite = _editSprite;

					// set text fields to raw data
					_titleInputField.text = _data.Title;
					_contentInputField.text = _data.Body;
					break;
			}
		}
		private void SetWindowState ( WindowStates windowState ) {

			if ( _windowState == windowState ) { return; }
			_windowState = windowState;

			_editToggle.gameObject.SetActive( _windowState == WindowStates.Maximized );
			_minimizationTimer.Start();
		}

		// data event handlers
		private void HandleTitleChange ( string oldTitle, string newTitle ) {

			switch ( _editState ) {
				case EditStates.Edit:
					break;

				case EditStates.View:
					_titleInputField.text = newTitle;
					break;
			}
		}
		private void HandleBodyChanged ( string body ) {

			switch ( _editState ) {
				case EditStates.Edit:
					_contentInputField.text = body;
					break;

				case EditStates.View:
					var content = new Model.Content( userString: body );
					_content.SetContentModel( content );
					_contentInputField.text = _content.TMPString;
					break;
			}
		}

		// ui event handlers
		private void HandleCloseButton () {

			Workspace.Instance.CloseNode( _data.ID );
		}
		private void HandleEditToggle ( bool isOn ) {

			SetEditState( isOn ? EditStates.Edit : EditStates.View );
		}
		private void HandleMinimizeToggle ( bool isOn ) {

			SetWindowState( isOn ? WindowStates.Minimized : WindowStates.Maximized );
		}
		void IPositionChangeHandler.PositionChanged () {
			Debug.Log( "View.Node: PositionChanged" );
			OnPositionChanged?.Invoke( nodeID: _data.ID, newPosition: transform.position );
		}

		// link clicking
		void IPointerClickHandler.OnPointerClick ( PointerEventData eventData ) {

			var charIndex = TMP_TextUtilities.FindIntersectingCharacter( _contentInputField.textComponent, eventData.position, Camera.main, true );
			foreach ( var link in _content.Links ) {
				if ( link.ContainsChar( charIndex ) ) {
					Debug.Log( $"Link Clicked: {link.ID}" );
					Workspace.Instance.OpenNode( nodeID: link.ID, sourceID: _data.ID );
				}
			}
		}


		// hovering stuff
		private bool _isHovering;
		private string _hoveredID;
		void IPointerEnterHandler.OnPointerEnter ( PointerEventData eventData ) {

			_isHovering = true;
		}
		void IPointerExitHandler.OnPointerExit ( PointerEventData eventData ) {

			_isHovering = false;
		}
		private void CheckHover () {

			if ( _isHovering ) {
				var charIndex = TMP_TextUtilities.FindIntersectingCharacter( _contentInputField.textComponent, Input.mousePosition, Camera.main, true );
				var foundLink = false;
				foreach ( var link in _content.Links ) {
					if ( link.ContainsChar( charIndex ) ) {
						if ( link.ID != _hoveredID ) {
							_hoveredID = link.ID;
						}
						foundLink = true;
						break;
					}
				}
				if ( _hoveredID != null && foundLink == false ) {
					_hoveredID = null;
				}
			} else {
				if ( _hoveredID != null ) {
					_hoveredID = null;
				}
			}
		}

	}
}