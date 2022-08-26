using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

// Script by Brandon Dines, Proton Fox 
public class IsometricMovement : MonoBehaviour
{
    #region Variables
    [Header("Walk Settings")]
    [SerializeField] MovementType movementType;
    [SerializeField][Range(0, 10)] float moveSpeed;
    [SerializeField][Range(0, 10)] float moveHeight;
    [SerializeField][Range(0, 10)] float injuredSpeed;
    public Vector3 move;
    private float currentSpeed;


    [Header("Rotation Settings")]
    [SerializeField] RotationType rotationType;
    private Vector3 rightMove;
    private Camera gameCamera;

    [Header("Sprint Settings")]
    [Space(5)]
    [SerializeField][Range(0, 15)] float sprintSpeed;
    [SerializeField][Range(0, 10)] float sprintDuration;
    [SerializeField][Range(0, 5)] float sprintRecharge;
    private float sprintTimer;


    [Header("Jump Settings")]
    [SerializeField][Range(0, 5)] int totalJumps;
    [SerializeField][Range(0, 5)] float jumpHeight;
    [SerializeField][Range(-20, 0)] float playerGravity;
    [SerializeField][Range(-20, 1)] float fallingThreshold;
    private Vector3 playerVelocity;
    private bool grounded;
    private bool falling;
    private int jumpCount;
    private float fallY;

    [Header("Crouch Settings")]
    [SerializeField][Range(0, 10)] float crouchSpeed;
    [SerializeField][Range(0, 10)] float crouchHeight;
    private float characterHeight;

    [Header("Slide Settings")]
    [SerializeField][Range(0, 10)] float slideSpeed;

    [Header("Dodge Settings")]
    [SerializeField] bool airDodge;
    [SerializeField] bool rightStickDodge;
    [SerializeField][Range(0, 5)] int totalDodges;
    [SerializeField][Range(0, 3)] float dodgeDistance;
    [SerializeField][Range(0, 40)] float dodgeSpeed; 
    [SerializeField][Range(0, 10)] float dodgeCooldown; 
    [SerializeField][Range(0.1f, 3)] float dodgeBuffer;
    public Vector3 stickDodge;
    private float dodgeTimer;
    private float dodgeResetTimer;
    private int dodgeCount;
    private bool dodging;

    [Header("Interact Settings")]
    [SerializeField] Collider interactionHitbox;
    [SerializeField] GameObject heldObject;
    private PlayerInventory inventory;
    private float pickupTimer;
    private GlobalInventory globalInventory;


    [Header("Knockback Settings")]
    [SerializeField] float knockbackTime;
    private float knockbackCounter;

    public enum MovementType
    {
        FullControl,
        Grid,
    }

    public enum RotationType
    {
        Isometric, // Character rotates with movement
        Twinstick, // Character rotates with right stick
        SpriteLookAt, // Character looks at camera
        CamControl // Character moves depending on camera pos
    }

    //private bool knockedDown;
    private InputManager input;
    private CharacterController controller;
    private Rigidbody rig;
    private PlayerState state;
    private Animator anima;
    private PlayerCombat combat;

    #endregion

    #region Methods

    void Start() // Sets default values and references.
    {
        input = GetComponent<InputManager>();
        controller = GetComponent<CharacterController>();
        anima = GetComponent<Animator>();
        rig = GetComponent<Rigidbody>();
        state = GetComponent<PlayerState>();
        combat = GetComponent<PlayerCombat>();
        inventory = GetComponent<PlayerInventory>();
        globalInventory = GameObject.FindObjectOfType<GlobalInventory>();
        dodgeTimer = dodgeBuffer;
        currentSpeed = moveSpeed;
        sprintTimer = sprintDuration;
        characterHeight = moveHeight;
        gameCamera = Camera.main;
    }

    void Update()
    {
        if (state.canMove)
        {
            PlayerMove(input.PlayerVectorInput("Move").x, input.PlayerVectorInput("Move").y);
            //PlayerRotate();

            if (!inventory.menusOpen)
            {
                PlayerSprint();
                PlayerJump();
                PlayerDodge();
                //PlayerCrouch();
                //PlayerSlide();
                PlayerInteract();
                PlayerEmote();
            }

            knockbackCounter -= Time.deltaTime;
        }
    }

