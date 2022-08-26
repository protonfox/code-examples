using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;

// Script by Brandon Dines, Proton Fox 
public class Square : MonoBehaviour, IPointerClickHandler
{
    #region Variables
    public enum State
    {
        Unfilled,
        Selected,
        Incorrect,
        Correct,
    }

    public enum SquareColour
    {
        White,
        Other,
    }

    public enum SquareFill
    {
        Empty,
        Filled,
        Marked,
        Unsure,
    }

    public State currentState;
    public SquareColour colour;
    public SquareFill fill;
    public Vector2 squarePosition;
    public int marked;
    public int squareNumber;
    public bool correctSquare;
    public bool empty;
    public bool notEmpty;
    public Sprite mistakeMark;

    private int count;
    private int holdCount;
    private Picross picross;
    private PhotonView photonView;
    private Sprite prevMark;
    private float navTimer;

    #endregion

    #region Methods
    private void Awake() // Sets the squares default state.
    {
        currentState = State.Unfilled;
        colour = SquareColour.White;
        picross = GameObject.FindObjectOfType<Picross>();
        photonView = transform.GetComponent<PhotonView>();

    }

    private void Start() // Turns on the numbers of the grid if it has them.
    {
        if (empty)
        {
            SetMark(0, 0, 1);
        }

        if (transform.GetChild(1).childCount > 0)
        {
            transform.GetChild(1).gameObject.SetActive(true);
        }
        if (transform.GetChild(2).childCount > 0)
        {
            transform.GetChild(2).gameObject.SetActive(true);
        }

        squareNumber = transform.GetSiblingIndex();
        transform.GetComponent<Image>().sprite = picross.players[0].GetPlayerFillType();

        //AutoEmpty();

    }

    private void Update()
    {
        if (!picross.players[0].holding)
        {
            holdCount = 0;
        }

        ProgressState();

        if (PlayerPrefs.GetInt("GridRollover") == 1)
        {
            if (navTimer < 2)
            {
                navTimer += Time.deltaTime;
            }
            if (navTimer >= 2)
            {
                SetNavigation();
            }
        }
    }

    private void ProgressState() // Sets the state of the square. Unfilled/Filled, Correct/Incorrect.
    {
        if (correctSquare && fill == SquareFill.Filled && currentState != State.Correct)
        {
            picross.SetAnswer(1);
            currentState = State.Correct;
        }
        else if (correctSquare && fill != SquareFill.Filled && currentState != State.Unfilled && currentState != State.Incorrect)
        {
            picross.SetAnswer(-1);
            currentState = State.Unfilled;
        }
        else if (!correctSquare && fill == SquareFill.Filled && currentState != State.Incorrect)
        {
            picross.SetAnswer(-1);
            currentState = State.Incorrect;
        }
        else if (!correctSquare && fill != SquareFill.Filled && currentState != State.Unfilled)
        {
            picross.SetAnswer(1);
            currentState = State.Unfilled;
        }
    }

    public void Fill(int player, int undo) // Fills the square a colour or white if possible.
    {
        if (!empty)
        {
            if (holdCount == 0)
            {
                count++;
                if (count > 1)
                {
                    picross.players[player].blocksFilledMultipleTimes++;
                }
                if (colour == SquareColour.White && marked == 0 || fill == SquareFill.Unsure && undo == 0)
                {
                    if (!picross.players[player].holding || picross.players[player].holding && picross.players[player].heldState == SquareFill.Filled)
                    {
                        //picross.players[player].squaresFilled++;
                        if (marked != 0)
                        {
                            SetMark(player, undo,0);
                        }
                        SwapColour(player, picross.players[player].GetPlayerColour());
                        picross.SetSuspend(squareNumber, 1);
                        if (undo == 0)
                        {
                            picross.SaveBlockAction(player, gameObject, fill + "," + SquareFill.Filled);
                        }
                        fill = SquareFill.Filled;
                    }

                    if (picross.players[player].GetDifficulty() != Difficulty._Difficulty.Chill && undo == 0)
                    {
                        if (!correctSquare) // if difficulty ++
                        {
                            //currentState = State.Incorrect;
                            SwapColour(player, Color.white);
                            picross.players[player].SetPlayerError();
                            Mark(player, undo, 1);
                        }
                    }

                }
                else
                {
                    if (Picross.puzzleStartDone)
                    {
                        if (!picross.players[player].holding || picross.players[player].holding && picross.players[player].heldState == SquareFill.Empty)
                        {
                            SwapColour(player, Color.white);
                            picross.SetSuspend(squareNumber, 0);

                            if (undo == 0 && marked == 0)
                            {
                                picross.SaveBlockAction(player, gameObject, fill + "," + SquareFill.Empty);
                            }
                            if (marked != 0)
                            {
                                SetMark(player, undo, 0);
                            }
                            fill = SquareFill.Empty;
                        }
                    
                    }
                }

                holdCount++;
            }
        }
    }

    public void Mark(int player, int undo, int mark) // Fills the square white, or places a mark if possible.
    {
        if (!empty)
        {
            if (holdCount == 0)
            {
                if (!picross.players[player].holding && colour == SquareColour.White || picross.players[player].holding && colour == SquareColour.White)
                {
                        SetMark(player, undo, mark);
                        holdCount++;
                }
                if (colour != SquareColour.White && !picross.players[player].holding || picross.players[player].holding && picross.players[player].heldState == SquareFill.Empty)
                {
                    if (mark != 2)
                    {
                        SwapColour(player, Color.white);
                        picross.SetSuspend(squareNumber, 0);

                        if (undo == 0)
                        {
                            picross.SaveBlockAction(player, gameObject, fill + "," + SquareFill.Empty);
                        }
                        fill = SquareFill.Empty;
                    }
                }
            }
        }
    }

