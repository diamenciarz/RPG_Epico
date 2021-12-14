﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StaticDataHolder
{
    [SerializeField] static AudioClip[] soundArray;

    public static List<GameObject> dashableObjectList = new List<GameObject>();

    private static List<GameObject> obstacleList = new List<GameObject>();
    private static List<GameObject> projectileList = new List<GameObject>();
    private static List<GameObject> playerProjectileList = new List<GameObject>();
    private static List<GameObject> entityList = new List<GameObject>();
    private static List<GameObject> objectsCollidingWithPlayerList = new List<GameObject>();

    public static List<float> soundDurationList = new List<float>();
    public static int soundLimit = 10;

    #region Lists

    #region Obstacle List
    public static void AddObstacle(GameObject addObject)
    {
        obstacleList.Add(addObject);
    }
    public static void RemoveObstacle(GameObject removeObject)
    {
        if (obstacleList.Contains(removeObject))
        {
            obstacleList.Remove(removeObject);
        }
    }
    public static List<GameObject> GetObstacleList()
    {
        return HelperMethods.CloneList(obstacleList);
    }
    #endregion

    #region Dashable Object
    #region Dashable Object List
    public static void AddDashableObject(GameObject addObject)
    {
        dashableObjectList.Add(addObject);
    }
    public static void RemoveDashableObject(GameObject removeObject)
    {
        if (dashableObjectList.Contains(removeObject))
        {
            dashableObjectList.Remove(removeObject);
        }
    }
    public static List<GameObject> GetDashableObjectList()
    {
        return HelperMethods.CloneList(dashableObjectList);
    }
    #endregion

    #region Dashable Object List Methods
    public static GameObject GetTheClosestDashableObject(Vector3 position)
    {
        List<GameObject> dashableObjectList = StaticDataHolder.GetDashableObjectList();
        return GetClosestObject(dashableObjectList, position);
    }
    public static GameObject GetTheClosestDashableObject(Vector3 position, float range)
    {
        List<GameObject> clonedDashableObjectList = StaticDataHolder.GetDashableObjectList();
        GameObject dashableObject = GetClosestObject(clonedDashableObjectList, position);

        float distance = HelperMethods.Distance(dashableObject.transform.position, position);
        if (distance <= range)
        {
            return dashableObject;
        }
        return null;
    }
    #endregion
    #endregion

    #region Projectile List
    public static void AddProjectile(GameObject projectile)
    {
        projectileList.Add(projectile);
    }
    public static void RemoveProjectile(GameObject projectile)
    {
        projectileList.Remove(projectile);
    }
    public static List<GameObject> GetProjectileList()
    {
        return HelperMethods.CloneList(projectileList);
    }
    #endregion

    #region Player Projectile List
    public static void AddPlayerProjectile(GameObject projectile)
    {
        playerProjectileList.Add(projectile);
    }
    public static void RemovePlayerProjectile(GameObject projectile)
    {
        playerProjectileList.Remove(projectile);
    }
    public static List<GameObject> GetPlayerProjectileList()
    {
        return HelperMethods.CloneList(playerProjectileList);
    }
    #endregion

    #region Entity List
    public static void AddEntity(GameObject entity)
    {
        entityList.Add(entity);
    }
    public static void RemoveEntity(GameObject entity)
    {
        entityList.Remove(entity);
    }
    public static List<GameObject> GetEntityList()
    {
        return HelperMethods.CloneList(entityList);
    }
    #endregion

    #region Objects colliding with player
    public static void AddCollidingObject(GameObject collidingObject)
    {
        objectsCollidingWithPlayerList.Add(collidingObject);
    }
    public static void RemoveCollidingObject(GameObject collidingObject)
    {
        if (objectsCollidingWithPlayerList.Contains(collidingObject))
        {
            objectsCollidingWithPlayerList.Remove(collidingObject);
        }
    }
    public static List<GameObject> GetCollidingObjectList()
    {
        return HelperMethods.CloneList(objectsCollidingWithPlayerList);
    }
    #region Objects colliding with player methods
    public static float GetHighestSlowEffect()
    {
        if (objectsCollidingWithPlayerList.Count != 0)
        {
            float maxSlowEffect = 1;
            foreach (GameObject item in objectsCollidingWithPlayerList)
            {
                EntityProperties entityProperties = item.GetComponent<EntityProperties>();
                if (entityProperties)
                {
                    if (entityProperties.slowingEffect < maxSlowEffect)
                    {
                        maxSlowEffect = entityProperties.slowingEffect;
                    }
                }
            }
            return maxSlowEffect;
        }
        return 1;
    }
    #endregion
    #endregion

    #region Sound List
    public static void AddSoundDuration(float duration)
    {
        soundDurationList.Add(Time.time + duration);
    }
    public static int GetSoundCount()
    {
        RemoveOldSoundsFromList();
        return soundDurationList.Count;
    }
    private static void RemoveOldSoundsFromList()
    {
        for (int i = soundDurationList.Count - 1; i >= 0; i--)
        {
            if (soundDurationList[i] < Time.time)
            {
                soundDurationList.RemoveAt(i);
            }
        }
    }
    public static int GetSoundLimit()
    {
        return soundLimit;
    }
    #endregion
    #endregion

    #region Helper Methods

    #region Get Contents
    //Enemies
    public static GameObject GetNearestEnemy(Vector3 positionVector, int myTeam)
    {
        return GetClosestObject(GetEnemyList(myTeam), positionVector);
    }
    public static List<GameObject> GetEnemyList(int myTeam)
    {
        return SubtractAllies(GetEntityList(), myTeam);
    }

    //Allies
    public static GameObject GetNearestAlly(Vector3 positionVector, int myTeam, GameObject gameObjectToIgnore)
    {
        return GetClosestObject(GetAllyList(myTeam, gameObjectToIgnore), positionVector);
    }
    public static List<GameObject> GetAllyList(int myTeam, GameObject gameObjectToIgnore)
    {
        return SubtractMeAndEnemies(GetEntityList(), myTeam, gameObjectToIgnore);
    }

    //Generic
    public static GameObject GetClosestObject(List<GameObject> possibleTargetList, Vector3 positionVector)
    {
        if (possibleTargetList.Count != 0)
        {
            GameObject currentNearestTarget = possibleTargetList[0];
            foreach (var item in possibleTargetList)
            {
                if (currentNearestTarget == null)
                {
                    currentNearestTarget = item;
                }
                bool currentTargetIsCloser = HelperMethods.Distance(positionVector, item.transform.position) < HelperMethods.Distance(positionVector, currentNearestTarget.transform.position);
                if (currentTargetIsCloser)
                {
                    currentNearestTarget = item;
                }
            }
            return currentNearestTarget;
        }
        else
        {
            return null;
        }
    }
    #endregion

    #region Modify List Contents
    public static List<GameObject> SubtractAllies(List<GameObject> inputList, int myTeam)
    {
        for (int i = inputList.Count - 1; i >= 0; i--)
        {
            DamageReceiver damageReceiver = inputList[i].GetComponent<DamageReceiver>();
            if (damageReceiver != null)
            {
                if (damageReceiver.GetTeam() == myTeam)
                {
                    inputList.Remove(inputList[i]);
                }
            }
        }
        return inputList;
    }
    public static List<GameObject> SubtractMeAndEnemies(List<GameObject> inputList, int myTeam, GameObject gameObjectToIgnore)
    {
        for (int i = inputList.Count - 1; i >= 0; i--)
        {
            DamageReceiver damageReceiver = inputList[i].GetComponent<DamageReceiver>();
            if (damageReceiver != null)
            {
                if (damageReceiver.GetTeam() != myTeam)
                {
                    inputList.Remove(inputList[i]);
                }
                //Remove itself from ally list
                if (inputList[i] == gameObjectToIgnore)
                {
                    inputList.Remove(inputList[i]);
                }
            }
        }
        return inputList;
    }
    #endregion
    #endregion

    #region Play Sounds
    /// <summary>
    /// Plays a sound, if there is space in the list of sounds. 
    /// MaxSoundCount is currently from 1-10. 
    /// The higher the count, the higher the priority, so more sounds can play in total.
    /// </summary>
    /// <param name="sound"></param>
    /// <param name="soundPosition"></param>
    /// <param name="volume"></param>
    public static void PlaySound(AudioClip sound, Vector3 soundPosition, float volume, int maxSoundCount)
    {
        if (sound != null)
        {
            if (GetSoundCount() <= (GetSoundLimit() - GetSoundCount() + maxSoundCount))
            {
                AudioSource.PlayClipAtPoint(sound, soundPosition, volume);
                AddSoundDuration(sound.length);
            }
        }
    }
    /// <summary>
    /// Plays a sound, if there is space in the list of sounds. 
    /// </summary>
    /// <param name="sound"></param>
    /// <param name="soundPosition"></param>
    /// <param name="volume"></param>
    public static void PlaySound(AudioClip sound, Vector3 soundPosition, float volume)
    {
        if (sound != null)
        {
            if (GetSoundCount() <= GetSoundLimit())
            {
                AudioSource.PlayClipAtPoint(sound, soundPosition, volume);
                AddSoundDuration(sound.length);
            }
        }
    }
    #endregion
}
