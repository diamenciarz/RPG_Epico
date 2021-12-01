using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageReceiver : ListUpdater
{
    [Header("Basic Stats")]
    [SerializeField] int team;
    [SerializeField] int health;
    [SerializeField] GameObject healthBarPrefab;
    [SerializeField] bool turnHealthBarOn;
    [SerializeField] bool canBePushed;



    [Header("Sounds")]
    [SerializeField] protected List<AudioClip> breakingSounds;
    [SerializeField] [Range(0, 1)] protected float breakingSoundVolume = 1f;
    [SerializeField] protected List<AudioClip> hitSounds;
    [SerializeField] [Range(0, 1)] protected float hitSoundVolume = 1f;



    private GameObject healthBarInstance;
    private int maxHP;
    private bool isDestroyed = false;
    private ICollidingEntityData myEntityData;
    protected ProgressionBarController healthBarScript;

    protected void Awake()
    {
        UpdateStartingVariables();
    }
    private void UpdateStartingVariables()
    {
        myEntityData = GetComponent<ICollidingEntityData>();
        maxHP = health;
    }
    protected void Update()
    {
        UpdateHealthBarVisibility();
    }
    private void UpdateHealthBarVisibility()
    {
        if (healthBarScript)
        {
            if (turnHealthBarOn)
            {
                healthBarScript.IsVisible(true);
            }
            else
            {
                healthBarScript.IsVisible(false);
            }
        }
    }

    #region Receive Damage
    //Receive damage
    /// <summary>
    /// Deal damage
    /// </summary>
    /// <param name="damage"></param>
    /// <param name="gameObject"></param>
    public void DealDamage(int damage)
    {
        health -= damage;
        CheckHealth();
    }
    /// <summary>
    /// Deal damage and try to push object
    /// </summary>
    /// <param name="damage"></param>
    /// <param name="gameObject"></param>
    public void DealDamage(IDamage iDamage)
    {
        health -= iDamage.GetDamage();
        UpdateHealthBar();
        CheckHealth();

        ModifyVelocity(iDamage);
    }
    private void ModifyVelocity(IDamage iDamage)
    {
        if (myEntityData != null)
        {
            if (canBePushed && iDamage.GetIsPushing())
            {
                myEntityData.ModifyVelocityVector3(iDamage.GetPushVector());
            }
        }
    }
    private void CheckHealth()
    {
        if (health <= 0)
        {
            HandleBreak();
        }
        else
        {
            HandleHit();
        }
    }
    #endregion

    #region Collision Handling
    //Break methods
    protected void HandleBreak()
    {
        if (!isDestroyed)
        {
            isDestroyed = true;
            StaticDataHolder.TryPlaySound(GetBreakSound(), transform.position, breakingSoundVolume);

            DestroyObject();
        }
    }
    protected void HandleHit()
    {
        StaticDataHolder.TryPlaySound(GetHitSound(), transform.position, hitSoundVolume);
    }
    //Destroy game object methods
    public void DestroyObject()
    {
        RemoveObjectFromLists();

        TriggerOnDeath[] triggerOnDeath = GetComponentsInChildren<TriggerOnDeath>();
        if (triggerOnDeath.Length != 0)
        {
            foreach (TriggerOnDeath item in triggerOnDeath)
            {
                item.ObjectDestroyed();
            }
        }
        else
        {
            //Debug.Log("No TriggerOnDeath found");
            StartCoroutine(DestroyAtTheEndOfFrame());
        }
    }
    private IEnumerator DestroyAtTheEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        Destroy(gameObject);
    }
    #endregion

    #region Sounds
    protected AudioClip GetHitSound()
    {
        int soundIndex = Random.Range(0, hitSounds.Count);
        if (hitSounds.Count > soundIndex)
        {
            return hitSounds[soundIndex];
        }
        return null;
    }
    protected AudioClip GetBreakSound()
    {
        int soundIndex = Random.Range(0, breakingSounds.Count);
        if (breakingSounds.Count > soundIndex)
        {
            return breakingSounds[soundIndex];
        }
        return null;
    }
    #endregion

    #region UI
    //Other stuff
    public void CreateHealthBar()
    {
        healthBarInstance = Instantiate(healthBarPrefab, transform.position, transform.rotation);
        healthBarScript = healthBarInstance.GetComponent<ProgressionBarController>();
        healthBarScript.SetObjectToFollow(gameObject);
    }
    private void UpdateHealthBar()
    {
        if (healthBarScript == null)
        {
            CreateHealthBar();
        }
        healthBarScript.UpdateProgressionBar(health, maxHP);
    }
    #endregion

    #region Team
    //Set methods
    public virtual void SetTeam(int newTeam)
    {
        team = newTeam;
        UpdateTeam(newTeam);
    }
    private void UpdateTeam(int newTeam)
    {
        TeamUpdater[] teamUpdater = GetComponentsInChildren<TeamUpdater>();
        foreach (TeamUpdater item in teamUpdater)
        {
            item.ChangeTeamTo(newTeam);
        }
        DamageReceiver[] damageReceivers = GetComponentsInChildren<DamageReceiver>();
        foreach (DamageReceiver item in damageReceivers)
        {
            item.ChangeTeamTo(newTeam);
        }
    }
    /// <summary>
    /// Change team of this script. Use SetTeam() to change team of the whole gameObject
    /// </summary>
    /// <param name="newTeam"></param>
    public void ChangeTeamTo(int newTeam)
    {
        team = newTeam;
    }
    //Accessor methods
    public int GetTeam()
    {
        return team;
    }
    #endregion

    public int GetCurrentHealth()
    {
        return health;
    }
}