    public void SetMark(int player, int undo, int marker) // Turns the mark on and off.
    {
        GameObject mark = transform.GetChild(0).gameObject;

        if (mark.activeSelf)
        {
            if (Picross.puzzleStartDone)
            {
                if (marker == 1 && fill == SquareFill.Unsure)
                {
                    mark.GetComponent<Image>().sprite = picross.players[player].GetPlayerMarkType();
                    //picross.squaresMarked++;
                    marked = 1;
                    if (undo == 0 && !empty)
                    {
                        picross.SaveBlockAction(player, gameObject, fill + "," + SquareFill.Marked);
                    }
                    fill = SquareFill.Marked;
                }
                else if (marker == 2 && fill != SquareFill.Marked || marker == 1 || marker == 0)
                {
                    if (!picross.players[player].holding || picross.players[player].holding && picross.players[player].heldState != SquareFill.Marked && picross.players[player].heldState != SquareFill.Unsure)
                    {
                        mark.SetActive(false);
                        marked = 0;
                        picross.SetSuspend(squareNumber, 0);
                        if (undo == 0)
                        {
                            picross.SaveBlockAction(player, gameObject, fill + "," + SquareFill.Empty);
                        }
                        fill = SquareFill.Empty;
                    }
                }
            }

        }
        else
        {
            if (marker == 1)
            {
                if (!picross.players[player].holding || picross.players[player].holding && picross.players[player].heldState != SquareFill.Empty)
                {
                    mark.GetComponent<Image>().sprite = picross.players[player].GetPlayerMarkType();
                    //picross.squaresMarked++;
                    marked = 1;
                    if (undo == 0 && !empty)
                    {
                        picross.SaveBlockAction(player, gameObject, fill + "," + SquareFill.Marked);
                    }
                    fill = SquareFill.Marked;
                    picross.SetSuspend(squareNumber, 2);
                    mark.SetActive(true);
                }
            }
            if (marker == 2)
            {
                if (!picross.players[player].holding || picross.players[player].holding && picross.players[player].heldState == SquareFill.Unsure)
                {
                    {
                        mark.GetComponent<Image>().sprite = picross.players[player].GetPlayerUnsureType();
                        marked = 2;
                        if (undo == 0)
                        {
                            picross.SaveBlockAction(player, gameObject, fill + "," + SquareFill.Unsure);
                        }
                        fill = SquareFill.Unsure;
                        mark.SetActive(true);
                    }
                }
            }
        }
    }
    
    private void SwapColour(int player, Color _colour) // Swaps the squares colour to the relevant colour.
    {
        if (transform.GetComponent<Image>().color != picross.players[player].GetPlayerColour())
        {
            for (int i = 0; i < picross.players.Count; i++)
            {
                if (transform.GetComponent<Image>().color == picross.players[i].GetPlayerColour())
                {
                    player = i;
                }
            }
        }

        if (_colour != Color.white)
        {
            colour = SquareColour.Other;
            transform.GetComponent<Image>().color = _colour;
            picross.players[player].squaresFilled++;
        }
        else
        {
            if (colour != SquareColour.White)
            {
                picross.players[player].squaresFilled--;
            }
            colour = SquareColour.White;
            transform.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f, 1);
        }
    }

    public void SetNavigation() // Sets button navigation of each square to allow rollover when edge of grid reached.
    {
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.Explicit;

        if (squarePosition.x != 0)
        {
            nav.selectOnUp = picross.puzzle.GetChild(0).GetChild(squareNumber - ((int)picross.gridSize.y)).GetComponent<Button>();
        }
        else
        {
            nav.selectOnUp = picross.puzzle.GetChild(0).GetChild((picross.puzzle.GetChild(0).childCount - (int)picross.gridSize.x) + (int)squarePosition.y).GetComponent<Button>();
        }

        if (squarePosition.x != picross.gridSize.y - 1)
        {
            nav.selectOnDown = picross.puzzle.GetChild(0).GetChild(squareNumber + ((int)picross.gridSize.y)).GetComponent<Button>();
        }
        else
        {
            nav.selectOnDown = picross.puzzle.GetChild(0).GetChild((int)squarePosition.y).GetComponent<Button>();
        }

        if (squarePosition.y != 0)
        {
            nav.selectOnLeft = picross.puzzle.GetChild(0).GetChild(squareNumber - 1).GetComponent<Button>();
        }
        else
        {
            nav.selectOnLeft = picross.puzzle.GetChild(0).GetChild(squareNumber + ((int)picross.gridSize.x - 1)).GetComponent<Button>();
        }

        if (squarePosition.y != picross.gridSize.x - 1)
        {
            nav.selectOnRight = picross.puzzle.GetChild(0).GetChild(squareNumber + 1).GetComponent<Button>();
        }
        else
        {
            nav.selectOnRight = picross.puzzle.GetChild(0).GetChild(squareNumber - ((int)picross.gridSize.x - 1)).GetComponent<Button>();
        }

        GetComponent<Button>().navigation = nav;
    }
    #endregion
}
