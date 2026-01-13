using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewGameData", menuName = "Data/GameData")]
public class GameData : ScriptableObject
{
    public int money;

    public bool[] characterUnlocks = new bool[8]
    {
        true,
        false,
        false,
        false,
        false,
        false,
        false,
        false
    };
    
    public ShotgunmanData shotgunmanData;
    public ScoundrelData scoundrelData;
    
    public void ResetData()
    {
        money = 0;
    }
}

[System.Serializable]
public class ShotgunmanData
{
    public CommonCharData common;
    
    public void Reset()
    {
        common.Reset(MasterDictionary.Characters.Shotgunman);
    }
}

[System.Serializable]
public class ScoundrelData
{
    public CommonCharData common;
}

[System.Serializable]
public class CommonCharData
{
    public MasterDictionary.Characters id;
    public int level = 1;

    public void Reset(MasterDictionary.Characters newId)
    {
        id = newId;
        level = 1;
    }
}