using UnityEngine;
using UnityEngine.InputSystem;

namespace BallCatch.Gameplay
{
    /// <summary>
    /// Hold-based horizontal movement for the player paddle. Input is sampled every frame and applied in
    /// FixedUpdate through the kinematic Rigidbody, so speed is frame-rate independent and the ball's
    /// collisions stay consistent. Time.timeScale = 0 freezes movement while paused.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayAreaBounds playArea;
        [SerializeField, Min(0f), Tooltip("Horizontal speed in world units per second.")]
        private float moveSpeed = 8f;

        private Rigidbody body;
        private float horizontalInput;
        private bool inputEnabled = true;

        public float MoveSpeed => moveSpeed;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (playArea == null)
                playArea = FindFirstObjectByType<PlayAreaBounds>();
            ResetToStart();
        }

        private void Update()
        {
            horizontalInput = inputEnabled ? ReadHorizontalInput() : 0f;
        }

        /// <summary>Gates player control (off in menus); disabling also stops any held movement.</summary>
        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled)
                horizontalInput = 0f;
        }

        private void FixedUpdate()
        {
            Vector3 position = body.position;
            position.x = Mathf.Clamp(position.x + horizontalInput * moveSpeed * Time.fixedDeltaTime,
                playArea.PlayerMinX, playArea.PlayerMaxX);
            position.y = playArea.PlayerY;
            position.z = playArea.ZPosition;
            body.MovePosition(position);
        }

        /// <summary>Returns the player to its start position and clears input (used on restart).</summary>
        public void ResetToStart()
        {
            horizontalInput = 0f;
            Vector3 start = playArea.PlayerStartPosition;
            body.position = start;
            transform.position = start;
        }

        private static float ReadHorizontalInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return 0f;

            bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
            bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
            return (right ? 1f : 0f) - (left ? 1f : 0f);
        }
    }
}
