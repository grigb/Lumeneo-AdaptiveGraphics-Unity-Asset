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
using UnityEditor;


namespace Lumeneo
{


    //
    // AdaptiveGraphicsInspector is the inspector panel which doesn't do anything at runtime.
    // The rest of this file is Unity GUI methods
    //

    [CustomEditor(typeof(AdaptiveGraphicsManager))]
    public class AdaptiveGraphicsInspector : Editor
    {

        AdaptiveGraphicsManager _effect;
        Texture2D _headerTexture;
        GUIStyle titleLabelStyle;
        Color titleColor;

        // Set it up
        void OnEnable()
        {
            titleColor = EditorGUIUtility.isProSkin ? new Color(0.52f, 0.66f, 0.9f) : new Color(0.12f, 0.16f, 0.4f);
            _headerTexture = Resources.Load<Texture2D>("AdaptiveGraphicsHeader");
            _effect = (AdaptiveGraphicsManager)target;
            _effect.repaintInspectorAction = RefreshInspector;
        }

        // Clean memory
        void OnDisable()
        {
            if (_effect != null)
                _effect.repaintInspectorAction = null;
        }


        // Update the screen
        void RefreshInspector()
        {
            Repaint();
        }
       


        //
        // Build the Inspector
        //
        public override void OnInspectorGUI()
        {


            if (_effect == null)
                return;
            
            
            _effect.isDirty = false;

            EditorGUILayout.Separator();
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label(_headerTexture, GUILayout.ExpandWidth(true));
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;


            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();


                if (_effect.niceFPSisActive)
                {
                    GUILayout.Label("Current quality: 100% (fps > niceFPS)");
                }
                else
                {
                    GUILayout.Label("Current quality: " + Mathf.RoundToInt(_effect.appliedDownsampling * 100f) + "%");
                }


                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            DrawLabel("General Settings");


            if (GUILayout.Button("Help", GUILayout.Width(50)))
            {
                EditorUtility.DisplayDialog("AdaptiveGraphics", "AdaptiveGraphics reduces lag and attempts to maintain the target frame rate by adapting the graphics settings and shaders.\n\nAdaptiveGraphics is very customizable so you can set the minimum acceptable quality to a high value, like 0.7 or 0.8 to get extra FPS without loosing too much quality, or choose a lower minimum quality to get the best FPS at any moment.\n\nMove the mouse over a setting for a short description.\n\nRead the PDF documentation for the details.", "Ok");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Method", "Select the operation method."), GUILayout.Width(110));
            _effect.method = (DOWNSAMPLING_METHOD)EditorGUILayout.EnumPopup(_effect.method);
            EditorGUILayout.EndHorizontal();



            switch (_effect.method)
            {
                case DOWNSAMPLING_METHOD.HorizontalDownsampling:
                    EditorGUILayout.HelpBox("Reduces the width of the render target, perform the rendering and scales back the image to full window width. The amount of reduction is specified with the quality parameter.", MessageType.Info);
                    break;

                case DOWNSAMPLING_METHOD.QuadDownsampling:
                    EditorGUILayout.HelpBox("Reduces both the width and height of the render target, perform the rendering and scales back the image to full window size. The amount of reduction is specified with the quality parameter.", MessageType.Info);
                    break;

                case DOWNSAMPLING_METHOD.AdaptativeDownsampling:
                    EditorGUILayout.HelpBox("Reduces the size of the render target depending on real and desired FPS, perform the rendering and scales back the image to full window size. The amount of reduction is dynamically adjusted based on parameters below.", MessageType.Info);
                    break;
            }



            if (_effect.method != DOWNSAMPLING_METHOD.Disabled)
            {

                // Check for antialias in the main camera
                MonoBehaviour[] components = _effect.GetComponents<MonoBehaviour>();


                if (components != null)
                {
                    for (int k = 0; k < components.Length; k++)
                    {
                        if (components[k] == null || !components[k].enabled)
                            continue;
                        string name = components[k].GetType().Name.ToUpper();
                        if (name.IndexOf("ANTIALIAS") >= 0)
                        {
                            EditorGUILayout.HelpBox("An antialias component was found on this camera. Disable it and use integrated MSAA instead (antialias slider below).", MessageType.Warning);
                        }
                        else if (name.IndexOf("NOISE") >= 0)
                        {
                            EditorGUILayout.HelpBox("A noise component was found on this camera. Please disable it or add it AdaptiveGraphicsCamera instead (only available with render methods that use a second camera - see below).", MessageType.Warning);
                        }
                        else if (name.IndexOf("BEAUTIFY") >= 0)
                        {
                            EditorGUILayout.HelpBox("Beautify component was found on this camera. Please disable it or add it to AdaptiveGraphicsCamera instead (only available with render methods that use a second camera - see below).", MessageType.Warning);
                        }
                    }
                }



                if (_effect.method == DOWNSAMPLING_METHOD.AdaptativeDownsampling)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("Minimum FPS", "Minimum desired FPS during playmode."), GUILayout.Width(110));
                    _effect.targetFPS = EditorGUILayout.IntSlider(_effect.targetFPS, 10, 120);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("Enable Nice FPS", "Enables or disabled Nice FPS feature. If FPS exceeds this value, AdaptiveGraphics will be disabled. If FPS goes down, AdaptiveGraphics will be enabled again. Pump up this value to the max if you don't want AdaptiveGraphics to be disabled."), GUILayout.Width(110));
                    _effect.niceFPSEnabled = EditorGUILayout.Toggle(_effect.niceFPSEnabled, GUILayout.Width(20));
                    if (_effect.niceFPSEnabled)
                    {
                        _effect.niceFPS = EditorGUILayout.IntSlider(_effect.niceFPS, 15, 120);
                    }
                    EditorGUILayout.EndHorizontal();
                }



