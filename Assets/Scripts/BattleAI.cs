using System.Collections;

using System.Collections.Generic;

using UnityEngine;


public class BattleAI : MonoBehaviour {

    public Character myCharacter;
    public PuzzleAI myPuzzleAI;
    public Character myEnemy;

    public float moveTimer = 0;
    public float moveTick = 0.5F;

    public int curRedValue;
    public int curGreenValue;
    public int curBlueValue;

    // State of the character
    private float myHealth;
    private float myRed;
    private float myGreen;
    private float myBlue;
    private float myUlt;
    private float enemyHealth;
    private float enemyUlt;

    // Character variables that decide the behavior of the AI
    public float fear;
    public float patience;
    public float hoarder;
    public float ignorant;
    public float haste;
    


    // Use this for initialization
    void Start () 
    {
        myCharacter = GetComponent<Character>();
        myPuzzleAI = GetComponent<PuzzleAI>();

        myEnemy = myCharacter.enemy;
    }

    // Update is called once per frame
    void Update () 
    {
        // Don't run if not an AI character
        if (!myCharacter.isAI)
            return;

        if (moveTimer < moveTick)
        {
            moveTimer += Time.deltaTime;
            return;
        }

        moveTimer = 0;

        myHealth = myCharacter.curHealth / myCharacter.maxHealth;
        myRed = myCharacter.redCount / myCharacter.redMax;
        myGreen = myCharacter.greenCount / myCharacter.greenMax;
        myBlue = myCharacter.blueCount / myCharacter.blueMax;

        enemyHealth = myEnemy.curHealth / myEnemy.maxHealth;

        if (myPuzzleAI.root == null)
            return;

        curRedValue = myPuzzleAI.root.redValue;
        curGreenValue = myPuzzleAI.root.greenValue;
        curBlueValue = myPuzzleAI.root.blueValue;

        print("RGB:" + curRedValue + " " + curGreenValue + " " + curBlueValue);
        
        myCharacter.PopRed();
        
        myCharacter.PopGreen();
        
        myCharacter.PopBlue();
        
        if (myRed > 0.66666F)
        {
            myCharacter.Attack();
        }

        if(myHealth < 0.5F)
        {
            myCharacter.Heal();
        }
    }
}
