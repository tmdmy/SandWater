using UnityEngine;
using SandWater.Inputs;

namespace SandWater.Player
{
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private Transform playerBody;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float pitchMin = -85f;
        [SerializeField] private float pitchMax = 85f;

        private float _pitch;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void LateUpdate()
        {
            var input = InputHandler.Instance;
            if (input == null || playerBody == null) return;

            Vector2 look = input.Look * mouseSensitivity;

            _pitch = Mathf.Clamp(_pitch - look.y, pitchMin, pitchMax);
            transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            playerBody.Rotate(Vector3.up, look.x, Space.Self);
        }
    }
}
