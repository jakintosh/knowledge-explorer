using Framework;
using Framework.Data;
using System.Collections.Generic;
using UnityEngine;

using Metadata = Framework.Data.Metadata.Resource;

namespace Explorer {

	public interface ISubsystem {
		void Initialize ();
		void Teardown ();
	}
	public class SubsystemStack {

		public SubsystemStack () {

			_subsystems = new Stack<ISubsystem>();
		}
		public void Push ( ISubsystem subsystem ) {

			subsystem.Initialize();
			_subsystems.Push( subsystem );
		}
		public void Teardown () {

			while ( _subsystems.Count > 0 ) {
				_subsystems.Pop().Teardown();
			}
		}

		private Stack<ISubsystem> _subsystems;
	}

	public class Application : MonoBehaviour {

		// ********** Public Interface **********

		public static Model.Resources Resources => Instance._resources;
		public static Model.State State => Instance._state;

		// ********** Private Interface **********

		// subsystems
		private SubsystemStack _subsystems;
		private Model.Resources _resources;
		private Model.State _state;

		private void Initialize () {

			_subsystems = new SubsystemStack();
			_resources = new Model.Resources();
			_state = new Model.State();

			_subsystems.Push( _resources );
			_subsystems.Push( _state );
		}
		private void SetupUI () {

		}
		private void TeardownUI () {

		}
		private void Teardown () {

			_subsystems.Teardown();
		}


		// lifecycle/mono/singleton
		private static Application _instance;
		private static Application Instance {
			get {
				if ( _instance == null ) {
					_instance = new GameObject( "Application" ).AddComponent<Application>();
				}
				return _instance;
			}
		}
		private void Awake () {

			if ( _instance == null ) {
				_instance = this;
				Initialize();
				SetupUI();
			} else {
				Destroy( gameObject );
				return;
			}
		}
		private void OnDestroy () {
			if ( _instance == this ) {
				TeardownUI();
				Teardown();
			}
		}
	}

}


namespace Explorer.View {

	public abstract class View : MonoBehaviour {

		protected abstract void Init ();
		protected void Init ( View view ) {
			view.Init();
		}
	}

	public abstract class RootView : View {

		private void Awake () {
			Init();
		}
	}
}


namespace Explorer.Model {

	// allows access to the application's state
	public class State : ISubsystem {

		public Contexts Contexts => _contexts;

		public State () {

			_contexts = new Contexts();
		}
		public void Initialize () {

			// TODO: make this load something
			_contexts.NewContext();
		}
		public void Teardown () { }

		// internal data
		private Contexts _contexts;
	}

	// manages the set of contexts of the application
	public class Contexts {

		// events
		public event Framework.Event<Context>.Signature OnCurrentContextChanged;

		// properties
		public Context Current => _current.Get();

		public Contexts () {

			_contexts = new Dictionary<string, Context>();

			// init observables
			_current = new Observable<Context>(
				initialValue: null,
				onChange: context => {
					Framework.Event<Context>.Fire(
						@event: OnCurrentContextChanged,
						value: context,
						id: $"Explorer.Model.Contexts.OnCurrentContextChanged"
					);
				}
			);
		}
		public string NewContext ( bool setToCurrent = true ) {

			var uid = GetUID();
			var context = new Context( uid );
			_contexts.Add( uid, context );

			if ( setToCurrent ) {
				SetCurrentContext( uid );
			}

			return uid;
		}
		public void SetCurrentContext ( string uid ) {

			var context = _contexts[uid];
			_current.Set( context );
		}

		// internal data
		private Dictionary<string, Context> _contexts;
		private Observable<Context> _current;

		// private helpers
		private string GetUID () {

			return StringHelpers.UID.Generate(
				length: 4,
				validateUniqueness: candidate => _contexts.KeyIsUnique( candidate )
			);
		}
	}

	// an instance of the application
	public class Context : IdentifiableResource {

		// events
		public event Framework.Event<Workspace>.Signature OnWorkspaceChanged;

		// properties
		public Workspace Workspace => _workspace.Get();
		public KnowledgeGraph Graph => _graph;

		public Context ( string uid ) : base( uid ) {

			// init observables
			_workspace = new Observable<Workspace>(
				initialValue: null,
				onChange: workspace => {
					Framework.Event<Workspace>.Fire(
						@event: OnWorkspaceChanged,
						value: workspace,
						id: $"Explorer.Model.Context.OnWorkspaceChanged(to: {workspace?.Name ?? "null"})"
					);
				}
			);
		}
		public void SetWorkspace ( string uid ) {

			if ( uid != null ) {

				// get workspace
				_workspace.Set( Application.Resources.Workspaces.Get( uid ) );

				// load graph needed by workspace
				_graph = Application.Resources.Graphs.Get( _workspace.Get().GraphUID );

			} else {
				_workspace.Set( null );
			}
		}

		// internal data
		private Observable<Workspace> _workspace;
		private KnowledgeGraph _graph;
	}

	public class Resources : ISubsystem {

		// ********** Public Interface **********

		public IGraphCRUD Graphs => _graphs;
		public IWorkspaceCRUD Workspaces => _workspaces;

		public Resources () {

			_graphs = new GraphResources(
				rootDataPath: LocalDataPath
			);

			_workspaces = new WorkspaceResources(
				rootDataPath: LocalDataPath,
				graphs: _graphs
			);
		}

		public void Initialize () {

			_graphs.LoadMetadata();
			_workspaces.LoadMetadata();
		}
		public void Teardown () {

			_graphs.Close();
			_workspaces.Close();
		}


		// ********** Private Interface **********

		private string LocalDataPath => $"/data/local";

