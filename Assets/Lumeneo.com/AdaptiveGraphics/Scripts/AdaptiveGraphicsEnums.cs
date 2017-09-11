/*
 *
 *  Lumeneo.com
 *  AdaptiveGraphics
 *
 *  Coded by Grig Bilham
 *  Questions? grig @ lumeneo.com
 *
 */

namespace Lumeneo
{


	// Adaptive graphics downsampling method.
	// Editor popup menu options.
    public enum DOWNSAMPLING_METHOD
    {
        Disabled = 0,
        AdaptativeDownsampling = 1,
        HorizontalDownsampling = 2,
        QuadDownsampling = 3
    }



	// Adaptive graphics compositing method.
	// Editor popup menu options.
	public enum COMPOSITING_METHOD
    {
        Simple = 0,
        SecondCameraBillboardWorldSpace = 1,
        SecondCameraBillboardOverlay = 2,
        SecondCameraBlit = 3
    }



	// Adaptive graphics filtering mode.
	// Editor popup menu options.
	public enum FILTERING_MODE
    {
        Bilinear = 0,
        NearestNeighbour = 1
    }


}