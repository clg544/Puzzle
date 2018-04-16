using UnityEngine;
using System.Collections;

public class DecisionTree
{
    public int debugCallCount;

    public delegate bool Decision();
    public delegate void Action();

    DecisionTree left;
    DecisionTree right;
    Decision myDecision;
    Action myAction;

    // Constructor
    public DecisionTree()
    {
        left = null;
        right = null;
        myDecision = null;
        myAction = null;

        debugCallCount = 0;
    }

    public void SetDecision(Decision dec)
    {
        myDecision = dec;
    }

    public void SetAction(Action act)
    {
        myAction = act;
    }

    public void SetLeft(DecisionTree t)
    {
        left = t;
    }

    public void SetRight(DecisionTree t)
    {
        right = t;
    }

    /****** Make Decisions *******/

    public bool Decide()
    {
        return myDecision();
    }

    public void goLeft()
    {
        left.Search();
    }

    public void goRight()
    {
        right.Search();
    }

    public void Search()
    {
        debugCallCount++;

        // recursive base case
        if (myAction != null)
        {
            myAction();
        }
        else if (Decide())
        {
            goRight();
        }
        else
        {
            goLeft();
        }
    }
}
