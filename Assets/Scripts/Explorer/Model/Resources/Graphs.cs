using Framework.Data;
using System.Collections.Generic;

using Metadata = Framework.Data.Metadata.Resource;

namespace Explorer.Model {

	public interface IGraphCRUD {

		event Framework.Event<IList<Metadata>>.Signature OnMetadataChanged;

		Metadata New ( string name );
		bool Delete ( string uid );

		Knowledge.Graph Get ( string uid );
		IList<Metadata> GetAll ();

		bool ValidateName ( string name );
	}
	public class GraphResources : IGraphCRUD {

		// ********** Public Interface **********

		public event Framework.Event<IList<Metadata>>.Signature OnMetadataChanged;

		public GraphResources ( string rootDataPath ) {

			_graphs = new Resources<Metadata, Knowledge.Graph>(
				resourcePath: $"{rootDataPath}/graph",
				resourceExtension: "graph",
				uidLength: 6
			);

			// pass through event
			_graphs.OnMetadataChanged += metadata => OnMetadataChanged?.Invoke( metadata );
		}

		public void LoadMetadata () {
			_graphs.LoadMetadataFromDisk();
		}
		public void Close () {
			_graphs.Close();
		}

		public Metadata New ( string name ) {

			// try create graph
			Metadata graphMetadata;
			Knowledge.Graph graphResource;
			try {

				(graphMetadata, graphResource) = _graphs.New( name );

			} catch ( ResourceNameEmptyException ) {
				UnityEngine.Debug.LogError( "Model.Application.IGraphAPI.Create: Failed due to Resource Name Empty" );
				return null;

			} catch ( ResourceNameConflictException ) {
				UnityEngine.Debug.LogError( "Model.Application.IGraphAPI.Create: Failed due to Resource Name Conflict" );
				return null;
			}

			graphResource.Initialize( graphMetadata.UID );

			return graphMetadata;
		}
		public bool Delete ( string uid ) {

			return _graphs.Delete( uid );
		}
		public Knowledge.Graph Get ( string uid ) {

			if ( uid == null ) {
				return null;
			}

			try {

				return _graphs.RequestResource( uid, load: true );

			} catch ( ResourceMetadataNotFoundException ) {
				UnityEngine.Debug.LogError( "Model.Application.IGraphAPI.Read: Failed due to missing graph." );
				return null;
			}
		}
		public IList<Metadata> GetAll () {

			return _graphs.GetAllMetadata();
		}

		public bool ValidateName ( string name ) {

			if ( string.IsNullOrEmpty( name ) ) return false;
			return _graphs.NameIsUnique( name );
		}


		// ********** Private Interface **********

		private Resources<Metadata, Knowledge.Graph> _graphs;
	}
}