using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileScript : MonoBehaviour {
    public GameBoardScript TileManager;
    public TileScript TileBelow;
    public TileScript TileAbove;
    public TileScript TileLeft;
    public TileScript TileRight;
    
    public Sprite EmptySprite;
    public Sprite RedSprite;
    public Sprite GreenSprite;
    public Sprite BlueSprite;
    public Sprite YellowSprite;
    public SpriteRenderer mySprite;
    
    public enum TileState
    {
        RED,
        GREEN,
        BLUE,
        YELLOW,
        EMPTY
    }

    // ID info
    public int tileID;
    public int x_coor;
    public int y_coor;

    // Game State info
    public TileState curState;
    public bool isActive = false;       // Is this in player control?
    public bool isPivot = false;        // Is this the Tile to rotate around?
    public bool isGrounded = false;     // Is this tile falling?
    public bool killFlag = false;
    
    public TileScript RightRotation;
    public TileScript LeftRotation;

    public void Reset()
    {
        isActive = false;
        isPivot = false;
        isGrounded = false;
        killFlag = false;

        TileScript curTile = TileAbove;
        while (curTile != null)
        {
            curTile.isGrounded = false;
            curTile = curTile.TileAbove;
        }

        this.setState(TileState.EMPTY);
    }

    public void GenerateTileID()
    {
        if(x_coor == -1 && y_coor == -1)
        {
            Debug.Log("TileScript:GenerateTileID:Attemted to generate before coordinates were assigned!");
        }

        tileID = (y_coor * 10000) + (x_coor);
    }

    public bool CanMoveLeft()
    {
        if (y_coor >= TileManager.boardHeight)
            return false;

        if (x_coor == 0)
            return false;

        if (TileLeft.isGrounded)
            return false;

        if (TileLeft.curState == TileState.EMPTY)
            return true;
        else 
            return TileLeft.CanMoveLeft();
    }
    
    public bool CanMoveRight()
    {
        if (y_coor >= TileManager.boardHeight)
            return false;

        if (x_coor == TileManager.boardWidth - 1)
            return false;

        if (TileRight.isGrounded)
            return false;

        if (TileRight.curState == TileState.EMPTY)
            return true;
        else
            return TileRight.CanMoveRight();
    }

    // Use this for initialization
    void Start ()
    {
        mySprite = gameObject.GetComponent<SpriteRenderer>();
        transform.localScale = new Vector2(TileManager.tileWidth, TileManager.tileWidth);

        setState(TileState.EMPTY);
	}
	
	// Update is called once per frame
	void Update () {

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
}
