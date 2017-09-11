using UnityEngine;

namespace Lumeneo
{

    public class ShowFPS : MonoBehaviour
    {

        public int
            fontSize = 40,
            displayX = 30,
            displayY = 30;

        public float updateInterval = 0.5F;

        float lastInterval, fps;

        int _frames = 0;




        void Start()
        {

            lastInterval = Time.realtimeSinceStartup;

            _frames = 0;
        }

        void OnGUI()
        {
            GUI.skin.label.fontSize = fontSize;

            GUI.Label(new Rect(displayX, displayY, 800, 100), "FPS: " + fps.ToString("f2"));

        }

        void Update()
        {
            ++_frames;

            if (Time.realtimeSinceStartup > lastInterval + updateInterval)
            {
                fps = _frames / (Time.realtimeSinceStartup - lastInterval);

                _frames = 0;

                lastInterval = Time.realtimeSinceStartup;
            }
        }
    }
}