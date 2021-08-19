using Jakintosh.View;
using System.Collections.Generic;
using UnityEngine;

namespace Library.Views {

	public struct Block {

		public string Title;
		public string Body;
	}

	public class BlockEdit : View {

		[SerializeField] private TextEdit.Scroll _scroll;

		[SerializeField] private GameObject _blockPrefab;

		public void SetBlocks ( List<Block> blocks ) {

			blocks.ForEach( block => {

			} );
		}

		protected override void OnInitialize () { }
		protected override void OnCleanup () { }

	}
}