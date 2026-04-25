using UnityEngine;
using SandWater.Inputs;

namespace SandWater.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float jumpHeight = 1.4f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundedStickForce = -2f;

        private CharacterController _cc;
        private Vector3 _velocity;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void Update()
        {
            var input = InputHandler.Instance;
            if (input == null) return;

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            forward.y = 0f; right.y = 0f;
            forward.Normalize(); right.Normalize();

            Vector2 move = input.Move;
            Vector3 horizontal = (forward * move.y + right * move.x) * moveSpeed;

            if (_cc.isGrounded)
            {
                if (_velocity.y < 0f) _velocity.y = groundedStickForce;
                if (input.JumpPressedThisFrame)
                {
                    _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }

            _velocity.y += gravity * Time.deltaTime;

            Vector3 motion = horizontal + new Vector3(0f, _velocity.y, 0f);
            _cc.Move(motion * Time.deltaTime);
        }
    }
}