    private void PlayerMove(float x, float y) // Takes input and moves the player. Movement speed is based on player's current state.
    {
        if (knockbackCounter <= 0)
        {
            float moveY = move.y;
            move = Camera.main.transform.forward * y + Camera.main.transform.right * x;
            move.y = 0;
            PlayerRotate();
            move.y = moveY;
        }
        else
        {
            knockbackCounter -= Time.deltaTime;
        }

        move.y += playerGravity * Time.deltaTime;
        controller.Move(move * Time.deltaTime * currentSpeed);

        if (input.PlayerBoolHeld("Pivot")) // Brings the player to a crawl speed to allow quick precise movements for aiming.
        {
            currentSpeed = 1;
        }
        else if (currentSpeed == 1)
        {
            currentSpeed = moveSpeed;
        }



        if (move.x != 0) // Sets state to moving.
        {
            if (!dodging && jumpCount == 0 && currentSpeed == moveSpeed && grounded && !heldObject)
            {
                if (state.currentMovementState != PlayerState.MovementStates.Injured)
                {
                    state.currentMovementState = PlayerState.MovementStates.Walking;
                    currentSpeed = moveSpeed;
                }
                else
                {
                    currentSpeed = injuredSpeed;
                }
            }
            anima.SetBool("Moving", true);
            anima.SetFloat("MoveSpeed", input.PlayerVectorInput("Move").magnitude);
        }
        else // Sets movement state to idle and resets move speeds.
        {
            state.currentMovementState = PlayerState.MovementStates.Idle;

            if (grounded && jumpCount == 0 && currentSpeed != crouchSpeed && !dodging && !heldObject)// && !knockedDown)
            {
                if (state.currentMovementState != PlayerState.MovementStates.Injured)
                {
                    currentSpeed = moveSpeed;
                }
                else
                {
                    currentSpeed = injuredSpeed;
                }
            }
            anima.SetBool("Moving", false);
        }


    }

    private void PlayerRotate() // Rotates the player based on desired rotation type.
    {
        if (rotationType == RotationType.Isometric)
        {
            // IF CAMERA NOT FOLLOWING
            if (move != Vector3.zero)
            {
                transform.GetChild(0).rotation = Quaternion.LookRotation(move);
            }
        }

        if (rotationType == RotationType.SpriteLookAt) // Has the player look directly at the camera at all times.
        {
            transform.GetChild(0).LookAt(Camera.main.transform);
        }

        if (rotationType == RotationType.Twinstick) // Allows for rotation via second input. Mouse currently broken.
        {
            rightMove = new Vector3(input.PlayerVectorInput("Look").x, 0, input.PlayerVectorInput("Look").y);
            if (rightMove != Vector3.zero)
            {
                transform.GetChild(0).rotation = Quaternion.LookRotation(rightMove);
            }
        }

        if (rotationType == RotationType.CamControl) // Allows for manual camera control.
        {
            if (move != Vector3.zero)
            {
                transform.GetChild(0).rotation = Quaternion.LookRotation(move);
            }

            rightMove = new Vector3(input.PlayerVectorInput("Look").x, 0, input.PlayerVectorInput("Look").y);
            if (rightMove != Vector3.zero)
            {
                if (input.ControlScheme() != "Keyboard&Mouse" || input.ControlScheme() == "Keyboard&Mouse" && input.PlayerBoolHeld("LookMouse"))
                {
                    gameCamera.GetComponentInChildren<Cinemachine.CinemachineInputProvider>().enabled = true;
                    gameCamera.transform.RotateAround(transform.position, rightMove, 100 * Time.deltaTime);//.transform.GetChild(0).rotation = Quaternion.LookRotation(rightMove);
                }
                else
                {
                    gameCamera.GetComponentInChildren<Cinemachine.CinemachineInputProvider>().enabled = false;
                }
            }
        }

        if (combat.lockedOn && combat.lockOn.targets.Count > 0) // Rotation override if player is locked onto target.
        {
            // FIX LOOKING UP/DOWN AT ENEMY
            transform.GetChild(0).LookAt(new Vector3(combat.lockOn.currentTarget.transform.position.x, transform.GetChild(0).position.y, combat.lockOn.currentTarget.transform.position.z));
        }
    }

