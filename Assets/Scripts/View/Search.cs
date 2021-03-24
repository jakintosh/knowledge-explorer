using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace View {

	public class Search : MonoBehaviour, IPointerClickHandler {

		[Header( "UI Components" )]
		[SerializeField] private TMP_InputField _searchField;
		[SerializeField] private CanvasGroup _searchText;
		[SerializeField] private SearchResultList _searchResultList;
		[SerializeField] private RectTransform _searchResults;
		[SerializeField] private CanvasGroup _searchResultsGroup;

		[Header( "Components" )]
		[SerializeField] private View.SearchResultCell _searchResultCellPrefab;

		private bool _searchOpen;
		private Timer _searchAnimation;


		private void Awake () {

			_searchAnimation = new Timer( 0.16f );

			_searchField.onValueChanged.AddListener( HandleSearchChanged );
		}
		private void Update () {

			AnimateSearch();
		}

		void IPointerClickHandler.OnPointerClick ( PointerEventData eventData ) {

			HandleSearchClicked();
		}
		private void HandleSearchClicked () {

			_searchOpen = _searchOpen.Toggled();
			_searchAnimation.Start();
		}
		private void HandleSearchChanged ( string searchString ) {

			// set search results active
			// _searchResultList.gameObject.SetActive( !string.IsNullOrEmpty( searchString ) );

			// // populate results
			// var results = Library.Model.Bucket.Instance.SearchTitles( searchString );
			// var nodes = results.Convert( result => result.Result );
			// _searchResultList.SetData( nodes );
		}
		private void AnimateSearch () {

			if ( _searchAnimation.IsRunning ) {

				var startWidth = _searchOpen ? 100f : 600f;
				var endWidth = _searchOpen ? 600f : 100f;

				var rt = _searchField.transform as RectTransform;
				var size = rt.sizeDelta;
				size.x = startWidth.Lerp( to: endWidth, _searchAnimation.Percentage );
				rt.sizeDelta = size;

				var startAlpha = _searchOpen ? 0f : 1f;
				var endAlpha = _searchOpen ? 1f : 0f;
				var currentAlpha = startAlpha.Lerp( to: endAlpha, _searchAnimation.Percentage );
				_searchText.alpha = currentAlpha;
				// _searchResultsGroup.alpha = currentAlpha;

				if ( _searchAnimation.IsComplete ) {
					_searchAnimation.Stop();
					if ( _searchOpen ) {
						// do stuff on finish open
					} else {
						// do stuff on finish close
						_searchField.text = "";
					}
				}
			}
		}

	}
}

