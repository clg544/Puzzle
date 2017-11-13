using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameBoardScript : MonoBehaviour
{
    public GameObject Tile;
    public GameObject TileEater;
    public bool killSem;
    
    public SortedList<int, TileScript> FallingTiles;        // Sorted by TileID  int:yyyyxxxx
    public SortedList<int, TileScript> ActiveTiles;
    public LinkedList<TileScript> TempTiles;
    public SortedList<int, TileScript> KillTiles;           // Sorted by -TileID, aka top-to-bottom

    public float tileDropRate;          // In Seconds
    public float fastDropRate;
    public float tileSpawnRate = 0;     // In Seconds
    public float dropTime;
    public float spawnTime;

    public Slider popSlider;

    public float cooldownMax;
    public float popCooldown;
    /*
    public float redCooldown;
    public float greenCooldown;
    public float blueCooldown;
    */

    public float bentChance;

    public int boardWidth = 0;
    public int boardHeight = 0;
    
    public float tileWidth;       // Assume tiles are square

    public TileScript entryPoint;
    public TileScript[,] BoardTileScripts;
    public TileEaterScript[] TileEaterScripts;
    public TileScript[,] SpawnArea;

    private void UpdateListsFromTemp()
    {
        /* Update the CurrentTile Lists */
        FallingTiles.Clear();
        ActiveTiles.Clear();
        foreach (TileScript curTile in TempTiles)
        {
            if (!curTile.isGrounded)
                FallingTiles.Add(curTile.tileID, curTile);

            if (curTile.isActive)
                ActiveTiles.Add(curTile.tileID, curTile);
        }
        TempTiles.Clear();    // Clean up after ourselves!
    }
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
        if (tileToMove.TileLeft.curState != TileScript.TileState.EMPTY)
            ShoveTilesLeft(tileToMove.TileLeft);
        // Else we were lied to by CanMoveLeft.;

        tileToMove.TileLeft.setState(tileToMove.curState);
        tileToMove.setState(TileScript.TileState.EMPTY);

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
        if (tileToMove.TileRight.curState != TileScript.TileState.EMPTY)
            ShoveTilesRight(tileToMove.TileRight);
        // Else we were lied to by CanMoveRight.

        tileToMove.TileRight.setState(tileToMove.curState);
        tileToMove.setState(TileScript.TileState.EMPTY);

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

                if(pivot.TileRight.curState == TileScript.TileState.EMPTY)
                {
                    Relocate(pivot.TileAbove, pivot.TileRight);
                }
                break;
            // Right Only
            case (2):
                if (pivot.TileBelow == null)
                    return;

                if (pivot.TileBelow.curState == TileScript.TileState.EMPTY)
                {
                    Relocate(pivot.TileRight, pivot.TileBelow);
                }
                break;
            // Below Only
            case (4):
                if (pivot.TileLeft == null)
                    return;

                if (pivot.TileLeft.curState == TileScript.TileState.EMPTY)
                {
                    Relocate(pivot.TileBelow, pivot.TileLeft);
                }
                break;
            // Left Only
            case (8):
                if (pivot.TileAbove == null)
                    return;

                if (pivot.TileAbove.curState == TileScript.TileState.EMPTY)
                {
                    Relocate(pivot.TileLeft, pivot.TileAbove);
                }
                break;
            // Up & Right (1 + 2)
            case (3):
                if (pivot.TileAbove.TileRight == null)
                    return;

                if (pivot.TileAbove.TileRight.curState == TileScript.TileState.EMPTY)
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

                if (pivot.TileRight.TileBelow.curState == TileScript.TileState.EMPTY)
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

                if (pivot.TileBelow.TileLeft.curState == TileScript.TileState.EMPTY)
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

                if (pivot.TileAbove.TileLeft.curState == TileScript.TileState.EMPTY)
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

                if (pivot.TileAbove.curState == TileScript.TileState.EMPTY && pivot.TileBelow.curState == TileScript.TileState.EMPTY)
                {
                    Relocate(pivot.TileLeft, pivot.TileAbove);
                    Relocate(pivot.TileRight, pivot.TileBelow);
                }
                break;
            // Above and Below (1 + 4)
            case (5):
                if (pivot.TileLeft == null || pivot.TileRight == null)
                    return;

                if (pivot.TileLeft.curState == TileScript.TileState.EMPTY && pivot.TileRight.curState == TileScript.TileState.EMPTY)
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



    public void ApplyGravity(TileScript tile)
    {

        if (tile.curState == TileScript.TileState.EMPTY)
        {
            print("Empty return.");
            return;
        }
        if (FallingTiles.ContainsKey(tile.tileID))
        {
            print("FallingTile Return");
            return;
        }

        FallingTiles.Add(tile.tileID, tile);
        tile.isGrounded = false;
        ApplyGravity(tile.TileAbove);
    }

    public void PopTiles(TileScript.TileState hitState)
    {
        if (popCooldown < cooldownMax)
            return;
        else
            popCooldown = 0;

        LinkedList<TileScript> TilesToCheck = new LinkedList<TileScript>();

        /* For all tiles at (x, 0) */
        for(int i = 0; i < boardWidth; i++)
        {
            /* if the color matches, and is grounded, add it to the kill list */
            if(BoardTileScripts[i, 0].curState == hitState && BoardTileScripts[i, 0].isGrounded)
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
        while(curNode != null)
        {

            immigrant = curNode.Value.TileLeft;
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

            immigrant = curNode.Value.TileBelow;
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

            immigrant = curNode.Value.TileRight;
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

            immigrant = curNode.Value.TileAbove;
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

            curNode = curNode.Next;
        }
        foreach(TileScript tile in KillTiles.Values)
        {
            tile.Reset();
        }
        
        for (int i = 0; i < boardWidth; i++)
        {
            if (TileEaterScripts[i].curState == hitState)
            {
                TileEaterScripts[i].Reset();
            }
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
            else if (tile.curState == TileScript.TileState.EMPTY)
            {
                //print("Curstate is empty:" + tile.tileID);
            }
            else if (tile.TileBelow != null)
            {
                if (!tile.TileBelow.isGrounded)
                {
                    //print("falling.add:" + tile.tileID);
                    FallingTiles.Add(tile.tileID, tile);
                }
                else
                {
                    print("IsGrounded runoff");
                }
            }
            else
            {
                print("Poptile runoff");
            }
        }
        //ListDebug();

        TilesToCheck.Clear();
        KillTiles.Clear();
    }

    public void GroundTile(TileScript ts)
    {
        ts.isActive = false;
        ts.isPivot = false;
        ts.isGrounded = true;

        if (ts.y_coor == 0)
        {
            TileEaterScripts[ts.x_coor].setState(ts.curState);
        }
    }
    public void DropAllTiles()
    {
        dropTime = 0;
        // Set all the new tiles
        foreach (TileScript curTile in FallingTiles.Values)
        {
            // If we're at ground...
            if(curTile.curState == TileScript.TileState.EMPTY)
            {
                break;
            }
            if (curTile.y_coor == 0 || curTile.TileBelow.isGrounded)
            {
                GroundTile(curTile);              // Ground the tile,
            }
            // Else, If we're above an empty space...
            else if(curTile.TileBelow.curState == TileScript.TileState.EMPTY && curTile.curState != TileScript.TileState.EMPTY)
            {
                curTile.TileBelow.setState(curTile.curState);       // Move this tile's state down one
                curTile.setState(TileScript.TileState.EMPTY);       // Set this state to empty

                curTile.TileBelow.isActive = curTile.isActive;      // Carry Tile Active level down
                curTile.TileBelow.isPivot = curTile.isPivot;// Carry Tile Active level down
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
        if (newTile.curState == TileScript.TileState.EMPTY)
        {
            newTile.setState((TileScript.TileState)Random.Range(0, 3));
            newTile.isActive = true;

            FallingTiles.Add(newTile.tileID, newTile);
        }
    }
    public void SpawnTromino()
    {
        foreach(TileScript ts in SpawnArea)
        {
            if(ts.curState != TileScript.TileState.EMPTY)
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
        TileEaterScripts = new TileEaterScript[boardWidth];
        
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

            TileEaterScripts[i] = curObject.GetComponent<TileEaterScript>();

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

        

    }
    
    void Update()
    {
        if (popCooldown < cooldownMax)
        {
            popCooldown += Time.deltaTime;
            popSlider.value = popCooldown / cooldownMax;
        }

        // periodically fall
        if (dropTime > tileDropRate)
        {
            if (FallingTiles.Count == 0)
            {
                SpawnTromino();
            }
            else
            {
                DropAllTiles();
            }
        }
        else
        {
            dropTime += Time.deltaTime;
        }
    }
}
