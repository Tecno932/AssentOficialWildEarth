using UnityEngine;
using UnityEngine.InputSystem;

namespace WildEarth.Voxel
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class VoxelPlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField]
        private float walkSpeed = 4.5f;

        [SerializeField]
        private float sprintSpeed = 7f;

        [SerializeField]
        private float crouchSpeed = 2.5f;

        [SerializeField]
        private float jumpHeight = 1.25f;

        [SerializeField]
        private float gravity = -25f;

        [Header("Creative")]
        [SerializeField]
        private float creativeFlySpeed = 8f;

        [SerializeField]
        private float creativeSprintSpeed = 14f;

        [Header("Spectator")]
        [SerializeField]
        private float spectatorSpeed = 12f;

        [SerializeField]
        private float spectatorSprintSpeed = 24f;

        [Header("Crouch")]
        [SerializeField]
        private float standingHeight = 1.8f;

        [SerializeField]
        private float crouchingHeight = 1.0f;

        private CharacterController characterController;

        private VoxelPlayerMode mode =
            VoxelPlayerMode.Survival;

        private float verticalVelocity;

        public VoxelPlayerMode Mode =>
            mode;

        private void Awake()
        {
            EnsureCharacterController();

            characterController.height =
                standingHeight;

            characterController.center =
                Vector3.up *
                (standingHeight * 0.5f);
        }

        public void SetMode(
            VoxelPlayerMode newMode)
        {
            EnsureCharacterController();

            mode = newMode;

            switch (mode)
            {
                case VoxelPlayerMode.Survival:
                    EnableCollision();
                    break;

                case VoxelPlayerMode.Creative:
                    EnableCollision();
                    verticalVelocity = 0f;
                    break;

                case VoxelPlayerMode.Spectator:
                    verticalVelocity = 0f;
                    DisableCollision();
                    break;
            }
        }

        public void Tick()
        {
            EnsureCharacterController();

            if (Keyboard.current == null)
                return;

            switch (mode)
            {
                case VoxelPlayerMode.Survival:
                    HandleSurvival();
                    break;

                case VoxelPlayerMode.Creative:
                    HandleCreative();
                    break;

                case VoxelPlayerMode.Spectator:
                    HandleSpectator();
                    break;
            }
        }

        private void HandleSurvival()
        {
            Vector2 input =
                ReadMovementInput();

            Vector3 movement =
                GetHorizontalMovement(input);

            bool crouching =
                Keyboard.current.leftCtrlKey.isPressed;

            bool sprinting =
                Keyboard.current.leftShiftKey.isPressed &&
                !crouching;

            float speed;

            if (crouching)
            {
                speed = crouchSpeed;
            }
            else if (sprinting)
            {
                speed = sprintSpeed;
            }
            else
            {
                speed = walkSpeed;
            }

            ApplyCrouch(crouching);

            movement *= speed;

            if (characterController.isGrounded)
            {
                if (verticalVelocity < 0f)
                    verticalVelocity = -2f;

                if (
                    Keyboard.current.spaceKey
                        .wasPressedThisFrame)
                {
                    verticalVelocity =
                        Mathf.Sqrt(
                            jumpHeight *
                            -2f *
                            gravity
                        );
                }
            }

            verticalVelocity +=
                gravity *
                Time.deltaTime;

            movement.y =
                verticalVelocity;

            characterController.Move(
                movement *
                Time.deltaTime
            );
        }

        private void HandleCreative()
        {
            Vector2 input =
                ReadMovementInput();

            Vector3 movement =
                GetHorizontalMovement(input);

            float speed =
                Keyboard.current.leftShiftKey.isPressed
                    ? creativeSprintSpeed
                    : creativeFlySpeed;

            movement *= speed;

            if (Keyboard.current.spaceKey.isPressed)
            {
                movement.y += speed;
            }

            if (Keyboard.current.leftCtrlKey.isPressed)
            {
                movement.y -= speed;
            }

            characterController.Move(
                movement *
                Time.deltaTime
            );
        }

        private void HandleSpectator()
        {
            Vector2 input =
                ReadMovementInput();

            Vector3 movement =
                transform.right * input.x +
                transform.forward * input.y;

            if (movement.sqrMagnitude > 1f)
                movement.Normalize();

            float speed =
                Keyboard.current.leftShiftKey.isPressed
                    ? spectatorSprintSpeed
                    : spectatorSpeed;

            movement *= speed;

            if (Keyboard.current.spaceKey.isPressed)
            {
                movement.y += speed;
            }

            if (Keyboard.current.leftCtrlKey.isPressed)
            {
                movement.y -= speed;
            }

            transform.position +=
                movement *
                Time.deltaTime;
        }

        private Vector2 ReadMovementInput()
        {
            Vector2 input =
                Vector2.zero;

            if (Keyboard.current.aKey.isPressed)
                input.x -= 1f;

            if (Keyboard.current.dKey.isPressed)
                input.x += 1f;

            if (Keyboard.current.sKey.isPressed)
                input.y -= 1f;

            if (Keyboard.current.wKey.isPressed)
                input.y += 1f;

            if (input.sqrMagnitude > 1f)
                input.Normalize();

            return input;
        }

        private Vector3 GetHorizontalMovement(
            Vector2 input)
        {
            Vector3 movement =
                transform.right * input.x +
                transform.forward * input.y;

            if (movement.sqrMagnitude > 1f)
                movement.Normalize();

            movement.y = 0f;

            return movement;
        }

        private void ApplyCrouch(
            bool crouching)
        {
            float targetHeight =
                crouching
                    ? crouchingHeight
                    : standingHeight;

            characterController.height =
                Mathf.Lerp(
                    characterController.height,
                    targetHeight,
                    12f * Time.deltaTime
                );

            characterController.center =
                Vector3.up *
                (characterController.height * 0.5f);
        }

        private void EnableCollision()
        {
            EnsureCharacterController();

            characterController.enabled = true;
        }

        private void DisableCollision()
        {
            EnsureCharacterController();

            characterController.enabled = false;
        }

        private void EnsureCharacterController()
        {
            if (characterController != null)
                return;

            characterController =
                GetComponent<CharacterController>();

            if (characterController == null)
            {
                throw new MissingComponentException(
                    "VoxelPlayerMovement requiere " +
                    "un CharacterController."
                );
            }
        }
    }
}