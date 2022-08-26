using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// Script by Brandon Dines, Proton Fox 
public class PlayerLockOn : MonoBehaviour
{

    #region Variables
    [Header("Target Settings")]
    [SerializeField] bool autoShuffleClosest;
    public GameObject currentTarget;
    public List<GameObject> targets;
    public List<GameObject> objects;
    public List<GameObject> interactables;
    public List<GameObject> allTargets;

    [Header("Other Settings")]
    [SerializeField] float swapDirectionDistance;
    [SerializeField] PlayerCombat combat;
    public Collider targetBeam;
    public GameObject lockOnMarker;
    //public float beamScaler;

    private int targetIndex;
    private float targetSwapTimer;
    private float delockTimer;
    private PlayerState state;

    #endregion

    #region Methods
    private void Start() // Set default values and references.
    {
        state = GetComponentInParent<PlayerState>();
    }

    void Update()
    {
        if (combat.lockedOn && targets.Count > 0 && autoShuffleClosest) // Orders targets by range if all conditions met.
        {
            targets = targets.OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).ToList();
            targetBeam.GetComponent<TargetsInRange>().targets = targets;
        }

        if (targets.Count == 0) // Sets target to null if list is empty.
        {
            SwapTarget("NoTarget");
        }

        if (currentTarget != null) // Enables the lockon UI if a target is valid.
        {
            lockOnMarker.SetActive(true);
            lockOnMarker.transform.parent = currentTarget.transform;
            lockOnMarker.transform.position = currentTarget.transform.position + (Vector3.up * 2);
            currentTarget.GetComponent<EnemyCombat>().ShowHealth(1);
        }

        if (!targets.Contains(currentTarget)) // Delocks if current target is not in target list (e.g. out of range).
        {
            delockTimer += Time.deltaTime;
            if (delockTimer >= 2)
            {
                SwapTarget("NoTarget");
            }
        }
        targetSwapTimer += Time.deltaTime;



        if (state.currentCombatState == PlayerState.CombatStates.Dead) // Clears all lists if player is dead.
        {
            targets.Clear();
            objects.Clear();
            interactables.Clear();
        }

        allTargets.Clear();
        allTargets.AddRange(targets);
        allTargets.AddRange(objects);
        allTargets.AddRange(interactables);

        if (autoShuffleClosest) // Sorts lists by closest object if enabled.
        {
            objects = objects.OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).ToList();
            interactables = interactables.OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).ToList();
            allTargets = allTargets.OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).ToList();
        }
    }

    public void SwapTarget(string type) // Swaps lock on target depending on desired behaviour.
    {
        if (type == "SetClosest") // Sets target to the closet in range of the player.
        {
            if (targets.Count > 0)
            {
                currentTarget = targets[0];
                targetIndex = 0;
                delockTimer = 0;
            }
            else
            {
                SwapTarget("NoTarget");
            }
        }

        if (type == "NoTarget") // Sets the lock on target to null.
        {
            combat.lockedOn = false;
            if (currentTarget)
            {
                currentTarget.GetComponent<EnemyCombat>().ShowHealth(0);
            }
            currentTarget = null;
            lockOnMarker.SetActive(false);
            lockOnMarker.transform.parent = transform;
        }

        if (type == "Forward") // Cycles forward through the targets list.
        {
            if (targetIndex < targets.Count -1)
            {
                targetIndex++;
            }
            else
            {
                targetIndex = 0;
            }
            currentTarget = targets[targetIndex];
        }

        if (type == "Back") // Cycles backwards through the targets list.
        {
            if (targetIndex > 0)
            {
                targetIndex--;
            }
            else
            {
                targetIndex = targets.Count -1;
            }
            currentTarget = targets[targetIndex];
        }

        if (type == "SwapDirection" && targetSwapTimer > 0.24f) // Sets target to the closet in the direction of the controller stick.
        {
            Vector3 dir = new Vector3(combat.input.PlayerVectorInput("SwapTarget").x, 0, combat.input.PlayerVectorInput("SwapTarget").y);
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }

            List<GameObject> t = targetBeam.GetComponent<TargetsInRange>().targets;
            if (t.Count > 0)
            {
                if (currentTarget != targetBeam.GetComponent<TargetsInRange>().ClosestToBeam(0)) // If target is not already target closest to detection beam.
                {
                    currentTarget = targetBeam.GetComponent<TargetsInRange>().ClosestToBeam(0);
                }
                else
                {
                    if (t.Count > 1) 
                    {
                        float distance = Vector3.Distance(currentTarget.transform.position, targetBeam.GetComponent<TargetsInRange>().ClosestToBeam(1).transform.position);
                        if (distance <= swapDirectionDistance)
                        {
                            currentTarget = targetBeam.GetComponent<TargetsInRange>().ClosestToBeam(1); // Set new target to the closest target that is not the one already selected.
                        }
                    }
                }
            }
            targetSwapTimer = 0;
            delockTimer = 0;
        }
    }

    public void AddToList(GameObject target, List<GameObject> list) // Adds target to list.
    {
        if (!list.Contains(target))
        {
            list.Add(target);
        }
    }

    public void RemoveFromList(GameObject target, List<GameObject> list) // Removes target from list.
    {
        if (list.Contains(target))
        {
            list.Remove(target);
        }
    }

    private void OnTriggerEnter(Collider other) // Add the target to designated list when in range.
    {
        if (other.tag == "Enemy")
        {
            AddToList(other.gameObject, targets);
        }

        if (other.tag == "Pickupable")
        {
            if (!other.GetComponent<Pickupable>().beingHeld) // Check if object being held before picking up.
            {
                AddToList(other.gameObject, objects);
            }
        }

        if (other.tag == "Interactable")
        {
            if (other.GetComponent<Interactable>())
            {
                if (!other.GetComponent<Interactable>().interacted) // Check if object already interacted with before interaction.
                {
                    AddToList(other.gameObject, interactables);
                }
            }
        }

    }


    private void OnTriggerExit(Collider other) // Remove the target from the designated list when out of range.
    {
        if (other.tag == "Enemy")
        {
            RemoveFromList(other.gameObject, targets);
        }

        if (other.tag == "Pickupable")
        {
            RemoveFromList(other.gameObject, objects);
        }

        if (other.tag == "Interactable")
        {
            RemoveFromList(other.gameObject, interactables);
        }
    }

    #endregion
}
