﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutomaticGunRotator : TeamUpdater, ISerializationCallbackReceiver
{
    //Instances
    GameObject theNearestEnemyGameObject;

    [Header("Sounds")]
    //Sound
    [SerializeField] List<AudioClip> shootingSoundsList;
    [SerializeField] [Range(0, 1)] float shootSoundVolume;

    [Header("Gun stats")]
    [Tooltip("Delta angle from the middle of parent's rotation")]
    [SerializeField] float basicGunDirection;
    [SerializeField] bool rotatesTowardsTheNearestEnemy = true;
    [SerializeField] float maximumShootingRange = 20f;
    [SerializeField] float maximumRangeFromMouseToShoot = 20f;
    [SerializeField] float deltaTurretRotation = 15f;

    [Header("Turret stats")]
    [Tooltip("In degrees per second")]
    [SerializeField] float gunRotationSpeed;
    [SerializeField] bool hasRotationLimits;
    [SerializeField] float leftMaxRotationLimit;
    [SerializeField] float rightMaxRotationLimit;
    [Tooltip("The delta rotation of the gun sprite to an enemy's position")]
    [SerializeField] float gunTextureRotationOffset = -90f;

    [Header("Instances")]
    [SerializeField] Transform shootingPoint;
    [SerializeField] [Tooltip("For forward orientation and team setup")] GameObject parentGameObject;
    [SerializeField] ShootingController[] shootingControllers;
    [Header("Shooting Zone")]
    [SerializeField] GameObject shootingZonePrefab;
    [SerializeField] Transform shootingZoneTransform;
    private ProgressionBarController shootingZoneScript;

    [Header("Mouse Steering")]
    [SerializeField] bool isControlledByMouseCursor;
    [SerializeField] bool isShootingZoneOn;

    private bool areEnemiesInRange;
    public float invisibleTargetRotation;
    private static bool debugZoneOn = true;
    private Coroutine randomRotationCoroutine;
    private ProgressionBarController debugZoneScript;
    private bool lastRotationLimitValue;


    // Startup
    protected void Start()
    {
        InitializeStartingVariables();

        CallStartingMethods();
    }

    private void InitializeStartingVariables()
    {

    }
    private void CallStartingMethods()
    {

    }
    protected void Update()
    {
        UpdateUI();

        LookForTargets();
        Rotate();
        CheckShooting();
    }
    private void Rotate()
    {
        if (rotatesTowardsTheNearestEnemy)
        {
            if (areEnemiesInRange)
            {
                StopRandomRotationCoroutine();
            }
            else
            {
                CreateRandomRotationCoroutine();
            }
        }
        else
        {
            CreateRandomRotationCoroutine();
        }
        RotateOneStepTowardsTarget();
    }

    #region RandomRotation
    private void CreateRandomRotationCoroutine()
    {
        if (randomRotationCoroutine == null)
        {
            float deltaAngleFromTheMiddle = GetGunDeltaAngleFromTheMiddle();
            invisibleTargetRotation = deltaAngleFromTheMiddle;
            randomRotationCoroutine = StartCoroutine(RotateRandomly());
        }
    }
    private void StopRandomRotationCoroutine()
    {
        if (randomRotationCoroutine != null)
        {
            StopCoroutine(randomRotationCoroutine);
            randomRotationCoroutine = null;
        }
    }
    private IEnumerator RotateRandomly()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(3, 8));
            if (!areEnemiesInRange)
            {
                GenerateNewInvisibleTargetAngle();
            }
        }
    }
    private void GenerateNewInvisibleTargetAngle()
    {
        invisibleTargetRotation = Random.Range(-leftMaxRotationLimit, rightMaxRotationLimit);
    }
    private float CountMoveTowardsInvisibleTarget()
    {
        float deltaAngleFromTheMiddle = GetGunDeltaAngleFromTheMiddle();
        float angleFromGunToItem = Mathf.DeltaAngle(deltaAngleFromTheMiddle, invisibleTargetRotation);
        return angleFromGunToItem;
    }
    #endregion

    private void UpdateUI()
    {
        UpdateUIState();
        //UpdateShootingZone();
    }


    //SHOOTING ------------
    private void CheckShooting()
    {
        if (areEnemiesInRange)
        {
            if (isControlledByMouseCursor)
            {
                if (Input.GetKey(KeyCode.Mouse0))
                {
                    SetShoot(true);
                    return;
                }
            }
            else
            {
                SetShoot(true);
                return;
            }
        }
        SetShoot(false);
    }
    private void SetShoot(bool shoot)
    {
        foreach (var item in shootingControllers)
        {
            item.shoot = shoot;
        }
    }

    #region CanShoot
    //----Checks
    private void LookForTargets()
    {
        if (rotatesTowardsTheNearestEnemy)
        {
            areEnemiesInRange = CheckForEnemiesOnTheFrontInRange();
        }
        else
        {
            areEnemiesInRange = CheckForTargetsInRange();
        }
    }
    private bool CheckForEnemiesOnTheFrontInRange()
    {
        if (isControlledByMouseCursor)
        {
            Vector3 mousePosition = StaticDataHolder.GetTranslatedMousePositionIn2D(transform.position);
            return CanShootMouse(mousePosition, maximumRangeFromMouseToShoot);
        }
        else
        {
            return IsAnyEnemyInRange();
        }
    }
    private bool CheckForTargetsInRange()
    {
        if (isControlledByMouseCursor)
        {
            Vector3 mousePosition = StaticDataHolder.GetTranslatedMousePositionIn2D(transform.position);
            return CanShootMouse(mousePosition, maximumRangeFromMouseToShoot);
        }
        else
        {
            return IsAnyEnemyInRange();
        }
    }
    private bool IsAnyEnemyInRange()
    {
        List<GameObject> targetList = StaticDataHolder.GetMyEnemyList(team);

        targetList.AddRange(StaticDataHolder.GetObstacleList());

        foreach (var item in targetList)
        {
            if (CanShootTarget(item, maximumShootingRange))
            {
                return true;
            }
        }
        return false;
    }

    //----Helper functions
    private bool CanShootTarget(GameObject target, float range)
    {
        if (CanSeeTargetDirectly(target))
        {
            if (hasRotationLimits)
            {
                return IsPositionInCone(target.transform.position, range);
            }
            else
            {
                return IsPositionInRange(target.transform.position, range);
            }
        }
        return false;
    }
    private bool CanShootMouse(Vector3 targetPosition, float range)
    {
        if (hasRotationLimits)
        {
            return IsPositionInCone(targetPosition, range);
        }
        else
        {
            return IsPositionInRange(targetPosition, range);
        }
    }
    private bool IsPositionInCone(Vector3 targetPosition, float range)
    {
        if (IsPositionInRange(targetPosition, range))
        {
            float middleZRotation = GetMiddleAngle();
            Vector3 relativePositionFromGunToItem = StaticDataHolder.GetDeltaPositionFromToIn2D(targetPosition, transform.position);
            float angleFromUpToItem = Vector3.SignedAngle(Vector3.up, relativePositionFromGunToItem, Vector3.forward);
            float zAngleFromMiddleToItem = Mathf.DeltaAngle(middleZRotation, angleFromUpToItem);

            bool isCursorInCone = zAngleFromMiddleToItem > -(rightMaxRotationLimit + 2) && zAngleFromMiddleToItem < (leftMaxRotationLimit + 2);
            if (isCursorInCone)
            {
                return true;
            }
        }
        return false;
    }
    private bool IsPositionInRange(Vector3 targetPosition, float range)
    {
        Vector3 relativePositionFromGunToItem = StaticDataHolder.GetDeltaPositionFromToIn2D(targetPosition, transform.position);
        bool canShoot = range > relativePositionFromGunToItem.magnitude || range == 0;
        if (canShoot)
        {
            return true;
        }
        return false;
    }
    private bool CanSeeTargetDirectly(GameObject target)
    {
        int obstacleLayerMask = LayerMask.GetMask("Actors", "Obstacles");
        Vector2 origin = transform.position;
        Vector2 direction = target.transform.position - transform.position;
        Debug.DrawRay(origin, direction, Color.red, 0.5f);

        RaycastHit2D raycastHit2D = Physics2D.Raycast(origin, direction, Mathf.Infinity, obstacleLayerMask);

        if (raycastHit2D)
        {
            GameObject objectHit = raycastHit2D.collider.gameObject;

            bool hitTargetDirectly = objectHit == target;
            if (hitTargetDirectly)
            {
                return true;
            }
        }
        return false;
    }
    #endregion

    #region Movement
    //Move gun
    private IEnumerator RotateTowardsUntilDone(int i)
    {
        const int STEPS_PER_SECOND = 30;
        //Counts the target rotation
        float gunRotationOffset = (deltaTurretRotation * i);
        //Ustawia rotacjê, na pocz¹tkow¹ rotacjê startow¹
        Quaternion targetRotation = Quaternion.Euler(0, 0, gunRotationOffset + basicGunDirection + parentGameObject.transform.rotation.eulerAngles.z);
        while (transform.rotation != targetRotation)
        {
            targetRotation = Quaternion.Euler(0, 0, gunRotationOffset + basicGunDirection + parentGameObject.transform.rotation.eulerAngles.z);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, gunRotationSpeed / STEPS_PER_SECOND);

            yield return new WaitForSeconds(1 / STEPS_PER_SECOND);
        }
    }
    private void RotateOneStepTowardsTarget()
    {
        float degreesToRotateThisFrame = CountAngleToRotateThisFrameBy();
        RotateBy(degreesToRotateThisFrame);
    }

    private void RotateBy(float angle)
    {
        transform.rotation *= Quaternion.Euler(0, 0, angle);
    }
    #endregion

    #region Movement Helper Methods
    //----Helper functions
    private float CountAngleToRotateThisFrameBy()
    {
        float zMoveAngle = GetTargetAngle();
        //Clamp by gun rotation speed and frame rate
        float degreesToRotateThisFrame = Mathf.Clamp(zMoveAngle, -gunRotationSpeed * Time.deltaTime, gunRotationSpeed * Time.deltaTime);
        return degreesToRotateThisFrame;
    }
    private float GetTargetAngle()
    {
        if (areEnemiesInRange)
        {
            return CountEnemyTargetAngle();
        }
        else
        {
            return CountMoveTowardsInvisibleTarget();
        }
    }
    private float CountEnemyTargetAngle()
    {
        float deltaAngle = CountDeltaAngleToEnemy();

        if (hasRotationLimits)
        {
            deltaAngle = AdjustZAngleAccordingToBoundaries(deltaAngle, CountDeltaPositionToEnemy());
            Debug.Log("Gun to left limit: " + CountAngleFromGunToLeftLimit());
        }

        UpdateDebugZone(GetGunAngleFromZero(), GetGunAngleFromZero() + deltaAngle);
        return deltaAngle;
    }
    private float CountDeltaAngleToEnemy()
    {
        if (isControlledByMouseCursor)
        {
            Vector3 mousePosition = StaticDataHolder.GetTranslatedMousePositionIn2D(transform.position);
            return CountAngleFromGunToPosition(mousePosition);
        }
        else
        {
            theNearestEnemyGameObject = FindTheClosestEnemyInTheFrontInRange();
            return CountAngleFromGunToPosition(theNearestEnemyGameObject.transform.position);
        }
    }
    private Vector3 CountDeltaPositionToEnemy()
    {
        if (isControlledByMouseCursor)
        {
            return GetRelativePositionToMouseVector();
        }
        else
        {
            Vector3 relativePositionToEnemy = StaticDataHolder.GetDeltaPositionFromToIn2D(theNearestEnemyGameObject.transform.position, transform.position);
            return relativePositionToEnemy;
        }
    }

    private float CountAngleFromGunToPosition(Vector3 targetPosition)
    {
        Vector3 relativePositionFromGunToItem = StaticDataHolder.GetDeltaPositionFromToIn2D(targetPosition, transform.position);

        float angleFromZeroToItem = StaticDataHolder.GetRotationFromToIn2D(transform.position, targetPosition).eulerAngles.z + gunTextureRotationOffset;
        float angleFromGunToItem = Mathf.DeltaAngle(GetGunAngleFromZero(), angleFromZeroToItem);
        return angleFromGunToItem;
    }
    private float AdjustZAngleAccordingToBoundaries(float zAngleFromGunToItem, Vector3 deltaPositionToTarget)
    {
        float angleToMove = zAngleFromGunToItem;
        //Should only work, if the gun can see targets outside of its rotation limit
        angleToMove = IfTargetIsOutOfBoundariesSetItToMaxValue(zAngleFromGunToItem, deltaPositionToTarget);

        //If angleToMove would cross a boundary, go around it instead
        //angleToMove = GoAroundBoundaries(angleToMove);
        return angleToMove;
    }
    private float IfTargetIsOutOfBoundariesSetItToMaxValue(float angleFromGunToItem, Vector3 relativePositionToTarget)
    {
        float angleFromMiddleToItem = CountAngleFromMiddleToPosition(relativePositionToTarget); // <-180;180>
        if (angleFromGunToItem <= -rightMaxRotationLimit)
        {
            angleFromGunToItem = -rightMaxRotationLimit - angleFromMiddleToItem;
        }
        else
        if (angleFromGunToItem >= leftMaxRotationLimit)
        {
            angleFromGunToItem = leftMaxRotationLimit - angleFromMiddleToItem;
        }
        return angleFromGunToItem;
    }
    private float CountAngleFromMiddleToPosition(Vector3 targetPosition)
    {
        Vector3 relativePositionFromGunToItem = targetPosition;
        //Wylicza k¹t od aktualnego kierunku do najbli¿szego przeciwnika.
        float angleFromZeroToItem = Vector3.SignedAngle(Vector3.up, relativePositionFromGunToItem, Vector3.forward);
        float middleZRotation = GetMiddleAngle();
        float angleFromMiddleToItem = angleFromZeroToItem - middleZRotation;

        if (angleFromMiddleToItem < -180)
        {
            angleFromMiddleToItem += 360;
        }
        return angleFromMiddleToItem;
    }
    #region Go Around Boundaries
    private float GoAroundBoundaries(float angleToMove)
    {
        if (angleToMove > 0)
        {
            return GoAroundLeftBoundary(angleToMove);
        }
        if (angleToMove < 0)
        {
            return GoAroundRightBoundary(angleToMove);
        }
        return angleToMove;
    }
    private float GoAroundLeftBoundary(float angleToMove)
    {
        float angleFromGunToLeftLimit = CountAngleFromGunToLeftLimit();
        Debug.Log("From gun to left limit: " + angleFromGunToLeftLimit);
        if (angleFromGunToLeftLimit > 0)
        {
            if (angleToMove >= angleFromGunToLeftLimit)
            {
                return (angleToMove - 360);
            }
        }
        return angleToMove;
    }
    private float GoAroundRightBoundary(float angleToMove)
    {
        float angleFromGunToRightLimit = CountAngleFromGunToRightLimit();
        Debug.Log("From gun to right limit: " + angleFromGunToRightLimit);
        if (angleFromGunToRightLimit < 0)
        {
            if (angleToMove <= angleFromGunToRightLimit)
            {
                return (angleToMove + 360);
            }
        }
        return angleToMove;
    }
    private float CountAngleFromGunToLeftLimit()
    {
        float angleFromGunToLeftLimit = (GetMiddleAngle() + leftMaxRotationLimit) - (GetGunAngleFromZero());
        if (angleFromGunToLeftLimit > 360)
        {
            angleFromGunToLeftLimit -= 360;
        }
        return angleFromGunToLeftLimit;
        //return Mathf.DeltaAngle(GetGunAngle() - gunTextureRotationOffset, GetMiddleAngle() + leftMaxRotationLimit);
    }
    private float CountAngleFromGunToRightLimit()
    {
        float angleFromGunToRightLimit = (GetMiddleAngle() - rightMaxRotationLimit) - (GetGunAngleFromZero());
        if (angleFromGunToRightLimit < -360)
        {
            angleFromGunToRightLimit += 360;
        }
        return angleFromGunToRightLimit;
        //return Mathf.DeltaAngle(GetGunAngle() - gunTextureRotationOffset, GetMiddleAngle() - rightMaxRotationLimit);
    }
    #endregion
    private Vector3 GetRelativePositionToMouseVector()
    {
        Vector3 mousePosition = StaticDataHolder.GetTranslatedMousePositionIn2D(transform.position);
            
        Vector3 relativePositionToTarget = StaticDataHolder.GetDeltaPositionFromToIn2D(mousePosition, transform.position);
        return relativePositionToTarget;
    }
    #region GetValues
    private float GetGunDeltaAngleFromTheMiddle()
    {
        return GetGunAngleFromZero() - GetMiddleAngle();
    }
    private float GetMiddleAngle()
    {
        float middleAngle = parentGameObject.transform.rotation.eulerAngles.z + basicGunDirection;
        if (middleAngle > 180)
        {
            middleAngle -= 360;
        }
        if (middleAngle < -180)
        {
            middleAngle += 360;
        }
        return middleAngle;
    }
    private float GetGunAngleFromZero()
    {
        Quaternion gunRotation = transform.rotation; //* Quaternion.Euler(0, 0, gunTextureRotationOffset);
        float gunAngle = gunRotation.eulerAngles.z;
        
        if (gunAngle > 180)
        {
            gunAngle -= 360;
        }
        
        return gunAngle;
    }
    #endregion

    #endregion

    #region UpdateUI
    //Update states
    private void UpdateDebugZone(float startAngle, float endAngle)
    {
        float parentAngle = parentGameObject.transform.rotation.eulerAngles.z;
        float angleSize = startAngle - endAngle;
        //Debug.Log(startAngle + ", " + endAngle);
        if (angleSize < 0)
        {
            debugZoneScript.UpdateProgressionBar(-angleSize, 360);
            float shootingZoneRotation = endAngle - parentAngle - gunTextureRotationOffset;
            debugZoneScript.SetDeltaRotationToObject(Quaternion.Euler(0, 0, shootingZoneRotation));
        }
        else
        {
            debugZoneScript.UpdateProgressionBar(angleSize, 360);
            float shootingZoneRotation = startAngle - parentAngle - gunTextureRotationOffset;
            debugZoneScript.SetDeltaRotationToObject(Quaternion.Euler(0, 0, shootingZoneRotation));
        }

    }
    private void UpdateShootingZone()
    {
        if (shootingZoneScript != null)
        {
            if (areEnemiesInRange)
            {
                //Make the light orange bar show up
                shootingZoneScript.IsVisible(true);
            }
            else
            {
                shootingZoneScript.IsVisible(false);
            }
        }
    }
    private void UpdateUIState()
    {
        if (isControlledByMouseCursor || isShootingZoneOn)
        {
            CreateGunShootingZone();
        }
        else
        {
            DeleteGunShootingZone();
        }
        if (debugZoneScript == null && debugZoneOn)
        {
            CreateDebugZone();
        }
        if (lastRotationLimitValue != hasRotationLimits)
        {
            lastRotationLimitValue = hasRotationLimits;
            DeleteGunShootingZone();
            CreateGunShootingZone();
        }
    }
    #endregion

    #region Create/Destroy UI
    //UI
    private void CreateDebugZone()
    {
        if (shootingZonePrefab != null)
        {
            GameObject newShootingZoneGo = Instantiate(shootingZonePrefab, shootingZoneTransform);
            newShootingZoneGo.transform.localScale = new Vector3(1.8f, 1.8f, 1);

            SetupDebugZone(newShootingZoneGo);
        }
    }
    private void SetupDebugZone(GameObject newShootingZoneGo)
    {
        debugZoneScript = newShootingZoneGo.GetComponent<ProgressionBarController>();
        debugZoneScript.SetObjectToFollow(shootingZoneTransform.gameObject);
    }
    private void DeleteGunShootingZone()
    {
        if (shootingZoneScript != null)
        {
            Destroy(shootingZoneScript.gameObject);
        }
    }
    private void CreateGunShootingZone()
    {
        if (shootingZonePrefab != null && shootingZoneScript == null)
        {
            GameObject newShootingZoneGo = Instantiate(shootingZonePrefab, shootingZoneTransform);

            float xScale = GetCurrentRange() / newShootingZoneGo.transform.lossyScale.x;
            float yScale = GetCurrentRange() / newShootingZoneGo.transform.lossyScale.y;
            newShootingZoneGo.transform.localScale = new Vector3(xScale, yScale, 1);

            SetupShootingZoneShape(newShootingZoneGo);
        }
    }
    private void SetupShootingZoneShape(GameObject newShootingZoneGo)
    {
        shootingZoneScript = newShootingZoneGo.GetComponent<ProgressionBarController>();
        if (hasRotationLimits)
        {
            shootingZoneScript.UpdateProgressionBar((leftMaxRotationLimit + rightMaxRotationLimit), 360);
        }
        else
        {
            shootingZoneScript.UpdateProgressionBar(1, 1);
        }
        shootingZoneScript.SetObjectToFollow(shootingZoneTransform.gameObject);
        float shootingZoneRotation = basicGunDirection + leftMaxRotationLimit;
        shootingZoneScript.SetDeltaRotationToObject(Quaternion.Euler(0, 0, shootingZoneRotation));
    }
    #endregion

    //Look for targets
    private GameObject FindTheClosestEnemyInTheFrontInRange()
    {
        List<GameObject> targetList = StaticDataHolder.GetMyEnemyList(team);

        targetList.AddRange(StaticDataHolder.GetObstacleList());
        if (targetList.Count == 0)
        {
            return null;
        }

        GameObject currentClosestEnemy = null;
        foreach (var item in targetList)
        {
            //I expect enemyList to never have a single null value
            if (CanShootTarget(item, maximumShootingRange))
            {
                if (currentClosestEnemy == null)
                {
                    currentClosestEnemy = item;
                }
                float zAngleFromMiddleToCurrentClosestEnemy = CountAngleFromGunToPosition(currentClosestEnemy.transform.position);
                float zAngleFromMiddleToItem = CountAngleFromGunToPosition(item.transform.position);
                //If the found target is closer to the middle (angle wise) than the current closest target, make is the closest target
                bool isCloserAngleWise = Mathf.Abs(zAngleFromMiddleToCurrentClosestEnemy) > Mathf.Abs(zAngleFromMiddleToItem);
                if (isCloserAngleWise)
                {
                    currentClosestEnemy = item;
                }
            }
        }
        return currentClosestEnemy;
    }
    public void SetIsControlledByMouseCursorTo(bool isTrue)
    {
        isControlledByMouseCursor = isTrue;
    }
    private float GetCurrentRange()
    {
        if (isControlledByMouseCursor)
        {
            return maximumRangeFromMouseToShoot;
        }
        else
        {
            return maximumShootingRange;
        }
    }

}