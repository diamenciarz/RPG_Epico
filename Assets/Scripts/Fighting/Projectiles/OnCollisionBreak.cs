using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnCollisionBreak : TeamUpdater
{
    [Header("Collision Settings")]
    public List<BreaksOn> breakEnum = new List<BreaksOn>();

    [Header("Sounds")]
    [SerializeField] protected List<AudioClip> breakingSounds;
    [SerializeField] [Range(0, 1)] protected float breakingSoundVolume = 1f;

    protected GameObject objectThatCreatedThisProjectile;
    private bool isDestroyed = false;
    protected float creationTime;
    private bool isARocket;

    public enum BreaksOn
    {
        Allies,
        Enemies,
        AllyBullets,
        EnemyBullets,
        Explosions,
        Rockets,
        Obstacles
    }

    protected virtual void Awake()
    {
        UpdateStartingVariables();
    }
    private void UpdateStartingVariables()
    {
        creationTime = Time.time;
        CheckRocket();
    }
    private void CheckRocket()
    {
        OnCollisionDamage onCollisionDamage = GetComponent<OnCollisionDamage>();
        if (onCollisionDamage != null)
        {
            isARocket = onCollisionDamage.DamageTypeContains(OnCollisionDamage.TypeOfDamage.Rocket);
        }
    }

    #region Collisions
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        BreakChecks(collision.gameObject);
    }
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        BreakChecks(collision.gameObject);
    }
    private void BreakChecks(GameObject collisionObject)
    {
        if (BreaksOnObstacle(collisionObject))
        {
            Break();
            return;
        }

        if (BreaksOnProjectile(collisionObject))
        {
            Break();
            return;
        }

        if (BreaksOnAllyOrEnemy(collisionObject))
        {
            Break();
            return;
        }
    }

    #region Break Checks
    private bool BreaksOnObstacle(GameObject collisionObject)
    {
        bool isAnObstacle = false;
        ListUpdater listUpdater = collisionObject.GetComponent<ListUpdater>();
        if (listUpdater)
        {
            isAnObstacle = listUpdater.ListContains(ListUpdater.AddToLists.Obstacle);
        }
        isAnObstacle = isAnObstacle || collisionObject.tag == "Obstacle";
        return isAnObstacle && BreaksOnContactWith(BreaksOn.Obstacles);
    }
    private bool BreaksOnAllyOrEnemy(GameObject collisionObject)
    {
        bool areTeamsEqual = team == HelperMethods.GetObjectTeam(collisionObject);
        if (CheckParent(collisionObject))
        {
            bool breaksOnAlly = areTeamsEqual && HelperMethods.IsObjectAnEntity(collisionObject) && BreaksOnContactWith(BreaksOn.Allies);
            if (breaksOnAlly)
            {
                return true;
            }
        }
        bool breaksOnEnemy = !areTeamsEqual && BreaksOnContactWith(BreaksOn.Enemies) && HelperMethods.IsObjectAnEntity(collisionObject);
        return breaksOnEnemy;
    }
    /// <summary>
    /// Checks, if the colliding object is the object that created this projectile and if so, then checks if it should be invulnerable
    /// </summary>
    /// <param name="collisionObject"></param>
    /// <returns></returns>
    private bool CheckParent(GameObject collisionObject)
    {
        bool isTouchingParent = createdBy == collisionObject;
        bool isInvulnerable = Time.time > creationTime + 0.1f;
        if (!isTouchingParent || (isTouchingParent && isInvulnerable))
        {
            return true;
        }
        return false;
    }
    private bool BreaksOnProjectile(GameObject collisionObject)
    {
        IDamage damageReceiver = collisionObject.GetComponent<IDamage>();
        if (damageReceiver != null && ShouldBreak(damageReceiver))
        {
            return true;
        }
        return false;
    }
    private bool ShouldBreak(IDamage iDamage)
    {
        int collisionTeam = iDamage.GetTeam();
        bool areTeamsEqual = collisionTeam == team;

        bool breaksOnAllyBullet = BreaksOnContactWith(BreaksOn.AllyBullets) && iDamage.DamageTypeContains(OnCollisionDamage.TypeOfDamage.Projectile) && areTeamsEqual;
        if (breaksOnAllyBullet)
        {
            return true;
        }
        bool breaksOnEnemyBullet = BreaksOnContactWith(BreaksOn.EnemyBullets) && iDamage.DamageTypeContains(OnCollisionDamage.TypeOfDamage.Projectile) && !areTeamsEqual;
        if (breaksOnEnemyBullet)
        {
            return true;
        }
        bool breaksOnExplosion = BreaksOnContactWith(BreaksOn.Explosions) && iDamage.DamageTypeContains(OnCollisionDamage.TypeOfDamage.Explosion);
        if (breaksOnExplosion)
        {
            return true;
        }
        bool breaksOnRocket = BreaksOnContactWith(BreaksOn.Rockets) && iDamage.DamageTypeContains(OnCollisionDamage.TypeOfDamage.Rocket);
        if (breaksOnRocket)
        {
            return true;
        }
        return false;
    }
    #endregion

    #endregion
    
    #region Destroy
    protected void Break()
    {
        if (!isDestroyed)
        {
            isDestroyed = true;
            StaticDataHolder.PlaySound(GetBreakSound(), transform.position, breakingSoundVolume);

            DestroyObject();
        }
    }
    private void DestroyObject()
    {
        if (!StaticDataHolder.CallAllTriggers(gameObject))
        {
            StartCoroutine(DestroyAtTheEndOfFrame());
        }
    }
    private IEnumerator DestroyAtTheEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        Destroy(gameObject);
    }
    #endregion

    //Sounds
    protected AudioClip GetBreakSound()
    {
        int soundIndex = Random.Range(0, breakingSounds.Count);
        if (breakingSounds.Count > soundIndex)
        {
            return breakingSounds[soundIndex];
        }
        return null;
    }
    //Accessor methods
    public bool BreaksOnContactWith(BreaksOn contact)
    {
        if (breakEnum.Contains(contact))
        {
            return true;
        }
        return false;
    }
}
