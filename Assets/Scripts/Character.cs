using UnityEngine;
using UnityEngine.UI;
using System;


public class Character : MonoBehaviour
{
    // Game State Holders
    public Character enemy;
    public GameBoardScript myGameBoard;
    public AI myAI;
    public bool isAI;
    public bool isAlive;

    // Character dependant stats
    public int attackStat;
    public int defenceStat;
    public int healthStat;
    public int specialStat;
    public int speedStat;

    // Scalar value for attacks
    public int oneBarAtkScale;
    public int twoBarAtkScale;
    public int threeBarAtkScale;
    public int defenceScale;
    public float speedScale;

    // Combat values
    private int oneBarAttack;
    private int twoBarAttack;
    private int threeBarAttack;
    private float defenceReduction;  // Damage reduced 5% per defence

    // Values to calculate health values
    private int oneBarHeal;
    private int twoBarHeal;
    private int threeBarHeal;
    private int oneBarHealScale;
    private int twoBarHealScale;
    private int threeBarHealScale;
    public int healthScale;
    public int curHealth;
    public int maxHealth;

    // Tile dropping variables
    public bool fastDropOn;     // false is slow, true is fast
    public float tileDropRate;
    public float fastTileDropRate;
    public float dropTime;

    // The UI sliders
    public Slider healthSlider;
    public Slider redSlider;
    public Slider greenSlider;
    public Slider blueSlider;

    // Values to calculate resource values 
    public int redCount;
    public int redMax;
    public int greenCount;
    public int greenMax;
    public int blueCount;
    public int blueMax;

    // UI Score Text & Value
    public Text TextScore;
    int curScore;

    public Character()
    {
        this.attackStat = 0;
        this.defenceStat = 0;
        this.healthStat = 0;
        this.specialStat = 0;
        this.speedStat = 0;

        MakeCharacter();
    }


    /**
    *    Constructer(int, int, int, int, int) : Create a new character with
    *        The given base stats.
    */
    public Character(int attack, int defence, int health, int special, int speed)
    {
        this.attackStat = attack;
        this.defenceStat = defence;
        this.healthStat = health;
        this.specialStat = special;
        this.speedStat = speed;

        MakeCharacter();
    }


    // Place an attack of the enemy player.
    public void Attack()
    {
        float redLevel = (float)redCount / (float)redMax;

        if (redCount == redMax)
        {
            redCount = 0;
            enemy.Defend(threeBarAttack);
        }
        else if (redLevel > 0.66666f)
        {
            redCount = redCount - (2 * redMax / 3);
            enemy.Defend(twoBarAttack);
        }
        else if (redLevel > 0.33333f)
        {
            redCount = redCount - (redMax / 3);
            enemy.Defend(oneBarAttack);
        }

        return;
    }


    // Recieve an attack.
    public void Defend(int attackDamage)
    {
        attackDamage -= (int)((float)attackDamage * defenceReduction);

        if (attackDamage > 0)
            curHealth -= attackDamage;

        if (curHealth <= 0)
        {
            curHealth = 0;
            KillPlayer();
        }

        return;
    }


    // Activate my spacial move
    public void Special()
    {
        System.Console.WriteLine("Character attampting to use undefined function 'Special()'");
    }


    // Heal Myself
    public void Heal()
    {
        if (!isAlive)
            return;

        float greenLevel = (float)greenCount / (float)greenMax;

        if (greenCount == greenMax)
        {
            greenCount = 0;
            curHealth += threeBarHeal;
        }
        else if (greenLevel > 0.66666f)
        {
            greenCount = greenCount - (2 * greenCount / 3);
            curHealth += twoBarHeal;
        }
        else if (greenLevel > 0.33333f)
        {
            greenCount = greenCount - (greenCount / 3);
            curHealth += oneBarHeal;
        }

        if (curHealth > maxHealth)
            curHealth = maxHealth;

        return;
    }


    public void AddHealth(int amt)
    {
        curHealth += amt;

        if(curHealth > maxHealth)
            curHealth = maxHealth;

        return;
    }
    public void AddBlue(int amt)
    {
        blueCount += amt;

        if (blueCount > blueMax)
            blueCount = blueMax;

        return;
    }
    public void AddGreen(int amt)
    {
        greenCount += amt;

        if (greenCount > greenMax)
            greenCount = greenMax;

        return;
    }
    public void AddRed(int amt)
    {
        redCount += amt;

        if (redCount > redMax)
            redCount = redMax;

        return;
    }


