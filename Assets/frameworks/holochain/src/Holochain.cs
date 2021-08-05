using NativeWebSocket;
using SouthPointe.Serialization.MessagePack;
using System;
using UnityEngine;

namespace Holochain {

	static public class AgentPubKey {
		private static string _agentPubKey = "uhCAkt_cNGyYJZIp08b2ZzxoE6EqPndRPb_WwjVkM_mOBcFyq7zCw";
		public static byte[] GetBytes () => System.Text.Encoding.UTF8.GetBytes( _agentPubKey );
		public static string Get () => _agentPubKey;
	}
	static public class DNAHash {
		private static string _dnaHash = "uhC0kTMixTG0lNZCF4SZfQMGozf2WfjQht7E06_wy3h29-zPpWxPQ";
		public static byte[] GetBytes () => System.Text.Encoding.UTF8.GetBytes( _dnaHash );
		public static string Get () => _dnaHash;
	}
	static public class CellID {
		public static string[] Get ()
			=> Create( DNAHash.Get(), AgentPubKey.Get() );
		public static byte[][] GetBytes ()
			=> CreateBytes( DNAHash.GetBytes(), AgentPubKey.GetBytes() );
		public static string[] Create ( string dnaHash, string agentPubKey )
			=> new string[] { dnaHash, agentPubKey };
		public static byte[][] CreateBytes ( byte[] dnaHash, byte[] agentPubKey )
			=> new byte[2][] { dnaHash, agentPubKey };
	}


	// holochain conductor data types

	[Serializable]
	public struct InstalledCell {
		public byte[][] cell_id;
		public string cell_nick;
	}

	[Serializable]
	public struct InstalledAppInfo {

		public string installed_app_id;
		public InstalledCell[] cell_data;
	}

	[Serializable]
	public class RustEnum<T> {
		public string type;
		public T data;

		public RustEnum ( string type, T data ) {
			this.type = type;
			this.data = data;
		}
	}

	[Serializable]
	public class WireMessage : RustEnum<byte[]> {

		public long id;

		public WireMessage (
			string type,
			byte[] data,
			long id
		) : base( type, data ) {
			this.id = id;
		}

		public override string ToString () => $"{{\n  type: \"{type}\",\n  id: {id},\n  data: {System.Text.Encoding.UTF8.GetString( data )}\n}}";
	}

	[Serializable]
	public class ConductorRequest : WireMessage {

		public ConductorRequest ( byte[] data, long id )
			: base( type: "Request", data: data, id: id ) { }
	}

	[Serializable]
	public class ZomeCallRequest : RustEnum<ZomeCallRequest.Data> {

		[Serializable]
		public struct Data {
			public string[] cell_id;
			public string zome_name;
			public string fn_name;
			public byte[] payload;
			public byte[] cap;
			public string provenance;
		}

		public ZomeCallRequest (
			string dnaHash,
			string agentPubKey,
			string zomeName,
			string functionName,
			byte[] payload,
			string provenance
		) : base(
			type: "zome_call",
			data: new Data {
				cell_id = CellID.Create( dnaHash, agentPubKey ),// CellID.Create( dnaHash, agentPubKey ),
				zome_name = zomeName,
				fn_name = functionName,
				payload = payload,
				cap = null,
				provenance = provenance
			}
		) { }
	}

	[Serializable]
	public class ConductorResponse {
		public string type;
		public long id;
		public byte[] data;
		public override string ToString () => $"{{\n  type: \"{type}\",\n  id: {id},\n  data: {System.Text.Encoding.UTF8.GetString( data )}\n}}";
	}

	[Serializable]
	public class ZomeCallResponse {
		public string type;
		public byte[] data;
		public override string ToString () => $"{{\n  type: \"{type}\",\n  data: {System.Text.Encoding.UTF8.GetString( data )}\n}}";
	}

	public abstract class Conductor {

