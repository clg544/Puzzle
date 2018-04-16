using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameBoardScript : MonoBehaviour
{
    public Character myCharacter;

    // Instantiable Game Objects
    public GameObject Tile;
    public GameObject TileEater;

    // Organizing Lists
    public List<TileScript> AllTiles;
    public SortedList<int, TileScript> FallingTiles;        // Sorted by TileID  int:yyyyxxxx
    public SortedList<int, TileScript> ActiveTiles;
    public LinkedList<TileScript> TempTiles;
    public SortedList<int, TileScript> KillTiles;           // Sorted by -TileID, aka top-to-bottom
    
    public float tileSpawnRate = 0;     // In Seconds
    public float spawnTime;

    // cooldown for cashing tile matches
    public float cooldownMax;
    public float popCooldown;
    
    // How likely a tile is to be an L-piece
    public float bentChance;

    // Board Dimensions
    public int boardWidth = 0;
    public int boardHeight = 0;
    
    // Tile size
    public float tileWidth;       // Assume tiles are square

    // starting position
    public float anchorX;
    public float anchorY;
    
    public TileScript entryPoint;
    public TileScript[,] BoardTileScripts;
    public TileScript[,] SpawnArea;

    private void UpdateListsFromTemp()
    {
        /* Update the CurrentTile Lists */
        FallingTiles.Clear();
        ActiveTiles.Clear();

        // Remake both lists from our Temp Values
        foreach (TileScript curTile in TempTiles)
        {
            if (!curTile.isGrounded)
            {
                FallingTiles.Add(curTile.tileID, curTile);
                curTile.RemoveAdjacencies();
            }
            if (curTile.isActive)
                ActiveTiles.Add(curTile.tileID, curTile);
        }
        TempTiles.Clear();    // Clean up after ourselves!
    }

    // Move a tile from one position to another
    public void Relocate(TileScript from, TileScript to)
    {
        to.isActive = from.isActive;
        to.isPivot = from.isPivot;
        to.setState(from.curState);

        if (ActiveTiles.ContainsKey(from.tileID))
        {
            ActiveTiles.Remove(from.tileID);
            ActiveTiles.Add(to.tileID, to);
        }
        if (FallingTiles.ContainsKey(from.tileID))
        {
            FallingTiles.Remove(from.tileID);
            FallingTiles.Add(to.tileID, to);
        }

        from.Reset();
    }

    /* Place the bottom left corner of the board */
    public void MoveGameBoard(Vector3 newPos)
    {
        gameObject.transform.position = newPos;

        // adjust for tile-eaters
        gameObject.transform.position += new Vector3(0, tileWidth, 0);

    }
    public void MoveLeft()
    {
        TileScript tile;
        
        // Must move all or none
        foreach (TileScript curTile in ActiveTiles.Values)
        {
            if (!curTile.CanMoveLeft())
                return;
        }

        for (int i = 0; i < FallingTiles.Count; i++)
        {
            tile = FallingTiles.Values[i];

            if (!(tile.isActive)){              // This isn't a player tile, Re-add it and quit
                TempTiles.AddFirst(tile);	
			}
            else if (tile.CanMoveLeft())        // Add the left tile to ou
            {
                ShoveTilesLeft(tile);
                TempTiles.AddFirst(tile.TileLeft);
            }
            else                                // Re add the current tile to the list
            {
                TempTiles.AddFirst(tile);
            }
        }

        /* Update the Current Tile Lists */
        UpdateListsFromTemp();
    }
    public void ShoveTilesLeft(TileScript tileToMove)
    {
        if (tileToMove.TileLeft.curState != TileState.EMPTY)
            ShoveTilesLeft(tileToMove.TileLeft);
        // Else we were lied to by CanMoveLeft

        tileToMove.TileLeft.setState(tileToMove.curState);
        tileToMove.setState(TileState.EMPTY);

        if (tileToMove.isActive)
        {
            tileToMove.isActive = false;
            tileToMove.TileLeft.isActive = true;
        }

        if (tileToMove.isPivot)
        {
            tileToMove.isPivot = false;
            tileToMove.TileLeft.isPivot = true;
        }
    }
    public void MoveRight()
    {
        TileScript tile;

        // Must move all or none
        foreach(TileScript curTile in ActiveTiles.Values)
        {
            if (!curTile.CanMoveRight())
                return;
        }

        for(int i = FallingTiles.Count - 1; i >=0; --i)
        {
            tile = FallingTiles.Values[i];

            if (!(tile.isActive))               // This isn't a player tile, Re-add it and quit
            {              
                TempTiles.AddFirst(tile);
            }
            else if (tile.CanMoveRight())        // Add the left tile to ou
            {
                ShoveTilesRight(tile);
                TempTiles.AddFirst(tile.TileRight);
            }
            else                                // Re add the current tile to the list
            {
                TempTiles.AddFirst(tile);
            }
        }
        
        /* Update the Current Tile Lists */
        UpdateListsFromTemp();
    }
    public void ShoveTilesRight(TileScript tileToMove)
    {
        if (tileToMove.TileRight.curState != TileState.EMPTY)
            ShoveTilesRight(tileToMove.TileRight);
        // Else we were lied to by CanMoveRight.

        tileToMove.TileRight.setState(tileToMove.curState);
        tileToMove.setState(TileState.EMPTY);

        if (tileToMove.isActive)
        {
            tileToMove.isActive = false;
            tileToMove.TileRight.isActive = true;
        }

        if (tileToMove.isPivot)
        {
            tileToMove.isPivot = false;
            tileToMove.TileRight.isPivot = true;
        }
    }
        
    public void RotateTrominoRight()
    {
        TileScript pivot = null;
        int shapeDescriptor = 0;
        
        // Nothing to do
        if (ActiveTiles.Count <= 1)
            return;

        // Find the tile we're pivoting around
        foreach(TileScript tile in ActiveTiles.Values)
        {
            if (tile.isPivot)
            {
                pivot = tile;
                break;
            }
        }

        if (pivot == null)
            return;
        
        // Descripe object shape in binary 
        foreach(TileScript tile in ActiveTiles.Values)
        {
            if(tile == pivot.TileAbove)
            {
                shapeDescriptor = shapeDescriptor | 1;
            }
            else if (tile == pivot.TileRight)
            {
                shapeDescriptor = shapeDescriptor | 2;
            }
            else if(tile == pivot.TileBelow)
            {
                shapeDescriptor = shapeDescriptor | 4;
            }
            else if (tile == pivot.TileLeft)
            {
                shapeDescriptor = shapeDescriptor | 8;
            }
        }

        // Use descriptor to perform rotation
        switch (shapeDescriptor)
        {
            // Up Only
            case (1):
                if(pivot.TileRight == null)
                    return;

                if(pivot.TileRight.curState == TileState.EMPTY)
                {
                    Relocate(pivot.TileAbove, pivot.TileRight);
                }
                break;
            // Right Only
            case (2):
                if (pivot.TileBelow == null)
                    return;

                if (pivot.TileBelow.curState == TileState.EMPTY)
                {
                    Relocate(pivot.TileRight, pivot.TileBelow);
                }
                break;
            // Below Only
            case (4):
                if (pivot.TileLeft == null)
                    return;

                if (pivot.TileLeft.curState == TileState.EMPTY)
                {
                    Relocate(pivot.TileBelow, pivot.TileLeft);
                }
                break;
            // Left Only
            case (8):
                if (pivot.TileAbove == null)
                    return;

                if (pivot.TileAbove.curState == TileState.EMPTY)
                {
                    Relocate(pivot.TileLeft, pivot.TileAbove);
                }
                break;
            // Up & Right (1 + 2)
            case (3):
                if (pivot.TileAbove.TileRight == null)
                    return;

                if (pivot.TileAbove.TileRight.curState == TileState.EMPTY)
                {
                    Relocate(pivot.TileAbove, pivot.TileAbove.TileRight);
                    Relocate(pivot, pivot.TileAbove);
                    Relocate(pivot.TileRight, pivot);
                }
                break;
            // Bottom & Right (2 + 4)
            case (6):
                if (pivot.TileRight.TileBelow == null)
                    return;

                if (pivot.TileRight.TileBelow.curState == TileState.EMPTY)
                {
                    Relocate(pivot.TileRight, pivot.TileRight.TileBelow);
                    Relocate(pivot, pivot.TileRight);
                    Relocate(pivot.TileBelow, pivot);
                }
                break;
            // Bottom & Left (4 + 8)
            case (12):
                if (pivot.TileBelow.TileLeft == null)
                    return;

                if (pivot.TileBelow.TileLeft.curState == TileState.EMPTY)
                {
                    Relocate(pivot.TileBelow, pivot.TileBelow.TileLeft);
                    Relocate(pivot, pivot.TileBelow);
                    Relocate(pivot.TileLeft, pivot);
                }
                break;
            // Up & Left (8 + 1)
            case (9):
                if (pivot.TileAbove.TileLeft == null)
                    return;

                if (pivot.TileAbove.TileLeft.curState == TileState.EMPTY)
                {
                    Relocate(pivot.TileLeft, pivot.TileAbove.TileLeft);
                    Relocate(pivot, pivot.TileLeft);
                    Relocate(pivot.TileAbove, pivot);
                }
                break;
            // Left and Right (8 + 2)
            case (10):
                if (pivot.TileAbove == null || pivot.TileBelow == null)
                    return;

                if (pivot.TileAbove.curState == TileState.EMPTY && pivot.TileBelow.curState == TileState.EMPTY)
                {
                    Relocate(pivot.TileLeft, pivot.TileAbove);
                    Relocate(pivot.TileRight, pivot.TileBelow);
                }
                break;
            // Above and Below (1 + 4)
            case (5):
                if (pivot.TileLeft == null || pivot.TileRight == null)
                    return;

                if (pivot.TileLeft.curState == TileState.EMPTY && pivot.TileRight.curState == TileState.EMPTY)
                {
                    Relocate(pivot.TileAbove, pivot.TileRight);
                    Relocate(pivot.TileBelow, pivot.TileLeft);
                }
                break;
        }
    }

    public void ListDebug()
    {
        //Debug info
        string debug = "";
        debug += "FallingTiles=";
        foreach (KeyValuePair<int, TileScript> tile in FallingTiles)
        {
            debug += tile.Value.tileID + ":";
        }
        Debug.Log(debug);

        debug = "";
        debug += "CurrentTiles=";
        foreach (KeyValuePair<int, TileScript> tile in FallingTiles)
        {
            if (tile.Value.isActive)
                debug += tile.Value.tileID + ":";
        }


        print(StackTraceUtility.ExtractStackTrace());
        Debug.Log(StackTraceUtility.ExtractStackTrace() + "\n" + debug);
    }

    public int MatchFlagValue(int tileMatchFlag)
    {
        // Bad Value
        if (tileMatchFlag > 15)
            return 0;

        switch (tileMatchFlag)
        {
            // Single Connection
            case 0:     // 0000
            case 1:     // 0001
            case 2:     // 0010
            case 4:     // 0100
            case 8:     // 1000
                return 1;

            // Two Connections
            case 3:     // 0011
            case 5:     // 0101
            case 6:     // 0110
            case 9:     // 1001
            case 10:    // 1010
            case 12:    // 1100
                return 2;

            // Three Connections
            case 7:     // 0111
            case 11:    // 1011
            case 13:    // 1101
            case 14:    // 1110
                return 4;

            // Four Connections
            case 15:    // 1111
                return 8;
        }

        return 0;
    }

    public int ScoreList(IList<TileScript> tileList)
    {
        int score = 0;

        foreach(TileScript tile in tileList)
        {
            score += MatchFlagValue(tile.matchFlag);

            tile.Reset();
        }
        
        return score;
    }

    public void PopTiles(TileState hitState)
    {
        if (popCooldown < cooldownMax)
            return;
        else
            popCooldown = 0;

        LinkedList<TileScript> TilesToCheck = new LinkedList<TileScript>();

        /* For all tiles at (x, 0) */
        for (int i = 0; i < boardWidth; i++)
        {
            /* if the color matches, and is grounded, add it to the kill list */
            if (BoardTileScripts[i, 0].curState == hitState && BoardTileScripts[i, 0].isGrounded)
            {
                // Mark for death...
                BoardTileScripts[i, 0].killFlag = true;
                KillTiles.Add(-BoardTileScripts[i, 0].tileID, BoardTileScripts[i, 0]);

                // Commit neighbors to be checked
                TilesToCheck.AddFirst(BoardTileScripts[i, 0]);
            }
        }

        /* Find all connected nodes to hitState nodes at (x, 0) */
        LinkedListNode<TileScript> curNode = TilesToCheck.First;
        TileScript immigrant;
        while (curNode != null)
        {
            immigrant = null;

            // For each side adjacent to the tile
            for (int i = 0; i < 4; i++)
            {
                switch (i)
                {
                    case 0:
                        immigrant = curNode.Value.TileLeft;
                        break;

                    case 1:
                        immigrant = curNode.Value.TileBelow;
                        break;

                    case 2:
                        immigrant = curNode.Value.TileRight;
                        break;

                    case 3:
                        immigrant = curNode.Value.TileAbove;
                        break;
                }

                // Check each neighbor
                if (immigrant != null)
                {
                    // If not already checked, matches the colour, and is not one of the player tiles
                    if (!(immigrant.killFlag) && immigrant.curState == hitState
                        && !ActiveTiles.ContainsKey(immigrant.tileID) && immigrant.isGrounded)
                    {
                        immigrant.killFlag = true;
                        KillTiles.Add(-immigrant.tileID, immigrant);
                        TilesToCheck.AddLast(immigrant);
                    }
                }
            }

            curNode = curNode.Next;
        }

        // Add the score to the right counter
        switch (hitState)
        {
            case TileState.RED:
                myCharacter.redCount += ScoreList(KillTiles.Values);
                break;
            case TileState.GREEN:
                myCharacter.greenCount += ScoreList(KillTiles.Values);
                break;
            case TileState.BLUE:
                myCharacter.blueCount += ScoreList(KillTiles.Values);
                break;
        }

        foreach (TileScript tile in BoardTileScripts)
        {
            if (tile.isActive)
            {
                //print("is active:" + tile.tileID);
            }
            else if (FallingTiles.ContainsKey(tile.tileID))
            {
                //print("in Fallingtiles:" + tile.tileID);
            }
            else if (tile.curState == TileState.EMPTY)
            {
                //print("Curstate is empty:" + tile.tileID);
            }
            else if (tile.TileBelow != null)
            {
                if (!tile.TileBelow.isGrounded)
                {
                    //print("falling.add:" + tile.tileID);
                    FallingTiles.Add(tile.tileID, tile);

                    tile.RemoveAdjacencies();
                    tile.UpdateSprite();
                }
            }
        }

        TilesToCheck.Clear();
        KillTiles.Clear();
    }


    public void MakeNewFallingTiles()
    {
        FallingTiles.Clear();

        foreach(TileScript t in AllTiles)
        {
            if(t.curState != TileState.EMPTY && !t.isGrounded)
            {
                FallingTiles.Add(t.tileID, t);
            }
        }
    }

    public void GroundTile(TileScript ts)
    {
        ts.isActive = false;
        ts.isPivot = false;
        ts.isGrounded = true;
        ts.SetAdjacencies();
    }
    public void DropAllTiles()
    {
        if (FallingTiles == null)
            return;

        if (FallingTiles.Count == 0)
            return;
        
        // Set all the new tiles
        foreach (TileScript curTile in FallingTiles.Values)
        {
            // If we're at ground...
            if(curTile.curState == TileState.EMPTY)
            {
                break;
            }
            if (curTile.y_coor == 0 || curTile.TileBelow.isGrounded)
            {
                GroundTile(curTile);              // Ground the tile,
            }
            // Else, If we're above an empty space...
            else if(curTile.TileBelow.curState == TileState.EMPTY && curTile.curState != TileState.EMPTY)
            {
                curTile.TileBelow.setState(curTile.curState);       // Move this tile's state down one
                curTile.setState(TileState.EMPTY);       // Set this state to empty

                curTile.TileBelow.isActive = curTile.isActive;      // Carry Tile Active level down
                curTile.TileBelow.isPivot = curTile.isPivot;        // Carry Tile Active level down
                curTile.isPivot = false;

                if (curTile.TileAbove != null)
                {
                    curTile.isActive = curTile.TileAbove.isActive;
                }
                else
                {    
                    curTile.isActive = false;
                }
                
                TempTiles.AddFirst(curTile.TileBelow);               // Add new tile to Temp list, to add to falling

                if (curTile.y_coor == 0 || curTile.TileBelow.isGrounded)
                {
                    GroundTile(curTile);              // Ground the tile,
                }
            }
        }

        /* Update the Current Tile Lists */
        UpdateListsFromTemp();
    }

    public void SpawnTile(TileScript newTile) {
        if (newTile.curState == TileState.EMPTY)
        {
            newTile.setState((TileState)Random.Range(0, 3));
            newTile.isActive = true;

            FallingTiles.Add(newTile.tileID, newTile);
        }
    }
    public void SpawnTromino()
    {
        foreach(TileScript ts in SpawnArea)
        {
            if(ts.curState != TileState.EMPTY)
            {
                // this.Loser();
                print("Player loses: not yet implemented");
            }
        }

        float rand = Random.Range(0.0F, 1.0F);

        if (rand < bentChance)
        {
            // Centre Tile
            SpawnTile(SpawnArea[1, 0]); 
            SpawnArea[1, 0].isPivot = true;

            // Top Tile
            SpawnTile(SpawnArea[1, 1]);
            SpawnArea[1, 1].RightRotation = SpawnArea[1, 0].TileRight;
            SpawnArea[1, 1].LeftRotation = SpawnArea[1, 0].TileLeft;

            if (rand < bentChance / 2)
            {
                // Right Tile
                SpawnTile(SpawnArea[2, 0]);
                SpawnArea[2, 0].RightRotation = SpawnArea[1, 0].TileBelow;
                SpawnArea[2, 0].LeftRotation = SpawnArea[1, 0].TileAbove;
            }
            else
            {
                // Left Tile
                SpawnTile(SpawnArea[0, 0]);
                SpawnArea[0, 0].RightRotation = SpawnArea[1, 0].TileAbove;
                SpawnArea[0, 0].LeftRotation = SpawnArea[1, 0].TileBelow;
            }
        }
        else
        {
            SpawnTile(SpawnArea[0, 0]); // ---
            SpawnTile(SpawnArea[1, 0]); // XXX
            SpawnTile(SpawnArea[2, 0]); 

            SpawnArea[1, 0].isPivot = true;
        } 
    }

    void Awake()
    {
        FallingTiles = new SortedList<int, TileScript>();
        ActiveTiles = new SortedList<int, TileScript>();
        TempTiles = new LinkedList<TileScript>();
        KillTiles = new SortedList<int, TileScript>();

        GameObject curObject;

        BoardTileScripts = new TileScript[boardWidth, boardHeight];
        
        for (int i = 0; i < boardWidth; i++)
        {
            for (int j = 0; j < boardHeight; j++)
            {
                curObject = Instantiate(Tile, new Vector3(tileWidth * i, tileWidth * j, 0), Quaternion.identity, gameObject.transform);
                BoardTileScripts[i, j] = curObject.GetComponent<TileScript>();

                BoardTileScripts[i, j].TileManager = this;

                BoardTileScripts[i, j].transform.localScale *= tileWidth;
                BoardTileScripts[i, j].x_coor = i;
                BoardTileScripts[i, j].y_coor = j;

                BoardTileScripts[i, j].GenerateTileID();
            }
        }

        /* Manually create the spawn area */ 
        SpawnArea = new TileScript[3, 2];

        int x, y;
        for(int i = 0; i < SpawnArea.Length; i++)
        {
            x = i % 3;
            y = i % 2;

            SpawnArea[x, y] = Instantiate(Tile, new Vector3(tileWidth * ((boardWidth/2) + (x - 1)), tileWidth * (boardHeight + y),0),
                Quaternion.identity, gameObject.transform).GetComponent<TileScript>();
            
            SpawnArea[x, y].x_coor = x;
            SpawnArea[x, y].y_coor = y;
        }

        SpawnArea[0, 0].x_coor = (boardWidth / 2) -1;
        SpawnArea[1, 0].x_coor = (boardWidth / 2) + 0;
        SpawnArea[2, 0].x_coor = (boardWidth / 2) + 1;
        SpawnArea[1, 1].x_coor = (boardWidth / 2) + 0;

        SpawnArea[0, 0].y_coor = boardHeight + 0;
        SpawnArea[1, 0].y_coor = boardHeight + 0;
        SpawnArea[2, 0].y_coor = boardHeight + 0;
        SpawnArea[1, 1].y_coor = boardHeight + 1;

        foreach (TileScript tile in SpawnArea)
        {
            tile.TileManager = this;
            tile.GenerateTileID();
        }
        
        /* Create the Tile Eaters */
        for (int i = 0; i < boardWidth; i++)
        {
            curObject = Instantiate(TileEater, new Vector3(tileWidth * i, 0), Quaternion.identity, gameObject.transform);
            
            curObject.transform.localScale *= tileWidth;
        }
    }
	void Start () {
        TileScript curTile;

        entryPoint = BoardTileScripts[boardWidth / 2, boardHeight - 1];
        
        for (int i = 0; i < boardWidth; i++)
        {
            for (int j = 0; j < boardHeight; j++)
            {
                curTile = BoardTileScripts[i, j];
                AllTiles.Add(curTile);

                if (j != 0)
                {
                    curTile.TileBelow = BoardTileScripts[i, j - 1];
                }
                if (j != boardHeight - 1)
                {
                    curTile.TileAbove = BoardTileScripts[i, j + 1];
                }
                if (i != 0)
                {
                    curTile.TileLeft = BoardTileScripts[i - 1, j];
                }
                if (i != boardWidth - 1)
                {
                    curTile.TileRight = BoardTileScripts[i + 1, j];
                }
            }
        }
        
        SpawnArea[1, 1].TileBelow = SpawnArea[1, 0];
        SpawnArea[1, 0].TileAbove = SpawnArea[1, 1];
        for (int i = 0; i < 3; i++)
        {
            curTile = SpawnArea[i, 0];

            curTile.TileBelow = BoardTileScripts[entryPoint.x_coor + (i - 1), entryPoint.y_coor];
            BoardTileScripts[entryPoint.x_coor + (i - 1), entryPoint.y_coor].TileAbove = curTile;
        }
        SpawnArea[0, 0].TileRight = SpawnArea[1, 0];
        SpawnArea[1, 0].TileLeft = SpawnArea[0, 0];

        SpawnArea[1, 0].TileRight = SpawnArea[2, 0];
        SpawnArea[2, 0].TileLeft = SpawnArea[1, 0];

        MoveGameBoard(new Vector3(anchorX, anchorY, 0));

    }
    
    void Update()
    {
        if (FallingTiles.Count == 0)
        {
            this.SpawnTromino();
        }

        if(popCooldown < cooldownMax)
        {
            popCooldown += Time.deltaTime;
        }
    }
}
