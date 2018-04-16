using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleAI : MonoBehaviour {

    // Game State Holders
    public Character myCharacter;
    public Character enemy;
    public GameBoardScript myGameBoard;
    public AI myAI;
    
    // Root nodes for decision Trees
    LinkedList<DecisionTree> allDecisionNodes;
    LinkedList<string> allDecisionNames;

    DecisionTree ShouldIAttack;
    DecisionTree ShouldIHeal;
    DecisionTree ShouldISpecial;
    DecisionTree ShouldIPopRed;
    DecisionTree ShouldIPopGreen;
    DecisionTree ShouldIPopBlue;

    // bool for tests, we use this a lot
    bool test;

    // floats that define certain decisions
    public float patience;
    public float aggression;
    public float bravery;

    // Thresholds for subjective checks
    public float RedCountThresh;
    public float GreenCountThresh;
    public float BlueCountThresh;
    public float HealthCheckThresh;
    public float RedPotentialThresh;
    public float GreenPotentialThresh;
    public float BluePotentialThresh;
    public float TimeThresh;
    public float SpecialThresh;
    public float OpponentHealthThresh;
    public float PatienceThresh;
    public float AggressionThresh;
    public float ScaredThresh;
    public float EmptySpaceThresh;

    // Decision Functions
    public bool RandomThresholdTest(float chanceToSucceed)
    {
        return Random.Range(0.0F, 1.0F) < chanceToSucceed;
    }

    public bool CanAttack()
    {
        //return (redCount / redMax) > (1.0f / 3.0f);
        return false;
    }

    public bool CanKill()
    {
        // TODO: calculate if an attack will kill the enemy
        return false;
    }

    public bool RedCount()
    {
        test = (myCharacter.redCount / myCharacter.redMax) > RedCountThresh ? true : false;

        return test;
    }

    public bool GreenCount()
    {
        test = (myCharacter.greenCount / myCharacter.greenMax) > GreenCountThresh ? true : false;

        return test;
    }

    public bool BlueCount()
    {
        test = (myCharacter.blueCount / myCharacter.blueMax) > BlueCountThresh ? true : false;

        return test;
    }

    public bool HealthCheck()
    {
        test = (myCharacter.curHealth / myCharacter.maxHealth) > HealthCheckThresh ? true : false;

        return test;
    }

    public bool RedPotential()
    {
        if (myAI.root == null)
            return false;

        test = myAI.root.redValue > RedPotentialThresh;

        return test;
    }

    public bool GreenPotential()
    {
        if (myAI.root == null)
            return false;

        test = myAI.root.greenValue > GreenPotentialThresh;

        return test;
    }

    public bool BluePotential()
    {
        if (myAI.root == null)
            return false;

        test = myAI.root.blueValue > BluePotentialThresh;

        return test;
    }

    public bool TimeCheck()
    {
        return false;
    }

    public bool SpecialCheck()
    {
        return false;
    }

    public bool OpponentHealth()
    {
        test = (enemy.curHealth / enemy.maxHealth) > HealthCheckThresh ? true : false;

        return false;
    }

    public bool PatienceXAggression()
    {
        test = (patience + aggression) / 2 > (PatienceThresh + AggressionThresh) / 2 ? true : false;

        return test;
    }

    public bool AggressionCheck()
    {
        test = aggression > AggressionThresh ? true : false;

        return false;
    }

    public bool PatienceCheck()
    {
        test = patience > PatienceThresh ? true : false;

        return false;
    }

    public bool Scared()
    {
        test = bravery > ScaredThresh ? true : false;

        return false;
    }

    public bool EmptySpace()
    {
        int numEmpty = 0;

        if (myAI.root == null)
            return false;

        for (int y = 0; y < myAI.boardHeight; y++)
        {
            for (int x = 0; x < myAI.boardWidth; x++)
            {
                if (myAI.root.board[x, y].state == TileState.EMPTY)
                {
                    numEmpty++;
                }
            }
        }

        test = (numEmpty / myAI.boardWidth * myAI.boardHeight) > EmptySpaceThresh ? true : false;

        return test;
    }

    public bool WillFillRed()
    {
        if (myAI.root == null)
            return false;

        if (myCharacter.redCount + myAI.root.redValue > myCharacter.redMax)
            return true;

        return false;
    }

    public bool WillFillGreen()
    {
        if (myAI.root == null)
            return false;

        if (myCharacter.greenCount + myAI.root.greenValue > myCharacter.greenMax)
            return true;

        return false;
    }

    public bool WillFillBlue()
    {
        if (myAI.root == null)
            return false;

        if (myCharacter.blueCount + myAI.root.blueValue > myCharacter.blueMax)
            return true;

        return false;
    }

    public bool FullHealth()
    {
        return myCharacter.curHealth >= myCharacter.maxHealth;
    }

    public bool WillFillHealth()
    {
        // To do: 
        return false;
    }

    // Action Functions
    public void Idle()
    {
        return;
    }

    public void PrintLists()
    {
        string s = "";
        LinkedListNode<string> curName = allDecisionNames.First;
        LinkedListNode<DecisionTree> curNode = allDecisionNodes.First;

        for (int i = 0; i < allDecisionNodes.Count; i++)
        {
            s = s + curName.Value;
            s = s + " : ";
            s = s + curNode.Value.debugCallCount.ToString();
            s = s + '\n';

            curName = curName.Next;
            curNode = curNode.Next;
        }

        print(s);
    }


    public void TrackNode(string nodeName, DecisionTree node)
    {
        allDecisionNames.AddLast(nodeName);
        allDecisionNodes.AddLast(node);
    }

    public void BuildDecisionTrees()
    {

        // Root Nodes
        ShouldIAttack = new DecisionTree();
        ShouldIHeal = new DecisionTree();
        ShouldISpecial = new DecisionTree();
        ShouldIPopRed = new DecisionTree();
        ShouldIPopGreen = new DecisionTree();
        ShouldIPopBlue = new DecisionTree();

        // Leaf Nodes
        DecisionTree AttackNode = new DecisionTree();
        DecisionTree HealNode = new DecisionTree();
        DecisionTree SpecialNode = new DecisionTree();
        DecisionTree PopRedNode = new DecisionTree();
        DecisionTree PopGreenNode = new DecisionTree();
        DecisionTree PopBlueNode = new DecisionTree();
        DecisionTree IdleNode = new DecisionTree();

        AttackNode.SetAction(myCharacter.Attack);
        HealNode.SetAction(myCharacter.Heal);
        SpecialNode.SetAction(myCharacter.Special);
        PopRedNode.SetAction(myCharacter.PopRed);
        PopGreenNode.SetAction(myCharacter.PopGreen);
        PopBlueNode.SetAction(myCharacter.PopBlue);
        IdleNode.SetAction(Idle);

        /**
         * Special?
         */
        TrackNode("Special_Idle", ShouldISpecial);
        ShouldISpecial.SetAction(Idle);

        /**
         * Should I Attack?
         */
        TrackNode("Attack_CanAttack", ShouldIAttack);
        ShouldIAttack.SetDecision(CanAttack);
        ShouldIAttack.SetLeft(IdleNode);

        DecisionTree OpponentHealthNode = new DecisionTree();
        TrackNode("Attack_OpponentHealthNode", OpponentHealthNode);
        OpponentHealthNode.SetDecision(OpponentHealth);
        ShouldIAttack.SetRight(OpponentHealthNode);

        DecisionTree CanKillNode = new DecisionTree();
        TrackNode("Attack_CanKillNode", CanKillNode);
        CanKillNode.SetDecision(CanKill);
        OpponentHealthNode.SetLeft(CanKillNode);

        DecisionTree PatienceNode = new DecisionTree();
        TrackNode("Attack_PatienceNode", PatienceNode);
        PatienceNode.SetDecision(PatienceCheck);
        CanKillNode.SetLeft(PatienceNode);

        PatienceNode.SetLeft(AttackNode);

        PatienceNode.SetRight(IdleNode);

        CanKillNode.SetRight(AttackNode);

        DecisionTree RedCountNode = new DecisionTree();
        TrackNode("Attack_RedCountNode", RedCountNode);
        RedCountNode.SetDecision(RedCount);
        OpponentHealthNode.SetRight(RedCountNode);

        RedCountNode.SetLeft(IdleNode);

        DecisionTree SpecialInPlayNode = new DecisionTree();
        TrackNode("Attack_SpecialInPlayNode", SpecialInPlayNode);
        SpecialInPlayNode.SetDecision(SpecialCheck);
        RedCountNode.SetRight(SpecialInPlayNode);

        SpecialInPlayNode.SetLeft(AttackNode);

        SpecialInPlayNode.SetRight(IdleNode);

        /**
         * Should I Heal?
         */
        TrackNode("Heal_FullHealth", ShouldIHeal);

        ShouldIHeal.SetDecision(FullHealth);

        ShouldIHeal.SetRight(IdleNode);

        DecisionTree WillFillHealthNode = new DecisionTree();
        TrackNode("Heal_WillFillHealthNode", WillFillHealthNode);
        WillFillHealthNode.SetDecision(WillFillHealth);
        ShouldIHeal.SetLeft(WillFillHealthNode);

        WillFillHealthNode.SetRight(HealNode);

        DecisionTree ScaredNode = new DecisionTree();
        TrackNode("Heal_ScaredNode", ScaredNode);
        ScaredNode.SetDecision(Scared);
        WillFillHealthNode.SetLeft(ScaredNode);

        ScaredNode.SetLeft(IdleNode);

        ScaredNode.SetRight(HealNode);

        /**
         * Should I Pop Red?
         */
        TrackNode("PopRed_redPotential", ShouldIPopRed);

        ShouldIPopRed.SetDecision(RedPotential);

        DecisionTree WillFillRedNode = new DecisionTree();
        TrackNode("PopRed_WillFillRedNode", WillFillRedNode);
        WillFillRedNode.SetDecision(WillFillRed);
        ShouldIPopRed.SetLeft(WillFillRedNode);

        RedCountNode = new DecisionTree();
        TrackNode("PopRed_RedCountNode", RedCountNode);
        RedCountNode.SetDecision(RedCount);
        ShouldIPopRed.SetRight(RedCountNode);

        DecisionTree EmptySpaceNode = new DecisionTree();
        TrackNode("PopRed_EmptySpaceNode", EmptySpaceNode);
        EmptySpaceNode.SetDecision(EmptySpace);
        WillFillRedNode.SetLeft(EmptySpaceNode);

        EmptySpaceNode.SetRight(IdleNode);

        EmptySpaceNode.SetLeft(PopRedNode);

        WillFillRedNode.SetRight(PopRedNode);

        RedCountNode.SetLeft(IdleNode);

        DecisionTree TempermentNode = new DecisionTree();
        TrackNode("PopRed_TempermentNode", TempermentNode);
        TempermentNode.SetDecision(PatienceXAggression);
        RedCountNode.SetRight(TempermentNode);

        TempermentNode.SetLeft(PopRedNode);

        TempermentNode.SetRight(IdleNode);

        /**
         * Should I Pop Green?
         */
        TrackNode("Attack_CanKillNode", ShouldIPopGreen);

        ShouldIPopGreen.SetDecision(GreenPotential);

        DecisionTree WillFillGreenNode = new DecisionTree();
        TrackNode("PopGreen_WillFillGreenNode", WillFillGreenNode);
        WillFillGreenNode.SetDecision(WillFillGreen);
        ShouldIPopGreen.SetLeft(WillFillGreenNode);

        DecisionTree GreenCountNode = new DecisionTree();
        TrackNode("PopGreen_GreenCountNode", GreenCountNode);
        GreenCountNode.SetDecision(GreenCount);
        ShouldIPopGreen.SetRight(GreenCountNode);

        EmptySpaceNode = new DecisionTree();
        TrackNode("PopGreen_EmptySpaceNode", EmptySpaceNode);
        EmptySpaceNode.SetDecision(EmptySpace);
        WillFillGreenNode.SetLeft(EmptySpaceNode);

        EmptySpaceNode.SetRight(IdleNode);

        EmptySpaceNode.SetLeft(PopGreenNode);

        WillFillGreenNode.SetRight(PopGreenNode);

        GreenCountNode.SetLeft(IdleNode);

        TempermentNode = new DecisionTree();
        TrackNode("PopGreen_TempermentNode", TempermentNode);
        TempermentNode.SetDecision(PatienceXAggression);
        GreenCountNode.SetRight(TempermentNode);

        TempermentNode.SetLeft(PopGreenNode);

        TempermentNode.SetRight(IdleNode);

        /**
         * Should I Pop Blue?
         */
        TrackNode("Attack_CanKillNode", ShouldIPopBlue);

        ShouldIPopBlue.SetDecision(BluePotential);

        DecisionTree WillFillBlueNode = new DecisionTree();
        TrackNode("PopBlue_WillFillBlueNode", WillFillBlueNode);
        WillFillBlueNode.SetDecision(WillFillBlue);
        ShouldIPopBlue.SetLeft(WillFillBlueNode);

        DecisionTree BlueCountNode = new DecisionTree();
        TrackNode("PopBlue_BlueCountNode", BlueCountNode);
        BlueCountNode.SetDecision(BlueCount);
        ShouldIPopBlue.SetRight(BlueCountNode);

        EmptySpaceNode = new DecisionTree();
        TrackNode("PopBlue_EmptySpaceNode", EmptySpaceNode);
        EmptySpaceNode.SetDecision(EmptySpace);
        WillFillBlueNode.SetLeft(EmptySpaceNode);

        EmptySpaceNode.SetRight(IdleNode);

        EmptySpaceNode.SetLeft(PopBlueNode);

        WillFillBlueNode.SetRight(PopBlueNode);

        BlueCountNode.SetLeft(IdleNode);

        TempermentNode = new DecisionTree();
        TrackNode("PopBlue_TempermentNode", TempermentNode);
        TempermentNode.SetDecision(PatienceXAggression);
        BlueCountNode.SetRight(TempermentNode);

        TempermentNode.SetLeft(PopBlueNode);

        TempermentNode.SetRight(IdleNode);
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        ShouldIAttack.Search();
        ShouldIHeal.Search();
        ShouldISpecial.Search();
        ShouldIPopRed.Search();
        ShouldIPopGreen.Search();
        ShouldIPopBlue.Search();
    }
}