		// public methods
		public async void OpenConnection ( int port ) {

			var url = $"ws://localhost:{port}";
			websocket = new WebSocket( url );
			websocket.OnOpen += OnOpen;
			websocket.OnMessage += OnMessage;
			websocket.OnError += OnError;
			websocket.OnClose += OnClose;
			Debug.Log( $"Holochain: Opening connection to conductor at `{url}`" );
			await websocket.Connect();
			await websocket.Receive();
		}
		public void DispatchMessageQueue () {
#if !UNITY_WEBGL || UNITY_EDITOR
			websocket.DispatchMessageQueue();
#endif
		}
		public async void CloseConnection () {

			await websocket.Close();
		}

		// internal requests
		private long _requestId = 1;
		protected WebSocket websocket;
		protected async void SendRequest ( byte[] payload ) {

			var formatter = new MessagePackFormatter();
			var request = new ConductorRequest(
				id: _requestId++,
				data: payload
			);
			var packedRequest = formatter.Serialize( request );

			Debug.Log( $"Holochain: Sending request\n{request.ToString()}" );
			await websocket.Send( packedRequest );
			Debug.Log( $"Holochain: Conductor sent request\n{System.Text.Encoding.UTF8.GetString( packedRequest )}" );
		}

		// protected handlers
		protected abstract void OnOpen ();
		protected abstract void OnMessage ( byte[] bytes );
		protected abstract void OnError ( string error );
		protected abstract void OnClose ( WebSocketCloseCode code );
	}

	public class AdminConductor : Conductor {
		public AdminConductor () => OpenConnection( port: 63125 );
		protected override void OnOpen () => Debug.Log( "Holochain: AdminConductor websocket connection opened" );
		protected override void OnMessage ( byte[] bytes ) => Debug.Log( $"Holochain: AdminConductor received bytes:\n{bytes}" );
		protected override void OnError ( string error ) => Debug.Log( $"Holochain: AdminConductor received error:\n{error}" );
		protected override void OnClose ( WebSocketCloseCode code ) => Debug.Log( $"Holochain: AdminConductor closed with code {code}" );
	}

	public class AppConductor : Conductor {

		public UnityEngine.Events.UnityEvent<byte[]> OnResponse = new UnityEngine.Events.UnityEvent<byte[]>();

		// constructor
		public AppConductor ( string dnaHash, string agentPubKey ) {

			this.dnaHash = dnaHash;
			this.agentPubKey = agentPubKey;
			OpenConnection( port: 8888 );
		}

		// public methods
		public void CallFunction ( string zome, string function, byte[] payload ) {

			if ( websocket.State != WebSocketState.Open ) {
				Debug.Log( $"Holochain: AppConductor couldn't call `{zome}/{function},` websocket connection not open" );
				return;
			}

			var formatter = new MessagePackFormatter();
			var zomeCallPayload = formatter.Serialize<ZomeCallRequest>(
				new ZomeCallRequest(
					dnaHash: dnaHash,
					agentPubKey: agentPubKey,
					zomeName: zome,
					functionName: function,
					payload: payload,
					provenance: agentPubKey
				)
			);
			SendRequest( zomeCallPayload );
		}

		// event handlers
		protected override void OnOpen ()
			=> Debug.Log( "Holochain: AppConductor websocket connection opened" );
		protected override void OnError ( string error )
			=> Debug.Log( $"Holochain: AppConductor received error: {error}" );
		protected override void OnClose ( WebSocketCloseCode code )
			=> Debug.Log( $"Holochain: AppConductor closed with code {code}" );
		protected override void OnMessage ( byte[] bytes ) {

			try {

				var formatter = new MessagePackFormatter();
				Debug.Log( $"Holochain: AppConductor received msgpack bytes: {System.Text.Encoding.UTF8.GetString( bytes )}" );

				var response = formatter.Deserialize<ConductorResponse>( bytes );
				Debug.Log( $"Holochain: AppConductor received response:\n{response}" );

				try {

					var zomeResponse = formatter.Deserialize<ZomeCallResponse>( response.data );
					Debug.Log( $"Holochain: AppConductor received content:\n{zomeResponse}" );
					OnResponse?.Invoke( zomeResponse.data );

				} catch { Debug.Log( "Holochain: Couldn't deserialize ConductorResponse bytes into ZomeCallResponse" ); }
			} catch { Debug.Log( "Holochain: Couldn't deserialize received message bytes into ConductorResponse" ); }
		}

		// private data
		private string dnaHash;
		private string agentPubKey;
	}
}

