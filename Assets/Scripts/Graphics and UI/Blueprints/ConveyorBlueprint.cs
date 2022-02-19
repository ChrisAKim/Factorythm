﻿using UnityEngine;
using UnityEditor;
using System.Collections;

public class ConveyorBlueprint : MachineBluePrint {
    [SerializeField] public Sprite[] Sprites;

    private SpriteRenderer _mySR;
    private float cornerAngle = 0;
    
    /*
     * 0 - Corner U->R
     * 1 - Edge   R->R
     * 2 - Corner R->D
     * 3 - Edge   D->D
     * 4 - Corner D->L
     * 5 - Edge   L->L
     * 6 - Corner L->U
     * 7 - Edge   U->U
     */

    public void Awake() {
        _mySR = GetComponentInChildren<SpriteRenderer>();
    }

    public void SetEdgeSprite(Vector2 dir) {
        float angleRot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angleRot < 0) {
            angleRot = angleRot * -1 + 180;
        }

        Sprite s = Sprites[(int) angleRot / 90];
        _mySR.sprite = s;
    }

    public void SetCornerSprite(Vector2 dir, Vector2 orthoDir) {
        
        float angleBetween = Vector2.SignedAngle(Vector2.right, orthoDir-dir);
        cornerAngle = angleBetween;
        if (angleBetween < 0) {
            angleBetween = angleBetween * -1 + 180;
        }
        Sprite s = Sprites[4+(int) angleBetween / 90];
        _mySR.sprite = s;
    }

    public void OnDrawGizmos() {
        Handles.Label(transform.position, ""+cornerAngle);
    }
}