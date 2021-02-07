using System;
using UnityEngine;

[Serializable]
public struct Timer {

	[SerializeField] private double _duration;
	private double _startTime;
	private bool _running;

	public bool IsRunning => _running;
	public double ElapsedTime => Time.time - _startTime;
	public float Percentage => Mathf.Clamp01( (float)( ElapsedTime / _duration ) );
	public bool IsComplete => ElapsedTime > _duration;

	public Timer ( float duration ) {

		_startTime = 0;
		_duration = duration;
		_running = false;
	}

	public void Start () {

		_startTime = Time.time;
		_running = true;
	}
	public void Stop () {

		_running = false;
	}
}