    private void PlayerSprint() // Allows the player so sprint, increasing speed and playing animation.
    {
        if (state.canSprint)
        {
            if (input.PlayerBoolInput("Sprint") && sprintTimer > 0 && currentSpeed != crouchSpeed)
            {
                currentSpeed = sprintSpeed;
            }
            else
            {
                if (sprintTimer < sprintDuration && currentSpeed != sprintSpeed)
                {
                    sprintTimer += Time.deltaTime;// * (sprintRecharge / 10);
                }
                if (sprintTimer >= sprintDuration)
                {
                    sprintTimer = sprintDuration;
                }
            }

            if (currentSpeed == sprintSpeed) // Sets the player state based on move speed.
            {
                if (jumpCount == 0 && !dodging && move != Vector3.zero && state.currentMovementState != PlayerState.MovementStates.Injured)
                {
                    state.currentMovementState = PlayerState.MovementStates.Sprinting;
                }
                if (combat.inCombat)
                {
                    sprintTimer -= Time.deltaTime;

                    if (sprintTimer < 0 || state.currentMovementState == PlayerState.MovementStates.Injured)
                    {
                        currentSpeed = moveSpeed;
                    }
                }
            }

        }
    }

    private void PlayerInteract() // Allows the player to interact with objects, playing animation if valid interaction.
    {
        if (state.canInteract)
        {
            if (input.PlayerBoolInput("Interact"))
            {
                if (combat.lockOn.interactables.Count > 0)
                {
                    if (combat.lockOn.interactables[0].GetComponent<Interactable>().interactable)
                    {
                        state.currentMovementState = PlayerState.MovementStates.Interacting;
                        combat.lockOn.interactables[0].GetComponent<Interactable>().interacted = true;
                        //combat.lockOn.RemoveFromList(combat.lockOn.interactables[0], combat.lockOn.interactables);
                    }
                }

            }

            //if (heldObject != null)
            //{
            //    pickupTimer += Time.deltaTime;
            //    anima.SetBool("Holding", true);
            //}
            //else
            //{
            //    pickupTimer = 0;
            //    anima.SetBool("Holding", false);
            //}
        }
    }

    public void Knockback(Vector3 direction, float knockbackAmount) // Calculates knockback for the player based on direction and amount.
    {
        knockbackCounter = knockbackTime;

        Vector3 knockDirection = transform.position - direction;
        move = knockDirection * knockbackAmount;
        //move.y = knockbackAmount;
    }

    private void PlayerRagdoll() // NOT CURRENTLY IMPLEMENTED
    {

    }

    private void PlayerJump() // Allows the player to jump. Does checks for current state and applies gravity.
    {
        grounded = controller.isGrounded;
        float distanceSinceLastFrame = (transform.position.y - fallY) * Time.deltaTime;
        fallY = transform.position.y;
        anima.SetInteger("JumpCount", jumpCount);


        if (distanceSinceLastFrame < fallingThreshold && !grounded && characterHeight != crouchHeight) // Sets the falling behaviour.
        {
            falling = true;
            state.currentMovementState = PlayerState.MovementStates.Falling;
        }
        else
        {
            falling = false;
        }

        if (grounded && move.y < 0) // Resets the jump cooldown.
        {
            jumpCount = 0;
            move.y = 0;
        }


        if (state.canJump)
        {
            if (input.PlayerBoolInput("Jump") && jumpCount != totalJumps && currentSpeed != crouchSpeed) // Applies jump if all conditions met.
            {
                move.y = 0f;
                move.y += Mathf.Sqrt(jumpHeight * -3.0f * playerGravity);
                jumpCount++;
                state.currentMovementState = PlayerState.MovementStates.Jumping;
            }
        }
    }

