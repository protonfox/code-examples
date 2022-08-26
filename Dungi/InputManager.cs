using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


// Script by Brandon Dines, Proton Fox 
public class InputManager : MonoBehaviour
{
    #region Variables
    public PlayerControls controls;
    public PlayerInput input;
    public EventSystem es;
    public bool movementEnabled;
    public bool inputEnabled;

    public static InputManager instance { get; private set; }
    #endregion

    #region Methods
    private void Awake() // Sets default player input state.
    {
        if (instance != null)
        {
            Debug.LogWarning("More than one input in scene");
        }
        instance = this;
        controls = new PlayerControls();
        es = GetComponent<EventSystem>();

        movementEnabled = true;
        inputEnabled = true;

        //Cursor.lockState = CursorLockMode.Confined;
        //Cursor.visible = false;
    }


    private void OnEnable() // Enables controls
    {
        controls.Enable();
    }

    private void OnDisable() // Disables controls
    {
        controls.Disable();
    }
    public bool PlayerBoolInput(string playerInput) // Gets a true/false value for designated buttom press.
    {
        return input.actions[playerInput].triggered;
    }

    public bool PlayerBoolHeld(string playerInput) // Gets a true/false value for designated button hold.
    {
        return input.actions[playerInput].ReadValue<float>() > 0;
    }

    public Vector2 PlayerVectorInput(string playerInput) // Gets a x & y value for designated stick.
    {
        return input.actions[playerInput].ReadValue<Vector2>();
    }

    public float PlayerFloatInput(string playerInput) // Gets a float value designated button.
    {
        return input.actions[playerInput].ReadValue<float>();
    }

    public string ButtonName(string playerInput) // Gets the pressed button's name.
    {

        return input.actions[playerInput].activeControl.shortDisplayName.ToString();
    }

    public string ControlScheme() // Gets current control scheme
    {
        return transform.GetComponent<PlayerInput>().currentControlScheme;
    }

    #endregion
}
