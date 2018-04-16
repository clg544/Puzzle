using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AI : MonoBehaviour
{

    // Board Description
    GameBoardScript myGameBoard;
    public int boardWidth;
    public int boardHeight;
    const int boardLeft = 0;
    const int boardBottom = 0;

    // A root node for each location
    public StateNode root;
    StateNode root90;
    StateNode root180;
    StateNode root270;

    // Value table for reference
    public int[,] emptyValueTable;

    // enum abstracting move options
    public enum MoveChoice { RIGHT, LEFT, ROTATE, FALL, WAIT };

    // A tree node to hold game states
    public class StateNode
    {
        public SubState[,] board;                // Array of all tiles
        public int value;                        // The maximum value of all leaves
        public int redValue;
        public int greenValue;
        public int blueValue;
        public List<int[]> active;               // If count > 0, not a leaf node

        public bool isLeft;                      // Is this node to the left of root?
        public int numRotations;

        public StateNode parent;
        public StateNode leftState;              // State result of moving left
        public StateNode belowState;             // State result of waiting to fall
        public StateNode rightState;             // State result of moving right

        public StateNode(SubState[,] board)
        {
            this.board = board;
            value = 0;
            redValue = 0;
            greenValue = 0;
            blueValue = 0;
            active = new List<int[]>();

            isLeft = false;

            parent = null;
            leftState = null;
            belowState = null;
            rightState = null;
        }
    }

    // State holder for possible tile data
    public class SubState
    {
        public TileState state = TileState.EMPTY;
        public bool isGrounded;
        public bool isActive;
        public bool isPivot;

        // Clear the state to default
        public void Reset()
        {
            this.state = TileState.EMPTY;
            this.isGrounded = false;
            this.isActive = false;
            this.isPivot = false;
        }

        public SubState()
        {
            this.state = TileState.EMPTY;
            this.isGrounded = false;
            this.isActive = false;
            this.isPivot = false;
        }
    }

    /**
     * SubState CopySubState(SubState fromState)
     * Return a new Substate with data matching fromState
     * 
     * fromState -  The state to copy
     * return    -  A new state with values copied from fromState
    **/
    public SubState CopySubState(SubState fromState)
    {
        SubState toState = new SubState();

        toState.state = fromState.state;
        toState.isGrounded = fromState.isGrounded;
        toState.isActive = fromState.isActive;
        toState.isPivot = fromState.isPivot;

        return toState;
    }
    /**
     * void ResetSubState(SubState subState)
     * Reset the given Substate to default values.
     * 
     * substate -  substate to reset.
    **/
    public void ResetSubState(SubState subState)
    {
        subState.state = TileState.EMPTY;
        subState.isGrounded = false;
        subState.isActive = false;
        subState.isPivot = false;
    }


    /**
     * void BuildTree(StateNode inState)
     * Build the state Tree with the root of inState. All below nodes are placement options.
     * 
     *  inState -   The state to populate.
    **/
    public void BuildTree(StateNode inState)
    {
        StateNode curState;
        LinkedList<StateNode> TopLevelStates = new LinkedList<StateNode>();
        TopLevelStates.AddFirst(inState);
        inState.belowState = FallToBottom(inState);

        // Fill left as far as possible.
        curState = inState;
        while (curState != null)
        {
            curState.belowState = FallToBottom(curState);  // Create Falling Node
            curState.leftState = MoveLeft(curState);      // Create Left node
            curState = curState.leftState;                // Shift left for iteration
        }

        // Fill right as far as possible.
        curState = inState;
        while (curState != null)
        {
            curState.belowState = FallToBottom(curState);    // Create Falling Node
            curState.rightState = MoveRight(curState);    // Create Right node
            curState = curState.rightState;               // Shift right for iteration
        }
    }

    /**
     * void FillTreeValues(StateNode inState)
     * Evaluate all values of the given tree root.  Value is decided as the max value of leftState,
     *      rightState, or belowState.  This leads the tree to the best possible final state.
     *      
     *  inState - A root node whose value will be recursively decided by it's children if applicable.
     *                  All values are stored in the nodes themselves.
    **/
    public void FillTreeValues(StateNode inState)
    {
        int maxValue = 0;

        if (inState.active.Count == 0)
        {
            inState.value = QuantifyBoardState(inState);
            return;
        }

        if (inState.leftState != null)
        {
            FillTreeValues(inState.leftState);
            maxValue = Mathf.Max(maxValue, inState.leftState.value);
        }

        if (inState.rightState != null)
        {
            FillTreeValues(inState.rightState);
            maxValue = Mathf.Max(maxValue, inState.rightState.value);
        }

        if (inState.belowState != null)
        {
            FillTreeValues(inState.belowState);
            maxValue = Mathf.Max(maxValue, inState.belowState.value);
        }

        inState.value = maxValue;

        return;
    }

    /**
     * void BuildRootsFromRoot()
     *  Builds an entire tree from root, as well as the three rotations into
     *          root90, root180, and root270.  Then, fills values for all new
     *          trees.
    **/
    public void BuildRootsFromRoot()
    {
        // if root is null, flush all values
        if (root == null)
        {
            root90 = null;
            root180 = null;
            root270 = null;
            return;
        }

        // Build the four roots from root
        BuildTree(root);
        FillTreeValues(root);

        root90 = Rotate(root);

        if (root90 != null)
        {
            BuildTree(root90);
            FillTreeValues(root90);
            root180 = Rotate(root90);
        }
        else
        {
            root180 = null;
        }

        if (root180 != null)
        {
            BuildTree(root180);
            FillTreeValues(root180);
            root270 = Rotate(root180);
        }
        else
        {
            root270 = null;
        }

        if (root270 != null)
        {
            BuildTree(root270);
            FillTreeValues(root270);
        }
    }

    /**
     * MoveChoice ChooseMove()
     *  Return a move enum based on the values of root's leftState, rightState, 
     *          belowState, and root90 subNodes.
    **/
    public MoveChoice ChooseMove()
    {
        int moveValue = 0;
        MoveChoice curMove = MoveChoice.FALL;

        if (root == null)
            return MoveChoice.WAIT;

        if (root.belowState != null)
            moveValue = Mathf.Max(moveValue, root.belowState.value);

        if (root.leftState != null)
        {
            if (root.leftState.value > moveValue)
            {
                curMove = MoveChoice.LEFT;
                moveValue = root.leftState.value;
            }
        }

        if (root.rightState != null)
        {
            if (root.rightState.value > moveValue)
            {
                curMove = MoveChoice.RIGHT;
                moveValue = root.rightState.value;
            }
        }

        int rotateValue = 0;
        if (root90 != null)
            rotateValue = Mathf.Max(rotateValue, root90.value);
        if (root180 != null)
            rotateValue = Mathf.Max(rotateValue, root180.value);
        if (root270 != null)
            rotateValue = Mathf.Max(rotateValue, root270.value);

        if (rotateValue > moveValue)
        {
            curMove = MoveChoice.ROTATE;
        }

        return curMove;
    }

    public List<int[]> FindActiveTiles(SubState[,] board)
    {
        List<int[]> tileList = new List<int[]>();

        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                if (board[x, y].isActive)
                    tileList.Add(new int[2] { x, y });

                if (tileList.Count == 3)
                    return tileList;
            }
        }

        return tileList;
    }


    /**
     * StateNode Rotate(StateNode inState)
     *  For the Substate where isPivot is true, rotate adjacent activeTiles to 
     *          an empty space according to the game’s rotation rules.
     *          
     * inState  - The tile to perform rotation on.
     * returns  - A new state derived from instate if rotation is possible, null 
     *                  if it is not.
     */
    public StateNode Rotate(StateNode inState)
    {
        // Nothing to do
        if (inState.active.Count == 0)
        {
            return null;
        }

        // Copy the current state
        SubState[,] newBoard = new SubState[boardWidth, boardHeight];
        List<int[]> newActiveList = new List<int[]>();

        int[] pivot = new int[] { -1, -1 };
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                newBoard[x, y] = CopySubState(inState.board[x, y]);

                if (newBoard[x, y].isPivot)
                {
                    pivot = new int[] { x, y };
                }
            }
        }
        // If there's nothing to rotate..
        if (pivot[0] == -1)
            return null;
        // else, descripe tile shape
        int shapeDescriptor = 0;

        // Descripe object shape in binary 
        foreach (int[] tile in inState.active)
        {
            // Above,
            if ((tile[0] == pivot[0]) && tile[1] == pivot[1] + 1)
            {
                shapeDescriptor = shapeDescriptor + 1;
            }
            // To the right,
            else if ((tile[0] == pivot[0] + 1) && tile[1] == pivot[1])
            {
                shapeDescriptor = shapeDescriptor + 2;
            }
            // Or below
            else if ((tile[0] == pivot[0]) && tile[1] == pivot[1] - 1)
            {
                shapeDescriptor = shapeDescriptor + 4;
            }
            // To the left,
            else if ((tile[0] == pivot[0] - 1) && tile[1] == pivot[1])
            {
                shapeDescriptor = shapeDescriptor + 8;
            }
        }

        // x, y coordinates for the for adjacent tiles
        int[] TileAbove, TileRight, TileBelow, TileLeft;
        TileAbove = new int[] { pivot[0], pivot[1] + 1 };
        TileRight = new int[] { pivot[0] + 1, pivot[1] };
        TileBelow = new int[] { pivot[0], pivot[1] - 1 };
        TileLeft = new int[] { pivot[0] - 1, pivot[1] };

        // Use descriptor to perform rotation
        switch (shapeDescriptor)
        {
            // Up Only
            case (1):
                // Out of Bounds?
                if (TileRight[0] >= boardWidth)
                {
                    return null;
                }

                // Spot Available?
                if (newBoard[TileRight[0], TileRight[1]].state != TileState.EMPTY)
                {
                    return null;
                }

                // Perform Rotation
                newBoard[TileRight[0], TileRight[1]] = CopySubState(newBoard[TileAbove[0], TileAbove[1]]);
                newBoard[TileAbove[0], TileAbove[1]].Reset();

                newActiveList.Add(pivot);
                newActiveList.Add(TileRight);

                break;
            // Right Only
            case (2):
                // Out of Bounds?
                if (TileBelow[1] < boardBottom)
                    return null;

                // Spot Available?
                if (newBoard[TileBelow[0], TileBelow[1]].state != TileState.EMPTY)
                    return null;

                // Perform Rotation
                newBoard[TileBelow[0], TileBelow[1]] = CopySubState(newBoard[TileRight[0], TileRight[1]]);
                newBoard[pivot[0] + 1, pivot[1]].Reset();

                newActiveList.Add(pivot);
                newActiveList.Add(TileBelow);

                break;
            // Below Only
            case (4):
                if (TileLeft[0] < boardLeft)
                    return null;

                // Spot Available?
                if (newBoard[TileLeft[0], TileLeft[1]].state != TileState.EMPTY)
                    return null;

                // Perform Rotation
                newBoard[TileLeft[0], TileLeft[1]] = CopySubState(newBoard[TileBelow[0], TileBelow[1]]);
                newBoard[TileBelow[0], TileBelow[1]].Reset();

                newActiveList.Add(pivot);
                newActiveList.Add(TileLeft);

                break;
            // Left Only
            case (8):
                if (TileAbove[1] >= boardHeight)
                    return null;

                // Spot Available?
                if (newBoard[TileAbove[0], TileAbove[1]].state != TileState.EMPTY)
                    return null;

                // Perform Rotation
                newBoard[TileAbove[0], TileAbove[1]] = CopySubState(newBoard[TileLeft[0], TileLeft[1]]);
                newBoard[TileLeft[0], TileLeft[1]].Reset();

                newActiveList.Add(pivot);
                newActiveList.Add(TileAbove);

                break;
            // Up & Right (1 + 2)
            case (3):
                if (TileAbove[1] >= boardHeight || TileRight[0] >= boardWidth)
                    return null;

                // Spot Available?
                if (newBoard[TileRight[0], TileAbove[1]].state != TileState.EMPTY)
                    return null;

                // Perform Rotation
                newBoard[TileRight[0], TileAbove[1]] = CopySubState(newBoard[TileAbove[0], TileAbove[1]]);
                newBoard[TileAbove[0], TileAbove[1]] = CopySubState(newBoard[pivot[0], pivot[1]]);
                newBoard[pivot[0], pivot[1]] = CopySubState(newBoard[TileRight[0], TileRight[1]]);
                newBoard[TileRight[0], TileRight[1]].Reset();

                newActiveList.Add(pivot);
                newActiveList.Add(TileAbove);
                newActiveList.Add(new int[] { TileRight[0], TileAbove[1] });

                break;
            // Bottom & Right (2 + 4)
            case (6):
                if (TileRight[0] >= boardWidth || TileBelow[1] < boardBottom)
                    return null;

                // Spot Available?
                if (newBoard[TileRight[0], TileBelow[1]].state != TileState.EMPTY)
                    return null;

                // Perform Rotation
                newBoard[TileRight[0], TileBelow[1]] = CopySubState(newBoard[TileRight[0], TileRight[1]]);
                newBoard[TileRight[0], TileRight[1]] = CopySubState(newBoard[pivot[0], pivot[1]]);
                newBoard[pivot[0], pivot[1]] = CopySubState(newBoard[TileBelow[0], TileBelow[1]]);
                newBoard[TileBelow[0], TileBelow[1]].Reset();

                newActiveList.Add(pivot);
                newActiveList.Add(TileRight);
                newActiveList.Add(new int[] { TileRight[0], TileBelow[1] });

                break;
            // Bottom & Left (4 + 8)
            case (12):
                if (TileBelow[1] < boardBottom || TileLeft[0] < boardLeft)
                    return null;

                // Spot Available?
                if (newBoard[TileLeft[0], TileBelow[1]].state != TileState.EMPTY)
                    return null;

                // Perform Rotation
                newBoard[TileLeft[0], TileBelow[1]] = CopySubState(newBoard[TileBelow[0], TileBelow[1]]);
                newBoard[TileBelow[0], TileBelow[1]] = CopySubState(newBoard[pivot[0], pivot[1]]);
                newBoard[pivot[0], pivot[1]] = CopySubState(newBoard[TileLeft[0], TileLeft[1]]);
                newBoard[TileLeft[0], TileLeft[1]].Reset();

                newActiveList.Add(pivot);
                newActiveList.Add(TileBelow);
                newActiveList.Add(new int[] { TileLeft[0], TileBelow[1] });

                break;
            // Up & Left (8 + 1)
            case (9):
                if (TileAbove[1] >= boardHeight || TileLeft[0] < boardLeft)
                    return null;

                // Spot Available?
                if (newBoard[TileLeft[0], TileAbove[1]].state != TileState.EMPTY)
                    return null;

                // Perform Rotation
                newBoard[TileLeft[0], TileAbove[1]] = CopySubState(newBoard[TileLeft[0], TileLeft[1]]);
                newBoard[TileLeft[0], TileLeft[1]] = CopySubState(newBoard[pivot[0], pivot[1]]);
                newBoard[pivot[0], pivot[1]] = CopySubState(newBoard[TileAbove[0], TileAbove[1]]);
                newBoard[TileAbove[0], TileAbove[1]].Reset();

                newActiveList.Add(pivot);
                newActiveList.Add(TileLeft);
                newActiveList.Add(new int[] { TileLeft[0], TileAbove[1] });

                break;
            // Left and Right (8 + 2)
            case (10):
                if (TileAbove[1] >= boardHeight || TileAbove[1] < boardBottom)
                    return null;

                if (newBoard[TileAbove[0], TileAbove[1]].state != TileState.EMPTY)
                    return null;

                if (newBoard[TileBelow[0], TileBelow[1]].state != TileState.EMPTY)
                    return null;

                newBoard[TileAbove[0], TileAbove[1]] = CopySubState(newBoard[TileLeft[0], TileLeft[1]]);
                newBoard[TileLeft[0], TileLeft[1]].Reset();

                newBoard[TileBelow[0], TileBelow[1]] = CopySubState(newBoard[TileRight[0], TileRight[1]]);
                newBoard[TileRight[0], TileRight[1]].Reset();

                newActiveList.Add(pivot);
                newActiveList.Add(TileAbove);
                newActiveList.Add(TileBelow);

                break;
            // Above and Below (1 + 4)
            case (5):
                if (TileRight[0] >= boardWidth || TileLeft[0] < boardLeft)
                    return null;

                if (newBoard[TileRight[0], TileRight[1]].state != TileState.EMPTY)
                    return null;

                if (newBoard[TileLeft[0], TileLeft[1]].state != TileState.EMPTY)
                    return null;

                newBoard[TileLeft[0], TileLeft[1]] = CopySubState(newBoard[TileBelow[0], TileBelow[1]]);
                newBoard[TileBelow[0], TileBelow[1]].Reset();

                newBoard[TileRight[0], TileRight[1]] = CopySubState(newBoard[TileAbove[0], TileAbove[1]]);
                newBoard[TileAbove[0], TileAbove[1]].Reset();

                newActiveList.Add(pivot);
                newActiveList.Add(TileLeft);
                newActiveList.Add(TileRight);

                break;
        }

        StateNode newNode = new StateNode(newBoard);
        newNode.active = newActiveList;

        return newNode;
    }


    /**
     * StateNode MoveLeft(StateNode inState)
    **/
    public StateNode MoveLeft(StateNode inState)
    {
        if (inState.active == null)
            inState.active = FindActiveTiles(inState.board);

        // Nothing to do
        if (inState.active.Count == 0)
        {
            return null;
        }

        // Check if the move is valid
        foreach (int[] coors in inState.active)
        {
            // Look for reasons we can’t move left
            // 1, We’re at the edge of the board
            if (coors[0] == 0)
            {
                return null;
            }

            // 2, if we’re rubbing against a tile that isn’t also active
            else if (!(inState.board[coors[0] - 1, coors[1]].state == TileState.EMPTY) &&
                    !(inState.board[coors[0] - 1, coors[1]].isActive))
            {
                return null;
            }
        }

        // if MoveLeft is valid, copy the board with the move performed
        SubState[,] newBoard = new SubState[boardWidth, boardHeight];
        List<int[]> newActiveList = new List<int[]>();
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                newBoard[x, y] = CopySubState(inState.board[x, y]);

                if (inState.board[x, y].isActive)
                {
                    newBoard[x - 1, y] = CopySubState(inState.board[x, y]);

                    newBoard[x, y].Reset();
                    ResetSubState(newBoard[x, y]);
                    newActiveList.Add(new int[] { x - 1, y });
                }
            }
        }

        StateNode newNode = new StateNode(newBoard);
        newNode.active = newActiveList;
        newNode.isLeft = true;

        return newNode;
    }

    /**
     *  For each SubState where isActive is true, move the tile one step to the right if the adjacent space is empty.
     */
    public StateNode MoveRight(StateNode inState)
    {
        if (inState.active == null)
            inState.active = FindActiveTiles(inState.board);

        // Nothing to do
        if (inState.active.Count == 0)
        {
            return null;
        }

        // Check if the move is valid
        foreach (int[] coors in inState.active)
        {
            // Look for reasons we can’t move left
            // 1, We’re at the edge of the board
            if (coors[0] == boardWidth - 1)
            {
                return null;
            }

            // 2, if we’re rubbing against a tile that isn’t also active
            else if (!(inState.board[coors[0] + 1, coors[1]].state == TileState.EMPTY) &&
                    !(inState.board[coors[0] + 1, coors[1]].isActive))
            {
                return null;
            }
        }

        // if MoveLeft is valid, copy the board with the move performed
        SubState[,] newBoard = new SubState[boardWidth, boardHeight];
        List<int[]> newActiveList = new List<int[]>();
        for (int x = boardWidth - 1; x >= 0; x--)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                newBoard[x, y] = CopySubState(inState.board[x, y]);

                if (inState.board[x, y].isActive)
                {
                    newBoard[x + 1, y] = CopySubState(inState.board[x, y]);
                    newBoard[x, y].Reset();
                    newActiveList.Add(new int[] { x + 1, y });
                }
            }
        }

        StateNode newNode = new StateNode(newBoard);
        newNode.active = newActiveList;
        newNode.isLeft = false;

        return newNode;
    }

    /**
     *  For each SubState where isGrounded is false, move the tile one step down and ground if it reaches the bottom, or a grounded tile.
     */
    public StateNode FallOnce(StateNode inState)
    {
        // if Right is valid, copy the board with the move performed
        SubState[,] newBoard = new SubState[boardWidth, boardHeight];
        List<int[]> newActiveList = new List<int[]>();

        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                newBoard[x, y] = CopySubState(inState.board[x, y]);
            }
        }

        // Push tiles that are not grounded
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                if (newBoard[x, y].state == TileState.EMPTY)
                {
                    // Do Nothing
                }
                else if (y == 0 && newBoard[x, y].state != TileState.EMPTY)
                {
                    newBoard[x, y].isGrounded = true;
                }
                else if (newBoard[x, y - 1].isGrounded)
                {
                    newBoard[x, y].isGrounded = true;
                }
                else if (newBoard[x, y - 1].state == TileState.EMPTY)
                {
                    newBoard[x, y - 1].state = inState.board[x, y].state;
                    newBoard[x, y - 1].isActive = inState.board[x, y].isActive;
                    newBoard[x, y - 1].isPivot = inState.board[x, y].isPivot;
                    newBoard[x, y - 1].isGrounded = false;

                    newBoard[x, y].state = TileState.EMPTY;
                    newBoard[x, y].isActive = false;
                    newBoard[x, y].isPivot = false;
                    newBoard[x, y].isGrounded = false;

                    if (inState.board[x, y].isActive)
                    {
                        newActiveList.Add(new int[2] { x, y });
                    }
                }
            }
        }

        StateNode output = new StateNode(newBoard);
        output.isLeft = inState.isLeft;

        return output;
    }

    public StateNode FallToBottom(StateNode inState)
    {
        SubState[,] newBoard = new SubState[boardWidth, boardHeight];
        List<int[]> newActiveList = new List<int[]>();

        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                newBoard[x, y] = CopySubState(inState.board[x, y]);
            }
        }

        int emptyLoc;
        for (int x = 0; x < boardWidth; x++)
        {
            emptyLoc = -1;
            for (int y = 0; y < boardHeight; y++)
            {
                if (newBoard[x, y].state == TileState.EMPTY && emptyLoc == -1)
                {
                    emptyLoc = y;
                }
                else if (newBoard[x, y].state != TileState.EMPTY && !newBoard[x, y].isGrounded)
                {
                    if (y == 0 || emptyLoc == -1)
                        return FallOnce(inState);

                    newBoard[x, emptyLoc] = CopySubState(inState.board[x, y]);
                    newBoard[x, y].Reset();
                    newBoard[x, emptyLoc].isActive = false;
                    newBoard[x, emptyLoc].isGrounded = true;
                    emptyLoc++;
                }
            }
        }

        StateNode outNode = new StateNode(newBoard);
        outNode.active.Clear();

        return outNode;
    }

    /**
     *  Find the value of all blue clusters that are touching the board floor.
     */
    public int EvaluateAllColors(StateNode state)
    {
        int i = 0;
        int value = 0;
        int colorCount, adjEmptyCount;

        LinkedList<int[]> RedList = new LinkedList<int[]>();
        LinkedList<int[]> GreenList = new LinkedList<int[]>();
        LinkedList<int[]> BlueList = new LinkedList<int[]>();

        // visited array, to record if each tile has been visited
        bool[,] visited = new bool[boardWidth, boardHeight];
        for (i = 0; i < boardWidth * boardHeight; i++)
        {
            visited[i % boardWidth, i / boardWidth] = false;
        }

        // for each tile on the bottom row, add it to a list for 
        //         iteration and mark as visited.
        for (int x = 0; x < boardWidth; x++)
        {
            switch (state.board[x, 0].state)
            {
                case TileState.RED:
                    RedList.AddLast(new int[] { x, 0 });
                    visited[x, 0] = true;
                    break;
                case TileState.GREEN:
                    GreenList.AddLast(new int[] { x, 0 });
                    visited[x, 0] = true;
                    break;
                case TileState.BLUE:
                    BlueList.AddLast(new int[] { x, 0 });
                    visited[x, 0] = true;
                    break;
            }
        }


        int[] curPoint;
        TileState curState = TileState.RED;
        LinkedList<int[]> curList = null;

        // For each of the three colors...
        while (i < 3)
        {
            value = 0;
            colorCount = 0;
            adjEmptyCount = 0;

            // Which color are we on?
            switch (i)
            {
                case (0):
                    curState = TileState.RED;
                    curList = RedList;
                    break;
                case (1):
                    curState = TileState.GREEN;
                    curList = GreenList;
                    break;
                case (2):
                    curState = TileState.BLUE;
                    curList = BlueList;
                    break;
            }

            while (curList.Count != 0)
            {
                curPoint = curList.First.Value;

                // boolean match flag where each bit is an adjacent side
                int matches = 0;

                // Add applicable & unvisited points to the list

                // If point is to the left...
                if (curPoint[0] > 0)
                {
                    if (state.board[curPoint[0] - 1, curPoint[1]].state == curState)
                    {
                        // Add b0001 
                        matches += 1;
                        colorCount++;

                        // Add
                        if (!visited[curPoint[0] - 1, curPoint[1]])
                        {
                            curList.AddLast(new int[] { curPoint[0] - 1, curPoint[1] });
                            visited[curPoint[0] - 1, curPoint[1]] = true;
                        }
                    }
                    else if (state.board[curPoint[0] - 1, curPoint[1]].state == TileState.EMPTY)
                    {
                        adjEmptyCount++;

                    }
                }

                if (curPoint[0] < boardWidth - 1)
                {
                    if (state.board[curPoint[0] + 1, curPoint[1]].state == curState)
                    {
                        // Add b0010 
                        matches += 2;
                        colorCount++;

                        if (!visited[curPoint[0] + 1, curPoint[1]])
                        {
                            curList.AddLast(new int[] { curPoint[0] + 1, curPoint[1] });
                            visited[curPoint[0] + 1, curPoint[1]] = true;
                        }
                    }
                    else if (state.board[curPoint[0] - 1, curPoint[1]].state == TileState.EMPTY)
                    {
                        adjEmptyCount++;

                    }
                }

                if (curPoint[1] > 0)
                {
                    if (state.board[curPoint[0], curPoint[1] - 1].state == curState)
                    {
                        // Add b0100 
                        matches += 4;
                        colorCount++;

                        if (!visited[curPoint[0], curPoint[1] - 1])
                        {
                            curList.AddLast(new int[] { curPoint[0], curPoint[1] - 1 });
                            visited[curPoint[0], curPoint[1] - 1] = true;
                        }
                    }
                    else if (state.board[curPoint[0] - 1, curPoint[1]].state == TileState.EMPTY)
                    {
                        adjEmptyCount++;

                    }
                }

                if (curPoint[1] < boardHeight - 1)
                {
                    if (state.board[curPoint[0], curPoint[1] + 1].state == curState)
                    {
                        // Add b1000
                        matches += 8;
                        colorCount++;

                        if (!visited[curPoint[0], curPoint[1] + 1])
                        {
                            curList.AddLast(new int[] { curPoint[0], curPoint[1] + 1 });
                            visited[curPoint[0], curPoint[1] + 1] = true;
                        }
                    }
                    else if (state.board[curPoint[0] - 1, curPoint[1]].state == TileState.EMPTY)
                    {
                        adjEmptyCount++;

                    }
                }

                value += myGameBoard.MatchFlagValue(matches);

                curList.RemoveFirst();
            }

            value += colorCount * adjEmptyCount;
            state.value += value;

            switch (curState)
            {
                case TileState.RED:
                    state.redValue = value;
                    break;
                case TileState.BLUE:
                    state.blueValue = value;
                    break;
                case TileState.GREEN:
                    state.greenValue = value;
                    break;
            }

            i++;
        }

        return state.value;
    }


    public int QuantifyBoardState(StateNode gameState)
    {
        int score = 0;

        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                if (gameState.board[x, y].state == TileState.EMPTY)
                    score += emptyValueTable[x, y];
            }
        }

        score += EvaluateAllColors(gameState);

        return score;
    }

    public StateNode BuildGameState()
    {
        SubState[,] newBoard = new SubState[boardWidth, boardHeight];
        List<int[]> activeList = new List<int[]>();

        foreach (TileScript tile in myGameBoard.AllTiles)
        {
            newBoard[tile.x_coor, tile.y_coor] = new SubState();

            newBoard[tile.x_coor, tile.y_coor].state = tile.curState;
            newBoard[tile.x_coor, tile.y_coor].isActive = tile.isActive;
            newBoard[tile.x_coor, tile.y_coor].isGrounded = tile.isGrounded;
            newBoard[tile.x_coor, tile.y_coor].isPivot = tile.isPivot;

            if (tile.isActive)
                activeList.Add(new int[] { tile.x_coor, tile.y_coor });
        }

        StateNode newNode = new StateNode(newBoard);
        newNode.active = activeList;
        return newNode;
    }

    #region Test Code

    public void PrintBoardStates(StateNode board)
    {
        string s = "";


        for (int y = boardHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                s = s + board.board[x, y].state.ToString();
                s = s + "-";
            }
            s = s + '\n';
        }

        print(s);
    }

    public void TestAll()
    {
        /**
         * Set up temporary test environment
         */
        int globalWidth = boardWidth;
        int globalHeight = boardHeight;
        boardWidth = 8;
        boardHeight = 8;

        string testResults = "";



        /********************************************************
         **** Left/Right Movement Tests *************************
         ********************************************************/

        /***********
         * ________
         * ________
         * ________
         * ________
         * ________
         * ________
         * ________
         * ________                           
         ************/
        SubState[,] testBoard = new SubState[8, 8];
        StateNode testState = new StateNode(testBoard);

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                testBoard[x, y] = new SubState();
                testBoard[x, y].state = TileState.EMPTY;
            }
        }



        /***********
         * Test: Set active tiles, then retrieve them in a list
         *
         * Expected Result:
         * 7 __A_____
         * 6 __A_____
         * 5 __A_____
         * 4 __A_____
         * 3 __A_____
         * 2 __AAAAAA
         * 1 __A_____
         * 0 __A_____                           
         *   01234567
         ************/
        testResults += "FindActiveTiles()...";
        for (int i = 0; i < 8; i++)
        {
            testState.board[2, i].isActive = true;
            testState.board[2, i].state = TileState.RED;
            testState.active.Add(new int[] { 2, i });

            if (i > 2)
            {
                testState.active.Add(new int[] { i, 2 });
                testState.board[i, 2].isActive = true;
                testState.board[i, 2].state = TileState.RED;
            }
        }

        List<int[]> activeTest = FindActiveTiles(testBoard);
        bool test = true;
        foreach (int[] coor in activeTest)
        {
            test = test && ((coor[0] == 2) || (coor[1] == 2));
        }
        if (test)
            testResults += " Pass\n";
        else
            testResults += " Fail\n";

        print(testResults);



        /***********
         * Setup against the left wall...
         * 7 __R_____
         * 6 __R_____
         * 6 __R_____
         * 6 __R_____
         * 6 __R_____
         * 2 __RRRRRR
         * 6 __R_____
         * 6 __R_____                        
         *   01234567
         ************/
        testResults = "Setup Left...";
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (x == 2)
                {
                    testBoard[x, y].state = TileState.RED;
                    testBoard[x, y].isActive = true;
                }
                else if (x > 2 && y == 2)
                {
                    testBoard[x, y].state = TileState.RED;
                    testBoard[x, y].isActive = true;
                }
                else
                {
                    testBoard[x, y].state = TileState.EMPTY;
                    testBoard[x, y].isActive = false;
                }
            }
        }
        testResults += " Complete.\n";

        print(testResults);


        /*************
         * Test: Move all reds one tile to the left
         *
         * Expected Result:
         * 7 _R______
         * 6 _R______
         * 5 _R______
         * 4 _R______
         * 3 _R______
         * 2 _RRRRRR_
         * 1 _R______
         * 0 _R______                           
         *   01234567
         ************/
        testResults = "MoveLeft...";
        test = true;
        testState = MoveLeft(testState);
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (x == 1 || (y == 2 && x != 0 && x != 7))
                {
                    test = test && (testState.board[x, y].state == TileState.RED);
                    test = test && (testState.board[x, y].isActive);
                }
                else
                {
                    test = test && (testState.board[x, y].state == TileState.EMPTY);
                    test = test && !(testState.board[x, y].isActive);
                }
            }
        }
        if (test)
            testResults += " Pass\n";
        else
            testResults += " Fail\n";

        print(testResults);



        /*************
         * Test: Move all reds one tile to the left again...
         *
         * Expected Result:
         * 7 R_______
         * 6 R_______
         * 5 R_______
         * 4 R_______
         * 3 R_______
         * 2 RRRRRR__
         * 1 R_______
         * 0 R_______                           
         *   01234567
         ************/
        testResults = "MoveLeft to 0...";
        test = true;
        testState = MoveLeft(testState);
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (x == 0 || (y == 2 && x < 6))
                {
                    test = test && (testState.board[x, y].state == TileState.RED);
                    test = test && (testState.board[x, y].isActive);
                }
                else
                {
                    test = test && (testState.board[x, y].state == TileState.EMPTY);
                    test = test && !(testState.board[x, y].isActive);
                }
            }
        }
        if (test)
            testResults += " Pass\n";
        else
            testResults += " Fail\n";

        print(testResults);



        /*************
         * Test: Move all reds one tile to the left again, Past the array bounds
         *
         * Expected Result:
         * Null
         ***********/
        testResults = "MoveLeft to -1...";
        test = true;
        testState = MoveLeft(testState);

        test = test && (testState == null);
        if (test)
            testResults += (" Pass\n");
        else
            testResults += (" Fail\n");

        print(testResults);



        /***********
         * Setup against the Right wall...
         *
         * 7 _____R__
         * 6 _____R__
         * 5 _____R__
         * 4 _____R__
         * 3 _____R__
         * 2 RRRRRR__
         * 1 _____R__
         * 0 _____R__                           
         *   01234567
         ************/
        testResults = ("Setup Right...");

        testBoard = new SubState[8, 8];
        testState = new StateNode(testBoard);

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                testBoard[x, y] = new SubState();
                testBoard[x, y].state = TileState.EMPTY;
            }
        }

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (x == 5 || (y == 2 && x < 5))
                {
                    testState.board[x, y].state = TileState.RED;
                    testState.board[x, y].isActive = true;
                    testState.active.Add(new int[] { x, y });
                }
                else
                {
                    testState.board[x, y].state = TileState.EMPTY;
                    testState.board[x, y].isActive = false;
                }
            }
        }
        testResults += "Complete\n";
        print(testResults);


        /*************
         * Test: Move all reds one tile to the left
         *
         * Expected Result:
         * 7 ______R_
         * 6 ______R_
         * 5 ______R_
         * 4 ______R_
         * 3 ______R_
         * 2 _RRRRRR_
         * 1 ______R_
         * 0 ______R_                           
         *   01234567
         ************/
        testResults = ("MoveRight..");
        test = true;
        testState = MoveRight(testState);
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (x == 6 || (y == 2 && x != 0 && x != 7))
                {
                    test = test && (testState.board[x, y].state == TileState.RED);
                    test = test && (testState.board[x, y].isActive);
                }
                else
                {
                    test = test && (testState.board[x, y].state == TileState.EMPTY);
                    test = test && !(testState.board[x, y].isActive);
                }
            }
        }
        if (test)
            testResults += (" Pass\n");
        else
            testResults += (" Fail\n");

        print(testResults);

        /*************
         * Test: Move all reds one tile to the left again...
         *
         * Expected Result:
         * 7 _______R
         * 6 _______R
         * 5 _______R
         * 4 _______R
         * 3 _______R
         * 2 __RRRRRR
         * 1 _______R
         * 0 _______R                           
         *   01234567
         ************/
        testResults = ("MoveRight to 7...");
        test = true;
        testState = MoveRight(testState);
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (x == 7 || (y == 2 && x > 1))
                {
                    test = test && (testState.board[x, y].state == TileState.RED);
                    test = test && (testState.board[x, y].isActive);
                }
                else
                {
                    test = test && (testState.board[x, y].state == TileState.EMPTY);
                    test = test && !(testState.board[x, y].isActive);
                }
            }
        }
        if (test)
            testResults += (" Pass\n");
        else
            testResults += (" Fail\n");

        print(testResults);



        /*************
         * Test: Move all reds one tile to the left again, Past the array bounds
         *
         * Expected Result:
         * 7 _______R
         * 6 _______R
         * 5 _______R
         * 4 _______R
         * 3 _______R
         * 2 EERRRRRR
         * 1 _______R
         * 0 _______R                           
         *   01234567
         ************/
        testResults = ("MoveRight to 8, out of bounds...");
        test = true;
        testState = MoveRight(testState);
        if (testState == null)
            testResults += (" Pass\n");
        else
            testResults += (" Fail\n");

        print(testResults);


        /********************************************************
         **** Vertical Movement Tests ***************************
         ********************************************************/

        /*************
         * Test FallOnce()
         */
        testBoard = new SubState[8, 8];
        testState = new StateNode(testBoard);
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                testBoard[x, y] = new SubState();
                testBoard[x, y].state = TileState.EMPTY;
            }
        }


        /*************
         * Init: Move all reds one tile to the left again, Past the array bounds
         *
         * Expected Result:
         * 7 RRRR____
         * 6 RRRR____
         * 5 RRRR____
         * 4 RRRR____
         * 3 ____RRRR
         * 2 ____RRRR
         * 1 ____RRRR
         * 0 ____RRRR                           
         *   01234567
         ************/
        testResults = ("Initialize alternating tiles...");
        bool x_mod;
        bool y_mod;
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                x_mod = (x % 8) > 3;
                y_mod = (y % 8) > 3;

                if (x_mod && y_mod)
                {
                    testBoard[x, y].state = TileState.EMPTY;
                    testBoard[x, y].isGrounded = false;
                }
                else if (x_mod && !y_mod)
                {
                    testBoard[x, y].state = TileState.RED;
                    testBoard[x, y].isGrounded = false;
                }
                else if (!x_mod && y_mod)
                {
                    testBoard[x, y].state = TileState.RED;
                    testBoard[x, y].isGrounded = false;
                }
                else if (!x_mod && !y_mod)
                {
                    testBoard[x, y].state = TileState.EMPTY;
                    testBoard[x, y].isGrounded = false;
                }
            }
        }
        testResults += "Complete.\n";
        print(testResults);



        /*************
         * Test: Drop all tiles one space, the right four should become grounded
         *
         * Expected Result:
         * 7 ________
         * 6 RRRR____
         * 5 RRRR____
         * 4 RRRR____
         * 3 RRRRRRRR
         * 2 ____RRRR
         * 1 ____RRRR
         * 0 ____RRRR                           
         *   01234567
         ************/
        testResults = ("Drop all tiles...");
        testState = FallOnce(testState);

        test = true;
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (x >= 0 && x < 4 && y > 2 && y < 7)
                {
                    test = test && (testState.board[x, y].state == TileState.RED);
                    test = test && !(testState.board[x, y].isGrounded);
                }
                else if (x > 3 && x < 8 && y >= 0 && y < 4)
                {
                    test = test && (testState.board[x, y].state == TileState.RED);
                    test = test && testState.board[x, y].isGrounded;
                }
                else
                {
                    test = test && (testState.board[x, y].state == TileState.EMPTY);
                    test = test && !testState.board[x, y].isGrounded;
                }
            }
        }
        if (test)
            testResults += (" Pass\n");
        else
            testResults += (" Fail\n");

        print(testResults);



        /*************
         * Test: Drop all tiles to the floor.
         *
         * Expected Result:
         * 7 ________
         * 6 ________
         * 5 ________
         * 4 ________
         * 3 RRRRRRRR
         * 2 RRRRRRRR
         * 1 RRRRRRRR
         * 0 RRRRRRRR                           
         *   01234567
         ************/
        testResults = ("Drop all tiles to Bottom...");
        testState = FallToBottom(testState);

        test = true;
        test = test && (testState.active.Count == 0);
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (y < 4)
                {
                    test = test && (testState.board[x, y].state == TileState.RED);
                    test = test && (testState.board[x, y].isGrounded);
                    test = test && !(testState.board[x, y].isActive);
                }
                else if (y >= 4)
                {
                    test = test && (testState.board[x, y].state == TileState.EMPTY);
                    test = test && !(testState.board[x, y].isGrounded);
                    test = test && !(testState.board[x, y].isActive);
                }
            }
        }
        if (test)
            testResults += (" Pass\n");
        else
            testResults += (" Fail\n");

        print(testResults);


        /********************************************************
         **** Rotation Method Tests *****************************
         ********************************************************/

        /*************
         * Init: Move all reds one tile to the left again, Past the array bounds
         *
         * Expected Result:
         * 7 ________
         * 6 ________
         * 5 ________
         * 4 ___B____
         * 3 ___G____
         * 2 ________
         * 1 ________
         * 0 ________                           
         *   01234567
         ************/
        testBoard = new SubState[boardWidth, boardHeight];

        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                testBoard[x, y] = new SubState();
            }
        }

        testBoard[3, 3].state = TileState.GREEN;
        testBoard[3, 3].isActive = true;
        testBoard[3, 3].isPivot = true;

        testBoard[3, 4].state = TileState.BLUE;
        testBoard[3, 4].isActive = true;

        testState = new StateNode(testBoard);

        testState.active.Add(new int[] { 3, 3 });
        testState.active.Add(new int[] { 3, 4 });


        /*************
         * Test: Move all reds one tile to the left again, Past the array bounds
         *
         * Expected Result:
         * 7 ________
         * 6 ________
         * 5 ________
         * 4 ________
         * 3 ___GB___
         * 2 ________
         * 1 ________
         * 0 ________                           
         *   01234567
         ************/
        testResults = "Rotate up...";

        testState = Rotate(testState);
        test = true;
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (x == 3 && y == 3)
                {
                    test = test && testState.board[x, y].state == TileState.GREEN;
                    test = test && testState.board[x, y].isActive;
                    test = test && testState.board[x, y].isPivot;
                }
                else if (x == 4 && y == 3)
                {
                    test = test && testState.board[x, y].state == TileState.BLUE;
                    test = test && testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
                else
                {
                    test = test && testState.board[x, y].state == TileState.EMPTY;
                    test = test && !testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
            }
        }
        if (test)
            testResults += "Pass.\n";
        else
            testResults += "Fail\n";

        print(testResults);



        /*************
         * Test: Move all reds one tile to the left again, Past the array bounds
         *
         * Expected Result:
         * 7 ________
         * 6 ________
         * 5 ________
         * 4 ________
         * 3 ___G____
         * 2 ___B____
         * 1 ________
         * 0 ________                           
         *   01234567
         ************/
        testResults = "Rotate Right...";

        testState = Rotate(testState);
        test = true;
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (x == 3 && y == 3)
                {
                    test = test && testState.board[x, y].state == TileState.GREEN;
                    test = test && testState.board[x, y].isActive;
                    test = test && testState.board[x, y].isPivot;
                }
                else if (x == 3 && y == 2)
                {
                    test = test && testState.board[x, y].state == TileState.BLUE;
                    test = test && testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
                else
                {
                    test = test && testState.board[x, y].state == TileState.EMPTY;
                    test = test && !testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
            }
        }
        if (test)
            testResults += "Pass.\n";
        else
        {
            testResults += "Fail\n";
        }

        print(testResults);



        /*************
         * Test: Move all reds one tile to the left again, Past the array bounds
         *
         * Expected Result:
         * 7 ________
         * 6 ________
         * 5 ________
         * 4 ________
         * 3 __BG____
         * 2 ________
         * 1 ________
         * 0 ________                           
         *   01234567
         ************/

        testState = Rotate(testState);
        test = true;
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (x == 3 && y == 3)
                {
                    test = test && testState.board[x, y].state == TileState.GREEN;
                    test = test && testState.board[x, y].isActive;
                    test = test && testState.board[x, y].isPivot;
                }
                else if (x == 2 && y == 3)
                {
                    test = test && testState.board[x, y].state == TileState.BLUE;
                    test = test && testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
                else
                {
                    test = test && testState.board[x, y].state == TileState.EMPTY;
                    test = test && !testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
            }
        }
        if (test)
            testResults += "Pass.\n";
        else
            testResults += "Fail\n";

        print(testResults);



        /*************
         * Test: Move all reds one tile to the left again, Past the array bounds
         *
         * Expected Result:
         * 7 ________
         * 6 ________
         * 5 ________
         * 4 ___B____
         * 3 ___G____
         * 2 ________
         * 1 ________
         * 0 ________                           
         *   01234567
         ************/
        testResults = "Rotate Left...";

        testState = Rotate(testState);
        test = true;
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (x == 3 && y == 3)
                {
                    test = test && testState.board[x, y].state == TileState.GREEN;
                    test = test && testState.board[x, y].isActive;
                    test = test && testState.board[x, y].isPivot;
                }
                else if (x == 3 && y == 4)
                {
                    test = test && testState.board[x, y].state == TileState.BLUE;
                    test = test && testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
                else
                {
                    test = test && testState.board[x, y].state == TileState.EMPTY;
                    test = test && !testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
            }
        }
        if (test)
            testResults += "Pass.\n";
        else
            testResults += "Fail\n";

        print(testResults);



        /*************
         * Init: Move all reds one tile to the left again, Past the array bounds
         *
         * Expected Result:
         * 7 ________
         * 6 ________
         * 5 ________
         * 4 ___B____
         * 3 ___GR___
         * 2 ________
         * 1 ________
         * 0 ________                           
         *   01234567
         ************/
        testBoard = new SubState[boardWidth, boardHeight];

        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                testBoard[x, y] = new SubState();
            }
        }

        testBoard[3, 3].state = TileState.GREEN;
        testBoard[3, 3].isActive = true;
        testBoard[3, 3].isPivot = true;

        testBoard[3, 4].state = TileState.BLUE;
        testBoard[3, 4].isActive = true;

        testBoard[4, 3].state = TileState.RED;
        testBoard[4, 3].isActive = true;

        testState = new StateNode(testBoard);

        testState.active.Add(new int[] { 4, 3 });
        testState.active.Add(new int[] { 3, 3 });
        testState.active.Add(new int[] { 3, 4 });



        /*************
         * Test: Move all reds one tile to the left again, Past the array bounds
         *
         * Expected Result:
         * 7 ________
         * 6 ________
         * 5 ________
         * 4 ___GB___
         * 3 ___R____
         * 2 ________
         * 1 ________
         * 0 ________                           
         *   01234567
         ************/
        testResults = "Rotate Top-right...";

        testState = Rotate(testState);

        test = true;
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (x == 4 && y == 4)
                {
                    test = test && testState.board[x, y].state == TileState.BLUE;
                    test = test && testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
                else if (x == 3 && y == 4)
                {
                    test = test && testState.board[x, y].state == TileState.GREEN;
                    test = test && testState.board[x, y].isActive;
                    test = test && testState.board[x, y].isPivot;
                }
                else if (x == 3 && y == 3)
                {
                    test = test && testState.board[x, y].state == TileState.RED;
                    test = test && testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
                else
                {
                    test = test && testState.board[x, y].state == TileState.EMPTY;
                    test = test && !testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
            }
        }
        if (test)
            testResults += "Pass.\n";
        else
        {
            testResults += "Fail\n";
        }

        print(testResults);



        /*************
         * Test: Move all reds one tile to the left again, Past the array bounds
         *
         * Expected Result:
         * 7 ________
         * 6 ________
         * 5 ________
         * 4 ___RG___
         * 3 ____B___
         * 2 ________
         * 1 ________
         * 0 ________                           
         *   01234567
         ************/
        testResults = "Rotate Right-Bottom ...";

        testState = Rotate(testState);
        test = true;
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (x == 4 && y == 3)
                {
                    test = test && testState.board[x, y].state == TileState.BLUE;
                    test = test && testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
                else if (x == 4 && y == 4)
                {
                    test = test && testState.board[x, y].state == TileState.GREEN;
                    test = test && testState.board[x, y].isActive;
                    test = test && testState.board[x, y].isPivot;
                }
                else if (x == 3 && y == 4)
                {
                    test = test && testState.board[x, y].state == TileState.RED;
                    test = test && testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
                else
                {
                    test = test && testState.board[x, y].state == TileState.EMPTY;
                    test = test && !testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
            }
        }
        if (test)
            testResults += "Pass.\n";
        else
            testResults += "Fail\n";

        print(testResults);



        /*************
         * Test: Move all reds one tile to the left again, Past the array bounds
         *
         * Expected Result:
         * 7 ________
         * 6 ________
         * 5 ________
         * 4 ____R___
         * 3 ___BG___
         * 2 ________
         * 1 ________
         * 0 ________                           
         *   01234567
         ************/
        testResults = "Rotate Left-Bottom ...";

        testState = Rotate(testState);
        test = true;
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (x == 3 && y == 3)
                {
                    test = test && testState.board[x, y].state == TileState.BLUE;
                    test = test && testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
                else if (x == 4 && y == 3)
                {
                    test = test && testState.board[x, y].state == TileState.GREEN;
                    test = test && testState.board[x, y].isActive;
                    test = test && testState.board[x, y].isPivot;
                }
                else if (x == 4 && y == 4)
                {
                    test = test && testState.board[x, y].state == TileState.RED;
                    test = test && testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
                else
                {
                    test = test && testState.board[x, y].state == TileState.EMPTY;
                    test = test && !testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
            }
        }
        if (test)
            testResults += "Pass.\n";
        else
            testResults += "Fail\n";

        print(testResults);



        /*************
         * Init: Move all reds one tile to the left again, Past the array bounds
         *
         * Expected Result:
         * 7 ________
         * 6 ________
         * 5 ________
         * 4 ________
         * 3 __RGB___
         * 2 ________
         * 1 ________
         * 0 ________                           
         *   01234567
         ************/
        testBoard = new SubState[boardWidth, boardHeight];

        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                testBoard[x, y] = new SubState();
            }
        }

        testBoard[3, 3].state = TileState.GREEN;
        testBoard[3, 3].isActive = true;
        testBoard[3, 3].isPivot = true;

        testBoard[4, 3].state = TileState.BLUE;
        testBoard[4, 3].isActive = true;

        testBoard[2, 3].state = TileState.RED;
        testBoard[2, 3].isActive = true;

        testState = new StateNode(testBoard);

        testState.active.Add(new int[] { 4, 3 });
        testState.active.Add(new int[] { 3, 3 });
        testState.active.Add(new int[] { 2, 3 });



        /*************
         * Test: Move all reds one tile to the left again, Past the array bounds
         *
         * Expected Result:
         * 7 ________
         * 6 ________
         * 5 ________
         * 4 ___R____
         * 3 ___G____
         * 2 ___B____
         * 1 ________
         * 0 ________                           
         *   01234567
         ************/
        testResults = "Rotate Left-Right ...";

        testState = Rotate(testState);
        test = true;
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (x == 3 && y == 2)
                {
                    test = test && testState.board[x, y].state == TileState.BLUE;
                    test = test && testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
                else if (x == 3 && y == 3)
                {
                    test = test && testState.board[x, y].state == TileState.GREEN;
                    test = test && testState.board[x, y].isActive;
                    test = test && testState.board[x, y].isPivot;
                }
                else if (x == 3 && y == 4)
                {
                    test = test && testState.board[x, y].state == TileState.RED;
                    test = test && testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
                else
                {
                    test = test && testState.board[x, y].state == TileState.EMPTY;
                    test = test && !testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
            }
        }
        if (test)
            testResults += "Pass.\n";
        else
            testResults += "Fail\n";

        print(testResults);



        /*************
         * Test: Move all reds one tile to the left again, Past the array bounds
         *
         * Expected Result:
         * 7 ________
         * 6 ________
         * 5 ________
         * 4 ________
         * 3 __BGR___
         * 2 ________
         * 1 ________
         * 0 ________                           
         *   01234567
         ************/
        testResults = "Rotate Top-Bottom ...";

        testState = Rotate(testState);
        test = true;
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (x == 2 && y == 3)
                {
                    test = test && testState.board[x, y].state == TileState.BLUE;
                    test = test && testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
                else if (x == 3 && y == 3)
                {
                    test = test && testState.board[x, y].state == TileState.GREEN;
                    test = test && testState.board[x, y].isActive;
                    test = test && testState.board[x, y].isPivot;
                }
                else if (x == 4 && y == 3)
                {
                    test = test && testState.board[x, y].state == TileState.RED;
                    test = test && testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
                else
                {
                    test = test && testState.board[x, y].state == TileState.EMPTY;
                    test = test && !testState.board[x, y].isActive;
                    test = test && !testState.board[x, y].isPivot;
                }
            }
        }
        if (test)
            testResults += "Pass.\n";
        else
            testResults += "Fail\n";

        print(testResults);



        testState = Rotate(testState);
        /*************
         * Init: Move all reds one tile to the left again, Past the array bounds
         *
         * Expected Result:
         * 7 ________
         * 6 ________
         * 5 ________
         * 4 _______B
         * 3 _______G
         * 2 _______R
         * 1 ________
         * 0 ________                           
         *   01234567
         ************/
        testState = MoveRight(testState);
        testState = MoveRight(testState);
        testState = MoveRight(testState);
        testState = MoveRight(testState);


        /*************
         * Test: Rotate with result out of bounds
         *
         * Expected Result: NULL
         * 
         ************/
        testResults = ("Rotate out of Bounds...");
        test = true;
        testState = MoveRight(testState);
        if (testState == null)
            testResults += (" Pass\n");
        else
            testResults += (" Fail\n");

        print(testResults);

        /**
         * Restore the global environment
         */

        boardWidth = globalWidth;
        boardHeight = globalHeight;

        /********************************************************
         **** State Evaluation Tests ****************************
         ********************************************************/

        /*************
         * Init: Test empty board to check EmptyTable values
         ************/

        testBoard = new SubState[boardWidth, boardHeight];

        for (int i = 0; i < boardHeight * boardWidth; i++)
        {
            testBoard[i % boardWidth, i / boardWidth] = new SubState();
        }

        testResults = ("Quantify Empty Table...");
        int emptyValues = 0;
        int testValue = QuantifyBoardState(new StateNode(testBoard));

        for (int i = 0; i < boardHeight * boardWidth; i++)
            emptyValues += emptyValueTable[i % boardWidth, i / boardWidth];

        if (emptyValues == testValue)
            testResults += (" Pass\n");
        else
        {
            testResults += (" Fail, " + emptyValues + " != " + testValue);
        }

        print(testResults);


        /*************
         * Init: Test full board to check EmptyTable values
        ************/
        testResults = ("Quantify Full Table...");

        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                if (x < 3)
                    testBoard[x, y].state = TileState.RED;
                else if (x < 6)
                    testBoard[x, y].state = TileState.GREEN;
                else
                    testBoard[x, y].state = TileState.BLUE;

                if (y == boardHeight - 1)
                    testBoard[x, y].state = TileState.BLUE;

                if (x == 0 && y != 0)
                    testBoard[x, y].state = TileState.BLUE;

                if (x == 1 && y == 1)
                    testBoard[x, y].state = TileState.BLUE;
            }
        }

        testValue = QuantifyBoardState(new StateNode(testBoard));
        if (315 == testValue)
            testResults += (" Pass\n");
        else
        {
            testResults += (" Fail, " + 315 + " != " + testValue);
        }


        /********************************************************
         **** State Tree Tests **********************************
         ********************************************************/
        int[,] globalEmptyTable = emptyValueTable;


        // Create testing environment
        boardWidth = 3;
        boardHeight = 4;

        int[,] testEmptyTable = new int[boardWidth, boardHeight];

        // Build fake emptyTable
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                if (x >= 2 || y > 0)
                    testEmptyTable[x, y] = 100;
                else
                    testEmptyTable[x, y] = 0;
            }
        }

        emptyValueTable = testEmptyTable;

        /**
         * Create board for test. Results in a 20 state tree.
         *
         * Expected Result:
         * 3 _R_
         * 2 _G_
         * 1 ___
         * 0 ___                          
         *   012
         ************/
        testBoard = new SubState[boardWidth, boardHeight];

        for (int i = 0; i < boardWidth * boardHeight; i++)
        {
            testBoard[i % boardWidth, i / boardWidth] = new SubState();
        }

        testBoard[1, 3].state = TileState.RED;
        testBoard[1, 3].isActive = true;

        testBoard[1, 2].state = TileState.GREEN;
        testBoard[1, 2].isActive = true;
        testBoard[1, 2].isPivot = true;

        root = new StateNode(testBoard);

        root.active.Add(new int[] { 1, 3 });
        root.active.Add(new int[] { 1, 2 });

        BuildRootsFromRoot();

        PrintBoardStates(root);
        print(root.value);
        PrintBoardStates(root.leftState);
        print(root.leftState.value);
        PrintBoardStates(root.leftState.belowState);
        print(root.leftState.belowState.value);
        PrintBoardStates(root.belowState);
        print(root.belowState.value);
        PrintBoardStates(root.rightState);
        print(root.rightState.value);
        PrintBoardStates(root.rightState.belowState);
        print(root.rightState.belowState.value);
        PrintBoardStates(root90);
        print(root90.value);
        PrintBoardStates(root90.leftState);
        print(root90.leftState.value);
        PrintBoardStates(root90.leftState.belowState);
        print(root90.leftState.belowState.value);
        PrintBoardStates(root90.belowState);

        print(ChooseMove().ToString());

        // Restore original environment
        boardWidth = globalWidth;
        boardHeight = globalHeight;
        emptyValueTable = globalEmptyTable;
    }

    #endregion
    
    #region Define Table

    public void BuildEmptyValueTable()
    {
        for (int y = 0; y < 4; y++)
        {
            emptyValueTable[0, y] = 1;
            emptyValueTable[1, y] = 2;
            emptyValueTable[2, y] = 3;
            emptyValueTable[3, y] = 4;
            emptyValueTable[4, y] = 3;
            emptyValueTable[5, y] = 2;
            emptyValueTable[6, y] = 1;
        }
        for (int y = 4; y < 8; y++)
        {
            emptyValueTable[0, y] = 2;
            emptyValueTable[1, y] = 4;
            emptyValueTable[2, y] = 6;
            emptyValueTable[3, y] = 8;
            emptyValueTable[4, y] = 6;
            emptyValueTable[5, y] = 4;
            emptyValueTable[6, y] = 2;
        }
        for (int y = 8; y < 10; y++)
        {
            emptyValueTable[0, y] = 3;
            emptyValueTable[1, y] = 8;
            emptyValueTable[2, y] = 12;
            emptyValueTable[3, y] = 16;
            emptyValueTable[4, y] = 12;
            emptyValueTable[5, y] = 8;
            emptyValueTable[6, y] = 3;
        }
        for (int y = 10; y < 11; y++)
        {
            emptyValueTable[0, y] = 6;
            emptyValueTable[1, y] = 8;
            emptyValueTable[2, y] = 12;
            emptyValueTable[3, y] = 16;
            emptyValueTable[4, y] = 12;
            emptyValueTable[5, y] = 8;
            emptyValueTable[6, y] = 6;
        }
        for (int y = 11; y < 13; y++)
        {
            emptyValueTable[0, y] = 12;
            emptyValueTable[1, y] = 16;
            emptyValueTable[2, y] = 24;
            emptyValueTable[3, y] = 32;
            emptyValueTable[4, y] = 24;
            emptyValueTable[5, y] = 16;
            emptyValueTable[6, y] = 12;
        }
    }

    #endregion

    #region Unity Functions

    // Use this for initialization
    void Start()
    {
        myGameBoard = GetComponent<GameBoardScript>();
        boardWidth = myGameBoard.boardWidth;
        boardHeight = myGameBoard.boardHeight;

        root = BuildGameState();

        emptyValueTable = new int[boardWidth, boardHeight];
        BuildEmptyValueTable();
    }

    // Update is called once per frame
    void Update()
    {
        root = BuildGameState();
        BuildRootsFromRoot();

        switch (ChooseMove())
        {
            case MoveChoice.LEFT:
                myGameBoard.MoveLeft();
                break;
            case MoveChoice.RIGHT:
                myGameBoard.MoveRight();
                break;
            case MoveChoice.ROTATE:
                myGameBoard.RotateTrominoRight();
                break;
            case MoveChoice.FALL:
                //if (myGameBoard.dropTime > myGameBoard.fastDropRate)
                //    myGameBoard.DropAllTiles();
                break;
            default:
                break;
        }
    }

    #endregion
}