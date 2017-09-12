# AdaptiveGraphics

AdaptiveGraphics consists of a camera component, custom editor interface and shaders that improve performance of Unity 3D apps.

The technique used by AdaptiveGraphics works in two steps:
1. Makes the camera render to an off-screen surface with reduced resolution which makes the render faster and also reduces any post-image effects time.

2. Upscales the rendered frame to window size, optionally applying MSAA antialiasing + a custom fast sharpen algorithm, which reduces blur and improves result vs a normal upscale operation.


# Quick Start
To use AdaptiveGraphics:
1. Import the AdaptiveGraphics package into your project.
2. Select your main camera and select Component => Rendering => AdaptiveGraphics from the component menu
