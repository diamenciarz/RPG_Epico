﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : BasicProjectileController
{

    [Header("Bullet Settings")]
    // Ustawienia dla bomby

    //Private variables
    private float originalSize;

    protected override void Update()
    {
        base.Update();
        SetNewSize();
    }
    protected override void SetupStartingValues()
    {
        base.SetupStartingValues();
        originalSize = transform.localScale.x;
    }
    

    private void SetNewSize()
    {
        float newSize = (Time.time - creationTime) / timeToExpire * (bombSize - originalSize) + originalSize;
        gameObject.transform.localScale = new Vector3(newSize, newSize, 0);

        if (Time.time - creationTime > timeToExpire)
        {
            gameObject.transform.localScale = new Vector3(bombSize, bombSize, 0);

            DestroyProjectile();
        }
    }
}


