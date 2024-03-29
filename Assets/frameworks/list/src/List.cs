using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Jakintosh.List {

	public abstract class List<TData, TCell> : MonoBehaviour
		where TCell : Cell<TData> {

		// ********** Public Interface **********

		public UnityEvent<TData> OnCellClicked = new UnityEvent<TData>();

		public void SetData ( IList<TData> data ) {

			int i = 0;

			// assign all data to a cell
			if ( data != null ) {
				while ( i < data.Count ) {
					if ( i == _cells.Count ) {
						AddNewCell();
					}
					_cells[i].gameObject.SetActive( true );
					_cells[i].SetData( data[i] );
					i++;
				}
			}

			// disable any remaining cells
			while ( i < _cells.Count ) {
				_cells[i].gameObject.SetActive( false );
				i++;
			}
		}

		protected virtual float GetSpacing () => 4;
		protected virtual RectOffset GetPadding ()
			=> new RectOffset(
				left: 4,
				right: 4,
				top: 4,
				bottom: 4
			);

		// ********** Private Interface **********

		[Header( "Options" )]
		[SerializeField] private bool _flipped = false;

		[Header( "Prefab Resources" )]
		[SerializeField] private TCell _cellPrefab = null;

		// data
		private List<TCell> _cells = new List<TCell>();

		// components
		private VerticalLayoutGroup _verticalLayoutGroup = null;
		private ContentSizeFitter _contentSizeFitter = null;


		private void Awake () {

			// vertical layout group
			_verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
			if ( _verticalLayoutGroup == null ) {
				_verticalLayoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();

				// child constraints
				_verticalLayoutGroup.reverseArrangement = _flipped;
				_verticalLayoutGroup.childAlignment = TextAnchor.UpperLeft;
				_verticalLayoutGroup.childControlWidth = true;
				_verticalLayoutGroup.childForceExpandWidth = true;
				_verticalLayoutGroup.childControlHeight = false;
				_verticalLayoutGroup.childForceExpandHeight = false;

				// spacing
				_verticalLayoutGroup.spacing = GetSpacing();
				_verticalLayoutGroup.padding = GetPadding();
			}


			// size fitter
			_contentSizeFitter = GetComponent<ContentSizeFitter>();
			if ( _contentSizeFitter = null ) {
				_contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();

				_contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
				_contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
			}
		}


		private TCell AddNewCell () {

			var cell = Instantiate<TCell>( _cellPrefab, transform, false );
			cell.OnClick.AddListener( data => OnCellClicked.Invoke( data ) );
			_cells.Add( cell );
			return cell;
		}
	}

}