    public void KillPlayer()
    {
        isAlive = false;
        return;
    }

    
    public void PopRed()
    {
        myGameBoard.PopTiles(TileState.RED);

        if (redCount > redMax)
            redCount = redMax;

        return;
    }
    public void PopGreen()
    {
        myGameBoard.PopTiles(TileState.GREEN);

        if (greenCount > greenMax)
            greenCount = greenMax;

        return;
    }
    public void PopBlue()
    {
        myGameBoard.PopTiles(TileState.BLUE);

        if (blueCount > blueMax)
            blueCount = blueMax;

        return;
    }


    /**
    * void MakeCharacter() - After all statistics are set, this function will
    *           set all values that are derivitive of them.
    *
    */
    private void MakeCharacter()
    {

        // Attack values
        int oneBarBase = 4;
        int twoBarBase = 8;
        int threeBarBase = 12;
        oneBarAttack = oneBarBase + ((attackStat - 3) * oneBarAtkScale);
        twoBarAttack = twoBarBase + ((attackStat - 3) * twoBarAtkScale);
        threeBarAttack = threeBarBase + ((attackStat - 3) * threeBarAtkScale);

        // Defence Values
        defenceReduction = (defenceStat - 3) * defenceScale;

        // Health values
        int healthBase = 30;
        maxHealth = healthBase + ((healthStat - 3) * healthScale);
        curHealth = maxHealth;

        // Healing Values
        int oneBarHealBase = 3;
        int twoBarHealBase = 7;
        int threeBarHealBase = 15;
        oneBarHeal = oneBarHealBase + ((3 - healthStat) * oneBarHealScale);
        twoBarHeal = twoBarHealBase + ((3 - healthStat) * twoBarHealScale);
        threeBarHeal = threeBarHealBase + ((3 - healthStat) * threeBarHealScale);
    }

    #region UnityFunctions

    // Setup all of the decision trees
    public void Start()
    {
        // Set universal static values
        redSlider.value = 0;
        greenSlider.value = 0;
        blueSlider.value = 0;

        redCount = 0;
        greenCount = 0;
        blueCount = 0;

        curHealth = maxHealth;

        healthSlider.value = (float)curHealth / (float)maxHealth;

        redSlider.value = (float)redCount / (float)redMax;
        greenSlider.value = (float)greenCount / (float)greenMax;
        blueSlider.value = (float)blueCount / (float)blueMax;
    }


    public void Update()
    {
        dropTime += Time.deltaTime;

        if(dropTime > tileDropRate && !fastDropOn)
        {
            print(dropTime);

            dropTime = 0;
            myGameBoard.DropAllTiles();
        }
        else if(dropTime > fastTileDropRate && fastDropOn)
        {
            dropTime = 0;
            myGameBoard.DropAllTiles();
        }

        healthSlider.value = (float)curHealth / (float)maxHealth;

        redSlider.value = (float)redCount / (float)redMax;
        greenSlider.value = (float)greenCount / (float)greenMax;
        blueSlider.value = (float)blueCount / (float)blueMax;
    }

    #endregion UnityFunctions

    #region TestCode

