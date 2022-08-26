using System.Collections.Generic;
using UnityEngine;


// Script by Brandon Dines, Proton Fox 
public class SquareMemory : MonoBehaviour
{
    #region Variables
    public List<Player> players = new List<Player>();

    private List<GameObject> allTouchedBlocks = new List<GameObject>();
    private List<string> allBlockActions = new List<string>();
    private int replayCurrentBlock;
    private float replayTimer;
    private bool replayReset;
    private bool replay;
    private Picross picross;
    #endregion

    #region Methods

    public void SaveBlockAction(int player, GameObject block, string action) // Saves the last block the player interacted with and its current state.
    {
        if (players[player].lastTouchedBlocks.Count >= 500)
        {
            players[player].lastTouchedBlocks.RemoveAt(0);
        }
        players[player].lastTouchedBlocks.Add(block);

        if (players[player].lastBlockActions.Count >= 500)
        {
            players[player].lastBlockActions.RemoveAt(0);
        }
        players[player].lastBlockActions.Add(action);

        allTouchedBlocks.Add(block);
        allBlockActions.Add(action);
    }

    public void Undo(int player) // Undo the player's last action. Takes actions from a list of recorded actions and reverses it.
    {
        if (Picross.puzzleActive)
        {
            if (players[player].lastBlockActions.Count > 0)
            {
                GameObject lastTouched = players[player].lastTouchedBlocks[players[player].lastTouchedBlocks.Count - 1];
                string lastAction = players[player].lastBlockActions[players[player].lastBlockActions.Count - 1];

                lastTouched.GetComponent<Square>().UndoAction(player, lastAction);
                if (InputManager.instance[player].DeviceName() != "Mouse")
                {
                    InputManager.instance[player].es.SetSelectedGameObject(null);
                    InputManager.instance[player].es.SetSelectedGameObject(lastTouched);
                    picross.NumberFade(player, 1);
                    picross.NumberFade(player, 2);
                }
                players[player].lastTouchedBlocks.RemoveAt(players[player].lastTouchedBlocks.Count - 1);
                players[player].lastBlockActions.RemoveAt(players[player].lastBlockActions.Count - 1);
            }
        }
    }

    public void Replay(bool play) // Used to replay the player's action after a puzzle is finished. Shows the progress they made throughout.
    {
        if (replay)
        {
            if (!replayReset)
            {
                for (int i = 0; i < picross.puzzle.GetChild(0).childCount; i++)
                {
                    picross.puzzle.GetChild(0).GetChild(i).GetComponent<Square>().fill = Square.SquareFill.Empty;
                }
                replayReset = true;
            }
            if (replayCurrentBlock != allTouchedBlocks.Count)
            {
                replayTimer += Time.deltaTime;
                if (replayTimer > 0.5f)
                {
                    allTouchedBlocks[replayCurrentBlock].GetComponent<Square>().UndoAction(0, allBlockActions[replayCurrentBlock]);
                    replayCurrentBlock++;
                    replayTimer = 0;
                }
            }
        }

    }
    #endregion
}
