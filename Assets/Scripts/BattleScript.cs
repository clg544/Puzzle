using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleScript : MonoBehaviour {
    
    // Used as indeces into character array
    public enum Characters
    {
        ATTACK,
        DEFENCE,
        HEALTH,
        SPEED,
        SPECIAL,
        BALANCE
    }
    public struct CharSheet
    {
        public int attack;
        public int defence;
        public int health;
        public int speed;
        public int special;
    }

    public CharSheet[] characterData;

    public void BuildCharacters()
    {
        for (int i = 0; i < 6; i++)
        {
            characterData[i] = new CharSheet();

            switch ((Characters) i)
            {
                case Characters.ATTACK:
                    characterData[i].attack = 5;
                    characterData[i].defence = 3;
                    characterData[i].health = 2;
                    characterData[i].speed = 4;
                    characterData[i].special = 1;
                    break;
                case Characters.DEFENCE:
                    characterData[i].attack = 2;
                    characterData[i].defence = 5;
                    characterData[i].health = 4;
                    characterData[i].speed = 1;
                    characterData[i].special = 3;
                    break;
                case Characters.HEALTH:
                    characterData[i].attack = 3;
                    characterData[i].defence = 1;
                    characterData[i].health = 5;
                    characterData[i].speed = 2;
                    characterData[i].special = 4;
                    break;
                case Characters.SPEED:
                    characterData[i].attack = 1;
                    characterData[i].defence = 4;
                    characterData[i].health = 3;
                    characterData[i].speed = 5;
                    characterData[i].special = 2;
                    break;
                case Characters.SPECIAL:
                    characterData[i].attack = 4;
                    characterData[i].defence = 2;
                    characterData[i].health = 1;
                    characterData[i].speed = 3;
                    characterData[i].special = 5;
                    break;
                case Characters.BALANCE:
                    characterData[i].attack = 3;
                    characterData[i].defence = 3;
                    characterData[i].health = 3;
                    characterData[i].speed = 3;
                    characterData[i].special = 3;
                    break;
            }
        }
    }


    public void Attack(BattleScript enemy)
    {
        
    }



    public void Start()
    {

    }

    public void Update()
    {

    }
    
}
