using UnityEngine;

namespace Framework {

	public enum ScaleUnits {
		Centimeter,
		Inch,
		Meter
	}

	public static class ScaleUnits_Extensions {
		public static float UnitsPerMeter ( this ScaleUnits scaleUnit )
			=> scaleUnit switch {
				ScaleUnits.Centimeter => 100f,
				ScaleUnits.Inch => 39.37f,
				ScaleUnits.Meter => 1f,
				_ => 1f
			};
		public static float MetersPerUnit ( this ScaleUnits scaleUnit )
			=> 1f / scaleUnit.UnitsPerMeter();
	}

	public class Global : MonoBehaviour {


		// ********** singleton management **********

		private static Global _instance;
		public static Global Instance {
			get {
				if ( _instance == null ) {
					_instance = FindObjectOfType<Global>();
					if ( _instance == null ) {
						Debug.LogError( "Global: No Global.Instance found" );
						_instance = new GameObject().AddComponent<Global>();
						_instance.gameObject.name = "Global";
					}
				}
				return _instance;
			}
		}

		// ****************************************

		[Header( "Units" )]
		public ScaleUnits ScaleUnit;
	}
}
