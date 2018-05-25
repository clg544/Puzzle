using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileState
{
    RED,
    GREEN,
    BLUE,
    YELLOW,
    EMPTY
}

public class TileScript : MonoBehaviour {

    public GameBoardScript TileManager;
    public TileScript TileBelow;
    public TileScript TileAbove;
    public TileScript TileLeft;
    public TileScript TileRight;

    public Sprite[] spriteSheet;
    public SpriteRenderer mySprite;

    // ID info
    public int tileID;
    public int x_coor;
    public int y_coor;

    public const int leftMatch  = 0x1;
    public const int aboveMatch = 0x2;
    public const int rightMatch = 0x4;
    public const int belowMatch = 0x8;
    public int matchFlag;

    public int connectionWidth;
    public int connectionHeight;
    
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
        matchFlag = 0;

        // Unground all tiles above here.
        TileScript curTile = TileAbove;
        while (curTile != null)
        {
            curTile.isGrounded = false;
            curTile = curTile.TileAbove;
        }

        RemoveAdjacencies();
        this.setState(TileState.EMPTY);
        UpdateSprite();
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

    public void RemoveAdjacencies()
    {
        matchFlag = 0;
        if(TileLeft != null)
        {
            TileLeft.matchFlag = TileLeft.matchFlag & (~rightMatch);
            TileLeft.UpdateSprite();
        }
        if (TileAbove != null)
        {
            TileAbove.matchFlag = TileAbove.matchFlag & (~belowMatch);
            TileAbove.UpdateSprite();
        }
        if (TileRight != null)
        {
            TileRight.matchFlag = TileRight.matchFlag & (~leftMatch);
            TileRight.UpdateSprite();
        }
        if (TileBelow != null)
        {
            TileBelow.matchFlag = TileBelow.matchFlag & (~aboveMatch);
            TileBelow.UpdateSprite();
        }
    }
    
    public void setState(TileState newState)
    {
        curState = newState;
        UpdateColor();
    }

    public void UpdateColor()
    {
        switch (curState)
        {
            case TileState.RED:
                mySprite.color = Color.red;
                break;
            case TileState.GREEN:
                mySprite.color = Color.green;
                break;
            case TileState.BLUE:
                mySprite.color = Color.blue;
                break;
            case TileState.YELLOW:
                mySprite.color = Color.yellow;
                break;
            case TileState.EMPTY:
                mySprite.color = Color.white;
                break;
        }
    }

    public void UpdateSprite()
    {
        if (curState == TileState.EMPTY)
            matchFlag = 0;

        mySprite.sprite = spriteSheet[matchFlag];
    }

    public void SetAdjacencies()
    {
        // If empty, reset and return...
        if(curState == TileState.EMPTY)
        {
            matchFlag = 0;  // No Neighbors
            mySprite.sprite = spriteSheet[matchFlag];
            return;
        }

        // else, check for neighbors
        if (TileLeft != null)
        {
            if (curState == TileLeft.curState && TileLeft.isGrounded)
            {
                matchFlag = matchFlag | leftMatch;
                TileLeft.matchFlag = TileLeft.matchFlag | rightMatch;
                TileLeft.UpdateSprite();
            }

        }

        if (TileAbove != null)
        {
            if (curState == TileAbove.curState && TileAbove.isGrounded)
            {
                matchFlag = matchFlag | aboveMatch;
                TileAbove.matchFlag = TileAbove.matchFlag | belowMatch;
                TileAbove.UpdateSprite();
            }
        }

        if (TileRight != null)
        {
            if (curState == TileRight.curState && TileRight.isGrounded)
            {
                matchFlag = matchFlag | rightMatch;
                TileRight.matchFlag = TileRight.matchFlag | leftMatch;
                TileRight.UpdateSprite();
            }
        }

        if (TileBelow != null)
        {
            if (curState == TileBelow.curState && TileBelow.isGrounded)
            {
                matchFlag = matchFlag | belowMatch;
                TileBelow.matchFlag = TileBelow.matchFlag | aboveMatch;
                TileBelow.UpdateSprite();
            }
        }

        UpdateSprite();
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
}