    public void TestAllMethods()
    {

        // Initialize relevent values
        string results;

        Character attacker = new Character(5, 5, 5, 5, 5);
        Character defender = new Character(5, 5, 5, 5, 5);

        attacker.enemy = defender;
        defender.enemy = attacker;

        /**
            * Test 0 bar Attack
            */
        results = "";

        attacker.redCount = 1;
        attacker.redMax = 6;

        defender.maxHealth = 100;
        defender.curHealth = 100;
        defender.defenceReduction = 0.05f;

        results += "Testing empty bar attack...";
        attacker.redCount = 3;

        attacker.Attack();

        if (defender.curHealth != 100)
            results += "Fail, Health != 100";
        else if (attacker.redCount != 1)
            results += "Fail, redCount != 1";
        else
            results += "Pass";

        System.Console.WriteLine(results);


        /**
            * Test 1 bar Attack
            */
        results = "Testing One bar attack...";
        attacker.redCount = 2; // Out of 6
        defender.curHealth = 100; // Out of 100

        attacker.Attack();

        if (defender.curHealth != 97)
            results += "Fail, " + defender.curHealth + ", expected 97";
        else if (attacker.redCount != 0)
            results += "Fail, redCount = " + attacker.redCount + ", expected 0";
        else
            results += "Pass";

        System.Console.WriteLine(results);


        /** 
            * Test 2 bar Attack
            */
        results = "Testing Two bar attack...";
        attacker.redCount = 5; // Out of 6
        defender.curHealth = 100; // Out of 100

        attacker.Attack();

        if (defender.curHealth != 94)
            results += "Fail, " + defender.curHealth + ", expected 94";
        else if (attacker.redCount != 1)
            results += "Fail, redCount != 1";
        else
            results += "Pass";

        System.Console.WriteLine(results);


        /** 
            * Test 3 bar Attack
            */
        results = "Testing Three bar attack...";
        attacker.redCount = 6; // Out of 6
        defender.curHealth = 100; // Out of 100

        attacker.Attack();

        if (defender.curHealth != 91)
            results += "Fail, " + defender.curHealth + ", expected 91";
        else if (attacker.redCount != 0)
            results += "Fail, redCount != 1";
        else
            results += "Pass";

        System.Console.WriteLine(results);


        // Defend
        /**
            * Test Defend() by passing different attack values
            *         Should block 25% of incoming damage.                
            */
        results = "Testing Regular Defence...";

        defender.Defend(20);

        if (defender.curHealth != 76)
            results += "Fail, " + defender.curHealth + ", expected 76";
        else
            results += "Pass";

        System.Console.WriteLine(results);


        /**
            * Test Uneven fraction, result should round damage up                
            */
        results = "Testing rational number...";

        defender.Defend(5);    // Reduced to  5 - 3.75

        if (defender.curHealth != 72)    // Rounded up to 4
            results += "Fail, " + defender.curHealth + ", expected 72";
        else
            results += "Pass";

        System.Console.WriteLine(results);


        /**
            * Test Negative number, health should not change                
            */
        results = "Testing negative number...";

        defender.Defend(-100);

        if (defender.curHealth != 72)
            results += "Fail, " + defender.curHealth + ", expected 72";
        else
            results += "Pass";

        System.Console.WriteLine(results);


        /**
            * Kill the player                
            */
        results = "Testing Kill State...";
        defender.isAlive = true;

        defender.Defend(300);

        if (defender.curHealth != 0)
            results += "Fail, " + defender.curHealth + ", expected 0";
        else if (defender.isAlive)
            results += "Fail, isAlive flag unchanged";
        else
            results += "Pass";

        System.Console.WriteLine(results);

        // Heal

        /**
            * Heal the now dead player, should fail.                
            */
        results = "Testing Heal on dead player...";
        defender.isAlive = false;
        defender.greenCount = 6;
        defender.greenMax = 6;

        defender.Heal();

        if (defender.curHealth != 0)
            results += "Fail, " + defender.curHealth + ", expected 0";
        else if (defender.isAlive)
            results += "Fail, isAlive flag changed";
        else if (defender.greenCount != 6)
            results += "Fail, greenCount = " + defender.greenCount + ", expected 6";
        else
            results += "Pass";

        System.Console.WriteLine(results);

        /**
            * Heal living player                
            */
        results = "Testing Empty heal on living player...";
        defender.isAlive = true;
        defender.greenCount = 0;
        defender.greenMax = 6;

        defender.Heal();

        if (defender.curHealth != 0)
            results += "Fail, " + defender.curHealth + ", expected 50";
        else
            results += "Pass";

        System.Console.WriteLine(results);


        /**
            * One bar heal                
            */
        results = "Testing one bar heal...";
        defender.greenCount = 2;

        defender.Heal();

        if (defender.curHealth != 3)
            results += "Fail, " + defender.curHealth + ", expected 3";
        else if (attacker.greenCount != 0)
            results += "Fail, redCount != 0";
        else
            results += "Pass";

        System.Console.WriteLine(results);


        /**
            * Two bar heal                
            */
        results = "Testing two bar heal...";
        defender.isAlive = true;
        defender.greenCount = 4;

        defender.Heal();

        if (defender.curHealth != 10)
            results += "Fail, " + defender.curHealth + ", expected 10";
        else if (attacker.greenCount != 0)
            results += "Fail, redCount != 0";
        else
            results += "Pass";

        System.Console.WriteLine(results);


        /**
            * Three bar heal                
            */
        results = "Testing three bar heal...";
        defender.greenCount = 6;

        defender.Heal();

        if (defender.curHealth != 25)
            results += "Fail, " + defender.curHealth + ", expected 25";
        else
            results += "Pass";

        System.Console.WriteLine(results);


        /**
            * Three bar heal                
            */
        results = "Testing heal over full health...";
        defender.curHealth = 95;
        defender.greenCount = 6;

        defender.Heal();

        if (defender.curHealth != 100)
            results += "Fail, " + defender.curHealth + ", expected 100";
        else if (attacker.greenCount != 0)
            results += "Fail, redCount != 0";
        else
            results += "Pass";

        System.Console.WriteLine(results);

    }
    #endregion //TestCode
}

