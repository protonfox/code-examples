using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab.ClientModels;
using PlayFab;

// Script by Brandon Dines, Proton Fox 
public class SaveSystem : MonoBehaviour
{
    #region Variables

    #endregion


    #region Methods
    public void Save(int puzzle) // Saves local created puzzle, with correct answers and puzzle size.
    {
        Save puzzleSave = new Save();
        puzzleSave.puzzleRowAnswers = Creator.puzzleRowSave;
        puzzleSave.puzzleColAnswers = Creator.puzzleColSave;
        puzzleSave.puzzleSize = Creator.puzzleGridSave;
        string puzzleData = JsonUtility.ToJson(puzzleSave);
        PlayerPrefs.SetString("SavedPuzzle" + puzzle, puzzleData);

        PlayerPrefs.Save();
        Debug.Log("Local SAVED");
        OnlineSave(puzzle, puzzleData);

        //Debug.Log(puzzleData);
    }

    public void SuspendSave(int puzzle) // Saves suspended arcade puzzle, keeps current answers for later load.
    {
        Save puzzleSave = new Save();
        puzzleSave.suspendRow = Picross.suspendedRowSave;
        //puzzleSave.puzzleColAnswers = Picross.suspendedColSave;
        string puzzleData = JsonUtility.ToJson(puzzleSave);
        PlayerPrefs.SetString("SuspendedPuzzle" + puzzle, puzzleData);

        PlayerPrefs.Save();
        Debug.Log("Local SAVED");
    }

    public void Load(int puzzle) // Loads saved local creator puzzle, a puzzle created by the player.
    {
        string puzzleData = PlayerPrefs.GetString("SavedPuzzle" + puzzle);
        Save puzzleLoad = JsonUtility.FromJson<Save>(puzzleData);
        Creator.puzzleRowSave = puzzleLoad.puzzleRowAnswers;
        Creator.puzzleColSave = puzzleLoad.puzzleColAnswers;
        Creator.puzzleGridSave = puzzleLoad.puzzleSize;
        Debug.Log("LOADED");
        //Debug.Log(puzzleData);
    }
    public void LoadSuspended(int puzzle) // Loads suspended arcade puzzle, a puzzle in the story mode of the game.
    {
        string puzzleData = PlayerPrefs.GetString("SuspendedPuzzle" + puzzle);
        Save puzzleLoad = JsonUtility.FromJson<Save>(puzzleData);
        Picross.suspendedRowSave = puzzleLoad.suspendRow;
        //Picross.suspendedColSave = puzzleLoad.puzzleColAnswers;
        Debug.Log("LOADED");
        //Debug.Log(puzzleData);
    }

    public static void SaveFromScript(int puzzle) // Saves creator puzzle from script.
    {
        Save puzzleSave = new Save();
        puzzleSave.puzzleRowAnswers = Creator.puzzleRowSave;
        puzzleSave.puzzleColAnswers = Creator.puzzleColSave;
        puzzleSave.puzzleSize = Creator.puzzleGridSave;
        string puzzleData = JsonUtility.ToJson(puzzleSave);
        PlayerPrefs.SetString("SavedPuzzle" + puzzle, puzzleData);
        PlayerPrefs.Save();
    }

    public void OnlineSave(int puzzle, string puzzleData) // Saves created puzzle online.
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "Puzzles" + puzzle, puzzleData }
            }
        };
        PlayFabClientAPI.UpdateUserData(request, OnDataSend, OnError);
    }

    private static void OnError(PlayFabError obj) // Calls when error in data send.
    {
        Debug.Log("DATA SEND FAILED");
    }

    private void OnDataSend(UpdateUserDataResult obj) // Calls when data sent.
    {
        Debug.Log("DATA SENT ONLINE");
    }

    public static void OnlineLoad() // Load online puzzle.
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnPuzzleRecieve, OnError);
    }

    private static void OnPuzzleRecieve(GetUserDataResult result) // NOT CURRENTLY WORKING, loads online puzzle.
    {
        if (result.Data != null)
        {
            for (int i = 0; i < 10; i++)
            {
                if (result.Data.ContainsKey("Puzzles" + i))
                {
                    string puzzleData = result.Data["Puzzles"].Value;
                    Save puzzleLoad = JsonUtility.FromJson<Save>(puzzleData);
                    Creator.puzzleRowSave = puzzleLoad.puzzleRowAnswers;
                    Creator.puzzleColSave = puzzleLoad.puzzleColAnswers;
                    Creator.puzzleGridSave = puzzleLoad.puzzleSize;
                    Creator.totalOnlinePuzzles++;
                    Debug.Log("Online LOADED");
                }
            }
        }
    
    }

    #endregion
}
