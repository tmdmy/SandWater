using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SandWater.UI
{
    [RequireComponent(typeof(Text))]
    public class FpsCounter : MonoBehaviour
    {
        [SerializeField] private float updateInterval = 0.5f;

        private Text _text;
        private float _timer;
        private int _frameCount;

        private void Awake()
        {
            _text = GetComponent<Text>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                _text.enabled = !_text.enabled;
            }

            _timer += Time.unscaledDeltaTime;
            _frameCount++;
            if (_timer >= updateInterval)
            {
                float fps = _frameCount / _timer;
                _text.text = $"FPS {fps:0}";
                _timer = 0f;
                _frameCount = 0;
            }
        }
    }
}