		private GraphResources _graphs;
		private WorkspaceResources _workspaces;
	}

	public interface IGraphCRUD {

		event Framework.Event<IList<Metadata>>.Signature OnMetadataChanged;

		Metadata New ( string name );
		bool Delete ( string uid );

		KnowledgeGraph Get ( string uid );
		IList<Metadata> GetAll ();

		bool ValidateName ( string name );
	}
	public class GraphResources : IGraphCRUD {

		// ********** Public Interface **********

		public event Framework.Event<IList<Metadata>>.Signature OnMetadataChanged;

		public GraphResources ( string rootDataPath ) {

			_graphs = new Resources<Metadata, KnowledgeGraph>(
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
			KnowledgeGraph graphResource;
			try {

				(graphMetadata, graphResource) = _graphs.New( name );

			} catch ( ResourceNameEmptyException ) {
				Debug.LogError( "Model.Application.IGraphAPI.Create: Failed due to Resource Name Empty" );
				return null;

			} catch ( ResourceNameConflictException ) {
				Debug.LogError( "Model.Application.IGraphAPI.Create: Failed due to Resource Name Conflict" );
				return null;
			}

			graphResource.FirstInitialization(); // TODO: can we make this also some resource management thing?

			return graphMetadata;
		}
		public bool Delete ( string uid ) {

			return _graphs.Delete( uid );
		}
		public KnowledgeGraph Get ( string uid ) {

			if ( uid == null ) {
				return null;
			}

			try {

				return _graphs.RequestResource( uid, load: true );

			} catch ( ResourceMetadataNotFoundException ) {
				Debug.LogError( "Model.Application.IGraphAPI.Read: Failed due to missing graph." );
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

		private Resources<Metadata, KnowledgeGraph> _graphs;
	}

	public interface IWorkspaceCRUD {

		event Framework.Event<IList<Metadata>>.Signature OnMetadataChanged;

		Metadata New ( string name, string graphID = null );
		bool Delete ( string uid );

		Workspace Get ( string uid );
		IList<Metadata> GetAll ();

		bool ValidateName ( string name );
	}
	public class WorkspaceResources : IWorkspaceCRUD {

		// ********** Public Interface **********

		public event Framework.Event<IList<Metadata>>.Signature OnMetadataChanged;

		public WorkspaceResources ( string rootDataPath, GraphResources graphs ) {

			_workspaces = new Resources<Metadata, Workspace>(
				resourcePath: $"{rootDataPath}/workspace",
				resourceExtension: "workspace",
				uidLength: 6
			);

			// pass through event
			_workspaces.OnMetadataChanged += metadata => OnMetadataChanged?.Invoke( metadata );
			_graphs = graphs;
		}

		public void LoadMetadata () {
			_workspaces.LoadMetadataFromDisk();
		}
		public void Close () {
			_workspaces.Close();
		}

		public Metadata New ( string name, string graphID ) {

			// try create workspace
			Metadata workspaceMetadata;
			Workspace workspaceResource;
			try {

				(workspaceMetadata, workspaceResource) = _workspaces.New( name );

			} catch ( ResourceNameEmptyException ) {
				Debug.LogError( "Model.Application.IWorkspaceAPI.Create: Failed due to empty resource name" );
				return null;
			} catch ( ResourceNameConflictException ) {
				Debug.LogError( "Model.Application.IWorkspaceAPI.Create: Failed due to name conflict" );
				return null;
			}

			// if graph doesn't exist, try to create it
			if ( graphID == null ) {
				var graphMetadata = _graphs.New( name );
				graphID = graphMetadata?.UID;
			}

			// try get graph
			KnowledgeGraph graphResource;
			try {

				graphResource = _graphs.Get( graphID );

			} catch ( ResourceMetadataNotFoundException ) {
				Debug.LogError( "Model.Application.IWorkspaceAPI.Create: Failed due to missing graph. Deleting created workspace and aborting." );
				Delete( workspaceMetadata.UID );
				return null;
			}

			// init workspace
			workspaceResource.Initialize(
				uid: workspaceMetadata.UID,
				graphUid: graphID,
				name: workspaceMetadata.Name
			);

			// return metadata
			return workspaceMetadata;
		}
		public bool Delete ( string uid ) {

			return _workspaces.Delete( uid );
		}

		public Workspace Get ( string uid ) {

			if ( uid == null ) {
				return null;
			}

			try {

				var workspaceResource = _workspaces.RequestResource( uid, load: true );

				// // try get graph
				// KnowledgeGraph graphResource;
				// try {

				// 	// TODO: this should be using some kind of resource dependency system
				// 	graphResource = _graphs.Get( workspaceResource.GraphUID );

				// } catch ( System.Exception ex ) {
				// 	Debug.LogError( $"Model.Application.IWorkspaceAPI.Read: Failed due to {ex.Message}. Deleting workspace and aborting." );
				// 	Delete( uid );
				// 	_graphs.Delete( workspaceResource.GraphUID );
				// 	return null;
				// }

				return workspaceResource;

			} catch ( ResourceMetadataNotFoundException ) {

				Debug.LogError( "Model.Application.IWorkspaceAPI.Read: Failed due to missing workspace." );
				return null;
			}
		}
		public IList<Metadata> GetAll () {

			return _workspaces.GetAllMetadata();
		}

		public bool ValidateName ( string name ) {

			if ( string.IsNullOrEmpty( name ) ) return false;
			return _workspaces.NameIsUnique( name );
		}


		// ********** Private Interface **********

		private GraphResources _graphs;
		private Resources<Metadata, Workspace> _workspaces;
	}
}