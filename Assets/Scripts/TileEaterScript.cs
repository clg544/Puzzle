using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileEaterScript : MonoBehaviour
{
    public Sprite EmptySprite;
    public Sprite RedSprite;
    public Sprite GreenSprite;
    public Sprite BlueSprite;
    public Sprite YellowSprite;
    public SpriteRenderer mySprite;

    public TileState curState;

    public void Reset()
    {
        setState(TileState.EMPTY);
    }

    /* Destroy connected tiles */
    public void EatTiles()
    {

    }

    public void setState(TileState newState)
    {
        curState = newState;
        
        /* Do something on state change */
        switch (newState)
        {
            case TileState.RED:
                mySprite.sprite = RedSprite;
                mySprite.color = Color.red;
                break;

            case TileState.GREEN:
                mySprite.sprite = GreenSprite;
                mySprite.color = Color.green;
                break;

            case TileState.BLUE:
                mySprite.sprite = BlueSprite;
                mySprite.color = Color.blue;
                break;

            case TileState.YELLOW:
                mySprite.sprite = YellowSprite;
                mySprite.color = Color.yellow;
                break;

            case TileState.EMPTY:
                mySprite.sprite = EmptySprite;
                mySprite.color = Color.white;
                break;
        }
    }

    // Use this for initialization
    void Start () {
        mySprite = GetComponent<SpriteRenderer>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
