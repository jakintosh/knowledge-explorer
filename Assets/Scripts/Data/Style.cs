using System;
using UnityEngine;

[CreateAssetMenu( menuName = "Data/Style", fileName = "New Style" )]
public class Style : ScriptableObject {
	public Color Foreground;
	public Color Background;
	public Color Accent;
}