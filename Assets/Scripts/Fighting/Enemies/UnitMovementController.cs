using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitMovementController : TeamUpdater
{
    public GameObject lastTargetPosition;
    [Header("Instances")]
    public GameObject target;
    [Header("Movement")]
    [Tooltip("Degrees per second")]
    public float rotationSpeed = 120;
    [SerializeField] float shootRange = 2;
    [SerializeField] float moveSpeed = 3;

    private Pathfinding.AIDestinationSetter aiDestinationSetter;
    private Pathfinding.AIBase aiBase;
    private float currentMoveSpeed;
    private bool canSeeTarget;

    private float POSITION_REFRESH_RATE = 0.25f; // Cooldown to refresh path to target

    #region Initiation
    private void Start()
    {
        SetupStartingVariables();
        StartCoroutine(ITargetPositionUpdater(POSITION_REFRESH_RATE));
    }
    private void Update()
    {
        CheckIfCanSeeTarget();
        SetTargetPositionPointer();
        RotateTowardsTarget();
    }
    private void SetupStartingVariables()
    {
        //Setup instances
        aiDestinationSetter = GetComponent<Pathfinding.AIDestinationSetter>();
        aiBase = GetComponent<Pathfinding.AIBase>();
        //Setup target position
        lastTargetPosition = new GameObject();
        lastTargetPosition.transform.position = transform.position;
        aiDestinationSetter.target = lastTargetPosition.transform;
        //Movement speed
        currentMoveSpeed = moveSpeed;
        SetMovementSpeed(currentMoveSpeed);
    }
    #endregion

    #region Movement
    #region Pathing
    private IEnumerator ITargetPositionUpdater(float refreshRate)
    {
        while (true)
        {
            UpdateTarget();
            yield return new WaitForSeconds(refreshRate);
        }
    }
    private void UpdateTarget()
    {
        if (!canSeeTarget)
        {
            FindNewTargetInSight();
        }
    }
    private void FindNewTargetInSight()
    {
        GameObject newTarget = StaticDataHolder.GetClosestEnemyInSight(transform.position, team);
        if (newTarget)
        {
            target = newTarget;
        }
    }
    private void SetTargetPositionPointer()
    {

        if (target)
        {
            if (HelperMethods.Distance(gameObject, target) <= shootRange)
            {
                if (canSeeTarget)
                {
                    MovePointerOntoMyself();
                }
            }
            else
            {
                MovePointerOntoTarget();
            }
        }
    }
    private void MovePointerOntoTarget()
    {
        if (target)
        {
            lastTargetPosition.transform.position = target.transform.position;
        }
        else
        {
            Debug.Log("No path target found");
        }
    }
    private void MovePointerOntoMyself()
    {
        lastTargetPosition.transform.position = transform.position;
    }

    #endregion

    #region Rotation
    private void CheckIfCanSeeTarget()
    {
        canSeeTarget = HelperMethods.CanSeeTargetDirectly(transform.position, target);
    }
    private void RotateTowardsTarget()
    {
        Vector3 lookAt = CountLookAtPosition();
        Quaternion targetRotation = HelperMethods.DeltaPositionRotation(transform.position, lookAt);
        float angleThisFrame = rotationSpeed * Time.deltaTime;

        Quaternion.RotateTowards(transform.rotation, targetRotation, angleThisFrame);
    }
    private Vector3 CountLookAtPosition() {
        if (canSeeTarget)
        {
            return target.transform.position;
        }
        else
        {
            return lastTargetPosition.transform.position;
        }
    }
    #endregion
    private void SetMovementSpeed(float newSpeed)
    {
        aiBase.maxSpeed = newSpeed;
    }
    #endregion

}
