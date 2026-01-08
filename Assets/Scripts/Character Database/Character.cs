using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Karakter adatbazis adatai
[System.Serializable]
public class Character
{
    public Sprite characterSprite;
    public string character_name;
    public NetworkPrefabRef playerPrefab;
}
