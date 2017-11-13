/**
 *  Unity InputManager.asset file pulled from https://forum.unity.com/
 *      under "Attribution-ShareAlike 3.0 Unported (CC BY-SA 3.0)"
 *      
 *      Share — copy and redistribute the material in any medium or format
 *      Adapt — remix, transform, and build upon the material
 *          for any purpose, even commercially.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    /* Names chosen with Xbox controller in mind */
    public struct InputNames
    {
        public string playerSuffix;

        public string LeftHorizontal;
        public string LeftVertical;
        public string RightHorizontal;
        public string RightVertical;

        public string DPadX;
        public string DPadY;

        public string ButtonX;
        public string ButtonA;
        public string ButtonB;
        public string ButtonY;

        public string LeftBumper;
        public string LeftTrigger;
        public string RightBumper;
        public string RightTrigger;
    }

    public GameBoardScript myGameBoard;
    InputNames myInputNames;

    public string playerSuffix;

    public string LeftHorizontal;
    public string LeftVertical;
    public string RightHorizontal;
    public string RightVertical;

    public string DPadX;
    public string DPadY;

    public string ButtonX;
    public string ButtonA;
    public string ButtonB;
    public string ButtonY;

    public string LeftBumper;
    public string LeftTrigger;
    public string RightBumper;
    public string RightTrigger;

    public float joythreshold;
    private bool joyFlag_LeftX;
    
    public void KeyboardInput()
    {
        /* Player Input */
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            myGameBoard.MoveLeft();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            myGameBoard.MoveRight();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            myGameBoard.RotateTrominoRight();
        }
        if (Input.GetKey(KeyCode.Space))
        {
            if (myGameBoard.FallingTiles.Count == 0)
                return;

            if (myGameBoard.dropTime > myGameBoard.fastDropRate)
                myGameBoard.DropAllTiles();
        }
        if (Input.GetKey(KeyCode.Keypad1))
        {
            myGameBoard.PopTiles(TileScript.TileState.RED);
            return;
        }
        if (Input.GetKey(KeyCode.Keypad2))
        {
            myGameBoard.PopTiles(TileScript.TileState.BLUE);
            return;
        }
        if (Input.GetKey(KeyCode.Keypad3))
        {
            myGameBoard.PopTiles(TileScript.TileState.GREEN);
            return;
        }
        if (Input.GetKey(KeyCode.Keypad4))
        {
            myGameBoard.PopTiles(TileScript.TileState.YELLOW);
            return;
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Break();
        }

    }
    
    public void JoypadInput()
    {
        float leftX = Input.GetAxis(myInputNames.LeftHorizontal);
        float leftY = Input.GetAxis(myInputNames.LeftVertical);

        float dPadX = Input.GetAxis(myInputNames.DPadX);
        float dPadY = Input.GetAxis(myInputNames.DPadY);

        /* Player Input */
        if ((Mathf.Min(leftX, dPadX) < -joythreshold && !(joyFlag_LeftX)))
        {
            myGameBoard.MoveLeft();
            joyFlag_LeftX = true;
        }
        else
        if ((Mathf.Max(leftX, dPadX) > joythreshold && !(joyFlag_LeftX)))
        {
            myGameBoard.MoveRight();
            joyFlag_LeftX = true;
        }
        if (Input.GetButtonDown(myInputNames.ButtonY))
        {
            myGameBoard.RotateTrominoRight();
        }
        
        if (Mathf.Min(leftY, dPadY) < -(joythreshold))
        {
            if (myGameBoard.dropTime > myGameBoard.fastDropRate)
                myGameBoard.DropAllTiles();
        }

        if(Mathf.Max(Mathf.Abs(leftX), Mathf.Abs(dPadX)) < .8f)
        {
            joyFlag_LeftX = false;
        }
    }

    public InputNames SetUpInputNames(string suffix)
    {
        InputNames input = new InputNames();

        input.playerSuffix = suffix;

        input.LeftHorizontal = "" + this.LeftHorizontal + suffix;
        input.LeftVertical = "" + this.LeftVertical + suffix;
        input.RightHorizontal = "" + this.RightHorizontal + suffix;
        input.RightVertical = "" + this.RightVertical + suffix;
        
        input.DPadX = "" + this.DPadX + suffix;
        input.DPadY = "" + this.DPadY + suffix;

        input.ButtonX = "" + this.ButtonX + suffix;
        input.ButtonA = "" + this.ButtonA + suffix;
        input.ButtonB = "" + this.ButtonB + suffix;
        input.ButtonY = "" + this.ButtonY + suffix;

        input.LeftBumper = "" + this.LeftBumper + suffix;
        input.LeftTrigger = "" + this.LeftTrigger + suffix;
        input.RightBumper = "" + this.RightBumper + suffix;
        input.RightTrigger = "" + this.RightTrigger + suffix;

        return input;
    }

    // Use this for initialization
    void Start()
    {
        myInputNames = SetUpInputNames(playerSuffix);

        joyFlag_LeftX = false;
    }


    // Update is called once per frame
    void Update()
    {
        KeyboardInput();
        JoypadInput();
    }
}
