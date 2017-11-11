using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public class PlatformTile : Tile {

	#if UNITY_EDITOR
	[MenuItem("Assets/Create/Custom Tiles/Platform")]

	public static void createPlatformTile()
	{
		string path = EditorUtility.SaveFilePanelInProject ("Save Platform Tile", "new_platform_tile", "asset", "Save Plaform Tile", "Assets");

		AssetDatabase.CreateAsset (ScriptableObject.CreateInstance<PlatformTile> (), path);
	}

	#endif
}
