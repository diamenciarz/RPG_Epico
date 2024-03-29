using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootingController : TeamUpdater
{
    [Header("Instances")]
    [SerializeField] SalvoScriptableObject salvo;
    [Tooltip("Game Object, which will act as the creation point for the bullets")]
    [SerializeField] Transform shootingPoint;
    [SerializeField] GameObject gunReloadingBarPrefab;
    [Header("Settings")]
    [Tooltip("The direction of bullets coming out of the gun pipe")]
    [SerializeField] float basicGunRotation;
    [Header("Mouse Steering")]
    bool isControlledByMouseCursor;
    [SerializeField] bool reloadingBarOn = true;

    //The gun tries to shoot, if this is set to true
    protected bool shoot;
    //Private variables
    private ProgressionBarController gunReloadingBarScript;
    private EntityCreator entityCreator;
    private SingleShotScriptableObject currentShotSO;
    private GameObject parent;

    private float shootingTimeBank;
    private float currentTimeBetweenEachShot;
    private float lastShotTime;
    private int shotIndex;
    private bool canShoot;
    private int shotAmount;

    #region Initialization
    protected void Start()
    {
        InitializeStartingVariables();
        CallStartingMethods();
    }
    private void InitializeStartingVariables()
    {
        parent = transform.parent.gameObject;
        entityCreator = FindObjectOfType<EntityCreator>();
        lastShotTime = Time.time;
        shootingTimeBank = GetSalvoTimeSum();
        shotAmount = salvo.shots.Length;
        canShoot = true;
        shotIndex = 0;
        UpdateTimeBetweenEachShot();
    }
    private void CallStartingMethods()
    {
        UpdateUIState();
    }
    #endregion

    protected virtual void Update()
    {
        CheckTimeBank();
        TryShoot();
        UpdateAmmoBar();
    }
    private void TryShoot()
    {
        if (shoot)
        {
            if ((shotIndex <= shotAmount - 1) && canShoot)
            {
                DoOneShot(shotIndex);
                canShoot = false;
                StartCoroutine(WaitForNextShotCooldown(shotIndex));
                shotIndex++;
                UpdateTimeBetweenEachShot();
            }
        }
    }

    #region Reloading
    private void CheckTimeBank()
    {
        if (salvo.reloadAllAtOnce)
        {
            TryReloadAllAmmo();
        }
        else
        {
            TryReloadOneBullet();
        }
    }
    private void TryReloadAllAmmo()
    {
        float reloadCooldown = salvo.additionalReloadTime + GetSalvoTimeSum(shotIndex - 1);
        float timeSinceLastShot = Time.time - lastShotTime;
        if (timeSinceLastShot >= reloadCooldown)
        {
            shootingTimeBank = GetSalvoTimeSum();
            shotIndex = 0;
            UpdateTimeBetweenEachShot();
        }
    }
    private void TryReloadOneBullet()
    {
        if (shotIndex > 0)
        {
            float previousShotDelay = salvo.reloadDelays[shotIndex - 1];
            float reloadCooldown = salvo.additionalReloadTime + previousShotDelay;
            float timeSinceLastShot = Time.time - lastShotTime;

            if ((timeSinceLastShot >= reloadCooldown) && (shotIndex > 0))
            {
                shootingTimeBank += previousShotDelay;
                shotIndex--;
                lastShotTime += previousShotDelay;
                UpdateTimeBetweenEachShot();
            }
        }
    }
    IEnumerator WaitForNextShotCooldown(int index)
    {
        float delay = salvo.delayAfterEachShot[index];
        yield return new WaitForSeconds(delay);
        canShoot = true;
    }
    IEnumerator ShootSalvo()
    {
        for (int i = 0; i < shotAmount; i++)
        {
            shotIndex = i;
            UpdateTimeBetweenEachShot();
            DoOneShot(i);
            yield return new WaitForSeconds(salvo.delayAfterEachShot[i]);
        }
    }
    #endregion

    #region Shot Methods
    private void DoOneShot(int shotIndex)
    {
        currentShotSO = salvo.shots[shotIndex];
        PlayShotSound();
        CreateNewProjectiles();
        //Update time bank
        DecreaseShootingTime();
    }
    private void CreateNewProjectiles()
    {
        if (currentShotSO.projectilesToCreateList.Count != 0)
        {
            for (int i = 0; i < currentShotSO.projectilesToCreateList.Count; i++)
            {
                SingleShotForward(i);
            }
        }
    }
    private void SingleShotForward(int i)
    {
        if (currentShotSO.spreadProjectilesEvenly)
        {
            SingleShotForwardWithRegularSpread(i);
        }
        else
        {
            SingleShotForwardWithRandomSpread(i);
        }
    }
    private void SingleShotForwardWithRandomSpread(int index)
    {
        SummonedProjectileData data = new SummonedProjectileData();
        data.summonRotation = RotForwardRandomSpread();
        data.summonPosition = shootingPoint.position;
        data.team = team;
        data.createdBy = createdBy;
        data.bulletType = currentShotSO.projectilesToCreateList[index];
        data.target = null;

        entityCreator.SummonProjectile(data);
    }
    private Quaternion RotForwardRandomSpread()
    {
        Quaternion newBulletRotation = HelperMethods.RandomRotationInRange(currentShotSO.leftBulletSpread, currentShotSO.rightBulletSpread);
        newBulletRotation *= transform.rotation * Quaternion.Euler(0, 0, basicGunRotation);
        return newBulletRotation;
    }

    private void SingleShotForwardWithRegularSpread(int index)
    {
        SummonedProjectileData data = new SummonedProjectileData();
        data.summonRotation = RotForwardRegularSpread(index);
        data.summonPosition = shootingPoint.position;
        data.team = team;
        data.createdBy = createdBy;
        data.bulletType = currentShotSO.projectilesToCreateList[index];
        data.target = null;

        //Parent game object should be the owner of the gun
        entityCreator.SummonProjectile(data);
    }
    private Quaternion RotForwardRegularSpread(int index)
    {
        float bulletOffset = (currentShotSO.spreadDegrees * (index - (currentShotSO.projectilesToCreateList.Count - 1f) / 2));
        Quaternion newBulletRotation = Quaternion.Euler(0, 0, bulletOffset);
        newBulletRotation *= transform.rotation * Quaternion.Euler(0, 0, basicGunRotation);
        return newBulletRotation;
    }

    #endregion

    #region Sound
    //Sounds
    private void PlayShotSound()
    {
        if (currentShotSO.shotSounds.Length != 0)
        {
            AudioClip sound = currentShotSO.shotSounds[Random.Range(0, currentShotSO.shotSounds.Length)];
            StaticDataHolder.PlaySound(sound, transform.position, currentShotSO.shotSoundVolume);
        }
    }
    #endregion

    #region Helper Functions
    public float GetSalvoTimeSum()
    {
        float timeSum = 0;
        foreach (var item in salvo.reloadDelays)
        {
            timeSum += item;
        }
        return timeSum;

    }
    /// <summary>
    /// Summes the time for the amount of shots. Starts counting from the last index. Amount starts from 0.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public float GetSalvoTimeSum(int amount)
    {
        amount = ClampInputIndex(amount);
        float timeSum = 0;

        for (int i = 0; i < amount; i++)
        {
            timeSum += salvo.reloadDelays[i];
        }
        return timeSum;
    }
    public void SetShoot(bool set)
    {
        shoot = set;
    }
    private void DecreaseShootingTime()
    {
        lastShotTime = Time.time;
        shootingTimeBank -= currentTimeBetweenEachShot;
    }
    private int ClampInputIndex(int index)
    {
        int shotAmount = salvo.shots.Length;
        if (index < 0)
        {
            index = 0;
        }
        else
        if (index >= shotAmount)
        {
            index = shotAmount - 1;
        }
        return index;
    }
    private void UpdateTimeBetweenEachShot()
    {
        if (shotIndex < salvo.reloadDelays.Count)
        {
            currentTimeBetweenEachShot = salvo.reloadDelays[shotIndex];
        }
        else
        {
            currentTimeBetweenEachShot = 1000;
        }
    }
    #endregion

    #region UI
    //Update states
    private void UpdateUIState()
    {
        if (isControlledByMouseCursor || reloadingBarOn)
        {
            CreateUI();
        }
        else
        {
            DeleteUI();
        }
    }
    private void CreateUI()
    {
        if (gunReloadingBarScript == null)
        {
            CreateGunReloadingBar();
        }
    }
    private void DeleteUI()
    {
        if (gunReloadingBarScript != null)
        {
            Destroy(gunReloadingBarScript.gameObject);
        }
    }
    private void CreateGunReloadingBar()
    {
        if (gunReloadingBarPrefab != null)
        {
            GameObject newReloadingBarGO = Instantiate(gunReloadingBarPrefab, transform.position, transform.rotation);
            gunReloadingBarScript = newReloadingBarGO.GetComponent<ProgressionBarController>();
            gunReloadingBarScript.SetObjectToFollow(parent);
            lastShotTime = Time.time;
        }
    }
    private void UpdateAmmoBar()
    {
        if (gunReloadingBarScript != null)
        {
            gunReloadingBarScript.UpdateProgressionBar(shootingTimeBank, GetSalvoTimeSum());
        }

    }
    public void SetIsControlledByMouseCursorTo(bool isTrue)
    {
        isControlledByMouseCursor = isTrue;
        UpdateUIState();
    }
    #endregion

}
