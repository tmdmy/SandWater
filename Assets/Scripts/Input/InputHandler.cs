using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SandWater.Inputs
{
    [DefaultExecutionOrder(-900)]
    public class InputHandler : MonoBehaviour
    {
        public static InputHandler Instance { get; private set; }

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool JumpPressedThisFrame { get; private set; }
        public bool DigHeld { get; private set; }
        public bool RaiseHeld { get; private set; }
        public float BrushResizeDelta { get; private set; }

        public event Action PauseToggled;
        public event Action BrushStrengthDecreased;
        public event Action BrushStrengthIncreased;
        public event Action RetryPressed;

        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _digAction;
        private InputAction _raiseAction;
        private InputAction _brushResizeAction;
        private InputAction _pauseAction;
        private InputAction _brushStrengthDownAction;
        private InputAction _brushStrengthUpAction;
        private InputAction _retryAction;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoBootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[InputHandler]");
            go.AddComponent<InputHandler>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildActions();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            DisposeActions();
            Instance = null;
        }

        private void OnEnable() => EnableActions();
        private void OnDisable() => DisableActions();

        private void BuildActions()
        {
            _moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddBinding("<Gamepad>/leftStick");

            _lookAction = new InputAction("Look", InputActionType.Value, expectedControlType: "Vector2");
            _lookAction.AddBinding("<Mouse>/delta");
            _lookAction.AddBinding("<Gamepad>/rightStick");

            _jumpAction = new InputAction("Jump", InputActionType.Button, binding: "<Keyboard>/space");
            _jumpAction.AddBinding("<Gamepad>/buttonSouth");
            _jumpAction.performed += _ => JumpPressedThisFrame = true;

            _digAction = new InputAction("Dig", InputActionType.Button, binding: "<Mouse>/leftButton");
            _raiseAction = new InputAction("Raise", InputActionType.Button, binding: "<Mouse>/rightButton");

            _brushResizeAction = new InputAction("BrushResize", InputActionType.Value, expectedControlType: "Vector2");
            _brushResizeAction.AddBinding("<Mouse>/scroll");

            _pauseAction = new InputAction("Pause", InputActionType.Button, binding: "<Keyboard>/escape");
            _pauseAction.AddBinding("<Gamepad>/start");
            _pauseAction.performed += _ => PauseToggled?.Invoke();

            _brushStrengthDownAction = new InputAction("BrushStrengthDown", InputActionType.Button);
            _brushStrengthDownAction.AddBinding("<Keyboard>/leftBracket");
            _brushStrengthDownAction.AddBinding("<Keyboard>/q");
            _brushStrengthDownAction.AddBinding("<Keyboard>/minus");
            _brushStrengthDownAction.performed += _ => BrushStrengthDecreased?.Invoke();

            _brushStrengthUpAction = new InputAction("BrushStrengthUp", InputActionType.Button);
            _brushStrengthUpAction.AddBinding("<Keyboard>/rightBracket");
            _brushStrengthUpAction.AddBinding("<Keyboard>/e");
            _brushStrengthUpAction.AddBinding("<Keyboard>/equals");
            _brushStrengthUpAction.performed += _ => BrushStrengthIncreased?.Invoke();

            _retryAction = new InputAction("Retry", InputActionType.Button, binding: "<Keyboard>/r");
            _retryAction.performed += _ => RetryPressed?.Invoke();
        }

        private void EnableActions()
        {
            _moveAction?.Enable();
            _lookAction?.Enable();
            _jumpAction?.Enable();
            _digAction?.Enable();
            _raiseAction?.Enable();
            _brushResizeAction?.Enable();
            _pauseAction?.Enable();
            _brushStrengthDownAction?.Enable();
            _brushStrengthUpAction?.Enable();
            _retryAction?.Enable();
        }

        private void DisableActions()
        {
            _moveAction?.Disable();
            _lookAction?.Disable();
            _jumpAction?.Disable();
            _digAction?.Disable();
            _raiseAction?.Disable();
            _brushResizeAction?.Disable();
            _pauseAction?.Disable();
            _brushStrengthDownAction?.Disable();
            _brushStrengthUpAction?.Disable();
            _retryAction?.Disable();
        }

        private void DisposeActions()
        {
            _moveAction?.Dispose();
            _lookAction?.Dispose();
            _jumpAction?.Dispose();
            _digAction?.Dispose();
            _raiseAction?.Dispose();
            _brushResizeAction?.Dispose();
            _pauseAction?.Dispose();
            _brushStrengthDownAction?.Dispose();
            _brushStrengthUpAction?.Dispose();
            _retryAction?.Dispose();
        }

        private void Update()
        {
            Move = _moveAction.ReadValue<Vector2>();
            Look = _lookAction.ReadValue<Vector2>();
            DigHeld = _digAction.IsPressed();
            RaiseHeld = _raiseAction.IsPressed();
            BrushResizeDelta = _brushResizeAction.ReadValue<Vector2>().y;
        }

        private void LateUpdate()
        {
            JumpPressedThisFrame = false;
        }
    }
}