                EditorGUILayout.Separator();
                EditorGUILayout.BeginHorizontal();
                DrawLabel("Quality Management Settings");
                EditorGUILayout.EndHorizontal();



                if (_effect.method == DOWNSAMPLING_METHOD.AdaptativeDownsampling)
                {

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("Adapt Speed Up", "A value that defines how fast FPS will be incremented or decremented to keep up with target FPS."), GUILayout.Width(110));
                    _effect.fpsChangeSpeedUp = EditorGUILayout.Slider(_effect.fpsChangeSpeedUp, 0.01f, 0.1f);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("Adapt Speed Down", "A value that defines how fast FPS will be decremented to keep up with target FPS."), GUILayout.Width(110));
                    _effect.fpsChangeSpeedDown = EditorGUILayout.Slider(_effect.fpsChangeSpeedDown, 0.01f, 0.1f);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("Minimum Quality", "Minimum acceptable quality level to enforce FPS rate."), GUILayout.Width(110));
                    _effect.downsampling = EditorGUILayout.Slider(_effect.downsampling, 0.1f, 1f);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("Static Camera Quality", "Quality level used when camera is static to enforce FPS rate. You may want to have a higher quality value when camera is not moving or rotating. When camera moves or rotates the minimum quality is used instead."), GUILayout.Width(120));
                    _effect.staticCameraDownsampling = EditorGUILayout.Slider(_effect.staticCameraDownsampling, 0.1f, 1f);
                    EditorGUILayout.EndHorizontal();

                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("Quality", "This value determines the amount of detail loss or downsampling applied (1=no loss/no effect, 0.5=half reduction, ...)"), GUILayout.Width(110));
                    _effect.downsampling = EditorGUILayout.Slider(_effect.downsampling, 0.1f, 1f);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("Static Camera Quality", "Quality level used when camera is static. You may want to have a higher quality value when camera is not moving or rotating. When camera moves or rotates the normal quality is used instead."), GUILayout.Width(120));
                    _effect.staticCameraDownsampling = EditorGUILayout.Slider(_effect.staticCameraDownsampling, 0.1f, 1f);
                    EditorGUILayout.EndHorizontal();
                }



                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Reduce Pixel Lights", "Reduce pixel light count as FPS decrease."), GUILayout.Width(110));
                _effect.reducePixelLights = EditorGUILayout.Toggle(_effect.reducePixelLights);
                EditorGUILayout.EndHorizontal();


                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Manage Shadows", "Choose the filtering used when upscaling the frame: nearest neighbour (fast, pixelates) or bilinear (smoother, blurs)."), GUILayout.Width(110));
                _effect.manageShadows = EditorGUILayout.Toggle(_effect.manageShadows);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Separator();
                EditorGUILayout.BeginHorizontal();
                DrawLabel("Rendering Settings");
                EditorGUILayout.EndHorizontal();


                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Filtering", "Use nearest neighbour to produce a pixelate effect."), GUILayout.Width(110));
                _effect.filtering = (FILTERING_MODE)EditorGUILayout.EnumPopup(_effect.filtering);
                EditorGUILayout.EndHorizontal();


                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Antialiasing", "Enable antialias on demand. Level of antialias depends on FPS."), GUILayout.Width(110));
                _effect.antialias = EditorGUILayout.IntSlider(_effect.antialias, 1, 4);


                if (_effect.antialias == 1)
                {
                    EditorGUILayout.LabelField("(Off)", GUILayout.Width(30));
                }
                else if (_effect.rt != null)
                {
                    EditorGUILayout.LabelField(string.Format("x{0}", _effect.rt.antiAliasing), GUILayout.Width(30));
                }


                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Clear Flags", "AdaptiveGraphics's camera clear flags. Set this to Color or Solid Color if you experience ghosting artifacts."), GUILayout.Width(110));
                _effect.cameraClearFlags = (CameraClearFlags)EditorGUILayout.EnumPopup(_effect.cameraClearFlags);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Render Method", "Compositing mode. Simple uses OnGUI event to draw the upscaled frame. SecondCameraBlit uses a second camera and a post-image effect to upscale the frame. SecondCameraBillboard uses a second camera and a simple billboard to draw the frame."), GUILayout.Width(110));
                _effect.compositingMethod = (COMPOSITING_METHOD)EditorGUILayout.EnumPopup(_effect.compositingMethod);
                EditorGUILayout.EndHorizontal();



                if (_effect.compositingMethod == COMPOSITING_METHOD.SecondCameraBlit)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("Sharpen Image", "Apply a sharpen filter to enhance details of the upscaled image and reduce blur. You may deactivate this option to slightly improve the performance on mobile."), GUILayout.Width(110));
                    _effect.sharpen = EditorGUILayout.Toggle(_effect.sharpen);
                    EditorGUILayout.EndHorizontal();
                }


                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Prewarm", "Generates target buffers during initialization. If this value is false, target buffers are created on demand. Most of the cases you can leave this option unchecked."), GUILayout.Width(110));
                _effect.prewarm = EditorGUILayout.Toggle(_effect.prewarm);
                EditorGUILayout.EndHorizontal();

            }

            if (_effect.isDirty)
            {
                EditorUtility.SetDirty(target);
            }


        } // End of OnInspectorGUI



        //
        // Draw Label
        //
        void DrawLabel(string s)
        {


            if (titleLabelStyle == null)
            {
                titleLabelStyle = new GUIStyle(GUI.skin.label);
            }


            titleLabelStyle.normal.textColor = titleColor;
            GUILayout.Label(s, titleLabelStyle);


        }



    }

}
