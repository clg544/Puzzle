using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCharacter : MonoBehaviour {

	// Use this for initialization
	void Start () {
        // Define root nodes
        DecisionTree Attack = new DecisionTree();
        DecisionTree Heal = new DecisionTree();
        DecisionTree Special = new DecisionTree();
        DecisionTree PopRed = new DecisionTree();
        DecisionTree PopGreen = new DecisionTree();
        DecisionTree PopBlue = new DecisionTree();

        //Define All Decision Nodes
        DecisionTree RedCountNode = new DecisionTree();
        DecisionTree greenCountNode = new DecisionTree();
        DecisionTree BlueCountNode = new DecisionTree();
        DecisionTree RedPotentialNode = new DecisionTree();
        DecisionTree GreenPotentialNode = new DecisionTree();
        DecisionTree BluePotentialNode = new DecisionTree();
        DecisionTree PollHealthNode = new DecisionTree();
        DecisionTree SpecialIsRelevent = new DecisionTree();
        DecisionTree EmptySpaceNode = new DecisionTree();
        DecisionTree AggressionNode = new DecisionTree();
        DecisionTree PatienceNode = new DecisionTree();


    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
