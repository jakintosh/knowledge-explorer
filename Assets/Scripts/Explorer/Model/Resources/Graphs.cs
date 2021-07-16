using Jakintosh.Knowledge;
using Jakintosh.Resources;
using System.Collections.Generic;

namespace Explorer.Model {

	public interface IGraphCRUD {

		event Framework.Event<IList<Metadata>>.Signature OnMetadataChanged;
		event Framework.Event<Metadata>.Signature OnMetadataAdded;
		event Framework.Event<Metadata>.Signature OnMetadataUpdated;
		event Framework.Event<Metadata>.Signature OnMetadataDeleted;

		Metadata New ( string name );
		bool Rename ( string uid, string name );
		bool Delete ( string uid );

		Graph Get ( string uid );
		IList<Metadata> GetAll ();

		bool ValidateName ( string name );
	}
	public class GraphResources : IGraphCRUD {

		// ********** Public Interface **********

		public event Framework.Event<IList<Metadata>>.Signature OnMetadataChanged;
		public event Framework.Event<Metadata>.Signature OnMetadataAdded;
		public event Framework.Event<Metadata>.Signature OnMetadataUpdated;
		public event Framework.Event<Metadata>.Signature OnMetadataDeleted;

		public GraphResources ( string rootDataPath ) {

			_graphs = new Resources<Metadata, Graph>(
				resourcePath: $"{rootDataPath}/graph",
				resourceExtension: "graph",
				uidLength: 6
			);

			// pass through event
			_graphs.OnAnyMetadataChanged += metadata => OnMetadataChanged?.Invoke( metadata );
			_graphs.OnMetadataAdded += metadata => OnMetadataAdded?.Invoke( metadata );
			_graphs.OnMetadataUpdated += metadata => OnMetadataUpdated?.Invoke( metadata );
			_graphs.OnMetadataDeleted += metadata => OnMetadataDeleted?.Invoke( metadata );
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
			Graph graphResource;
			try {

				(graphMetadata, graphResource) = _graphs.New( name );

			} catch ( ResourceNameEmptyException ) {
				UnityEngine.Debug.LogError( "Model.Application.GraphResources.Create: Failed due to Resource Name Empty" );
				return null;

			} catch ( ResourceNameConflictException ) {
				UnityEngine.Debug.LogError( "Model.Application.GraphResources.Create: Failed due to Resource Name Conflict" );
				return null;
			}

			graphResource.Initialize( graphMetadata.UID );

			return graphMetadata;
		}
		public bool Rename ( string uid, string name ) {

			try {

				return _graphs.Rename( uid, name );

			} catch ( ResourceNameEmptyException ) {
				UnityEngine.Debug.LogError( "Model.Application.GraphResources.Rename: Failed due to Resource Name Empty" );
			} catch ( ResourceNameConflictException ) {
				UnityEngine.Debug.LogError( "Model.Application.GraphResources.Rename: Failed due to Resource Name Conflict" );
			} catch ( ResourceFileNameConflictException ) {
				UnityEngine.Debug.LogError( "Model.Application.GraphResources.Rename: Failed due to Resource File Name Conflict, but not Metadata name conflict. Data may be corrupted." );
			}
			return false;
		}
		public bool Delete ( string uid ) {

			return _graphs.Delete( uid );
		}
		public Graph Get ( string uid ) {

			if ( uid == null ) {
				return null;
			}

			try {

				return _graphs.RequestResource( uid, load: true );

			} catch ( ResourceMetadataNotFoundException ) {
				UnityEngine.Debug.LogError( "Model.Application.GraphResources.Read: Failed due to missing graph." );
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

		private Resources<Metadata, Graph> _graphs;
	}
}