    private void PlayerDodge() // Allows the player to dodge. Does checks for current dodge count/buffer and if air dodges are allowed.
    {
        if (state.canDodge)
        {
            if (dodgeCount <= totalDodges && dodgeTimer >= dodgeBuffer)
            {
                if (input.PlayerBoolInput("Dodge") && move != Vector3.zero)
                {
                    if (airDodge)
                    {
                        StartCoroutine(Dodge());
                    }
                    else if (!airDodge && grounded)
                    {
                        StartCoroutine(Dodge());
                        GameObject dodgeVfx = Instantiate(combat.vfx[1], transform.position, Quaternion.identity, null);
                    }
                }

                stickDodge = Camera.main.transform.forward * input.PlayerVectorInput("FlickDodge").y + Camera.main.transform.right * input.PlayerVectorInput("FlickDodge").x;
                stickDodge.y = 0;

                if (stickDodge != Vector3.zero && rightStickDodge)
                {
                    if (airDodge)
                    {
                        StartCoroutine(Dodge());
                    }
                    else if (!airDodge && grounded)
                    {
                        StartCoroutine(Dodge());
                    }
                }
            }
        }

        if (dodgeCount != totalDodges)
        {
            dodgeTimer += Time.deltaTime;
        }
        else
        {
            dodgeResetTimer += Time.deltaTime;
            if (dodgeResetTimer >= dodgeCooldown)
            {
                dodgeTimer = 0;
                dodgeResetTimer = 0;
                dodgeCount = 0;
            }
        }
    }

    IEnumerator Dodge() // Applies constant motion to the player until specified time has been reached.
    {
        float startTime = Time.time;
        dodging = true;
        dodgeCount++;
        dodgeTimer = 0;
        state.currentMovementState = PlayerState.MovementStates.Dodging;

        Vector3 stickD = stickDodge;

        while (Time.time < startTime + dodgeDistance)
        {
            if (!rightStickDodge && move != Vector3.zero)
            {
                controller.Move(move * (dodgeSpeed * Time.deltaTime));
            }
            else
            {
                controller.Move(stickDodge * (dodgeSpeed * Time.deltaTime));
                transform.GetChild(0).rotation = Quaternion.LookRotation(stickDodge);
            }
            yield return null;
        }
        dodging = false;
    }

    private void PlayerEmote() // Makes the player emote if button held.
    {
        if (input.PlayerBoolHeld("Emote"))
        {
            state.currentMovementState = PlayerState.MovementStates.Emoting;
            int selectedEmote = 2; // Change to list selection when implemented.
            anima.SetInteger("Emote", selectedEmote);
        }
        else
        {
            anima.SetInteger("Emote", 0);
        }
    }


    // NOT CURRENTLY USING
    //void PlayerCrouch() // Allows the player to crouch. Checks if they can and plays the relevant animation + movement speed.
    //{
    //    if (state.canCrouch)
    //    {
    //        if (input.PlayerBoolInput("Crouch") && grounded)
    //        {
    //            if (currentSpeed != crouchSpeed)
    //            {

    //                currentSpeed = crouchSpeed;
    //                characterHeight = crouchHeight;
    //                state.currentMovementState = PlayerState.MovementStates.Crouching;
    //            }
    //            else
    //            {
    //                currentSpeed = moveSpeed;
    //                characterHeight = moveHeight;
    //                state.currentMovementState = PlayerState.MovementStates.Idle;
    //            }

    //        }
    //        if (grounded)
    //        {
    //            controller.height = Mathf.Lerp(controller.height, characterHeight, 10 * Time.deltaTime);
    //        }
    //    }
    //}

    // NOT CURRENTLY USING
    //void PlayerSlide() // Allows the player to slide.
    //{
    //    if (state.canSlide)
    //    {
    //        if (input.PlayerBoolInput("Slide"))
    //        {
    //            currentSpeed = slideSpeed;
    //            state.currentMovementState = PlayerState.MovementStates.Sliding;
    //        }
    //    }
    //}


    private void OnTriggerStay(Collider other) // Sets touching wall animation on if colliding with wall.
    {
        if (other.tag == "Wall")
        {
            anima.SetBool("TouchingWall", true);
            currentSpeed = 1;
        }
    }

    private void OnTriggerExit(Collider other) // Sets touching wall animation off if exiting wall collision.
    {
        if (other.tag == "Wall")
        {
            anima.SetBool("TouchingWall", false);
            currentSpeed = moveSpeed;
        }
    }

    #endregion
}