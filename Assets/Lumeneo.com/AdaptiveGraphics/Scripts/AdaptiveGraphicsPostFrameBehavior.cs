/*
 *
 *  Lumeneo.com
 *  AdaptiveGraphics
 *
 *  Coded by Grig Bilham
 *  Questions? grig @ lumeneo.com
 *
 */

using UnityEngine;


namespace Lumeneo
{



	#if UNITY_EDITOR

		[ExecuteInEditMode]
		[RequireComponent(typeof(Camera))]
		[AddComponentMenu("")]

	#endif



	// Post frame behavior
	// This is attached to instantiated camera
	public class AdaptiveGraphicsPostFrameBehavior : MonoBehaviour
	{


		// adaptiveGraphicsManager instance
		// public for objects setting, but not avaiable in the editor
		[HideInInspector]
		public AdaptiveGraphicsManager adaptiveGraphicsManager;


		Material bMatDefault, bMatSharp;


		//
		// Create the private material used for recording at lower resolution
		//
		void OnEnable()
		{
			bMatDefault = Instantiate(Resources.Load<Material>("Materials/AdaptiveGraphicsDefault"));
			bMatDefault.hideFlags = HideFlags.DontSave;
			bMatSharp = Instantiate(Resources.Load<Material>("Materials/AdaptiveGraphicsSharp"));
			bMatSharp.hideFlags = HideFlags.DontSave;
		}


		//
		// Make sure the main component is available before starting
		//
		void Start()
		{
			if (adaptiveGraphicsManager == null) {
				Debug.Log("AdaptiveGraphicsManager not initialized - add AdaptiveGraphics to the main camera.");
			}
		}


		//
		// The Main Event
		// Which is called when the camera finished rendering the scene.
		//
		void OnPostRender()
		{
			Material bMat = adaptiveGraphicsManager.sharpen ? bMatSharp : bMatDefault;
			if (bMat == null || adaptiveGraphicsManager == null || adaptiveGraphicsManager.rt == null || adaptiveGraphicsManager.method == DOWNSAMPLING_METHOD.Disabled) {
				return;
			}
			Graphics.Blit(adaptiveGraphicsManager.rt, null, bMat);
		}


		//
		// Clean up unused vars on disable
		//
		void OnDisable()
		{
			if (bMatDefault != null) {
				DestroyImmediate(bMatDefault);
				bMatDefault = null;
			}
			if (bMatSharp != null) {
				DestroyImmediate(bMatSharp);
				bMatSharp = null;
			}
		}





	}

}