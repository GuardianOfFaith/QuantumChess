/*
 * Copyright (c) 2018 Razeware LLC
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * Notwithstanding the foregoing, you may not use, copy, modify, merge, publish, 
 * distribute, sublicense, create a derivative work, and/or sell copies of the 
 * Software in any work that is designed, intended, or marketed for pedagogical or 
 * instructional purposes related to programming, coding, application development, 
 * or information technology.  Permission for such use, copying, modification,
 * merger, publication, distribution, sublicensing, creation of derivative works, 
 * or sale is expressly withheld.
 *    
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public Board board;

    public GameObject whiteKing;
    public GameObject whiteQueen;
    public GameObject whiteBishop;
    public GameObject whiteKnight;
    public GameObject whiteRook;
    public GameObject whitePawn;

    public GameObject blackKing;
    public GameObject blackQueen;
    public GameObject blackBishop;
    public GameObject blackKnight;
    public GameObject blackRook;
    public GameObject blackPawn;

    private GameObject[,] pieces;
    private List<GameObject> movedPawns;

    private Player white;
    private Player black;
    public Player currentPlayer;
    public Player otherPlayer;

    /// <summary>
    /// Quantum Part
    /// </summary>
    Socket socket;
    int shots = 1024;

    //Interface
    public UnityEngine.UI.Image connection;
    public GameObject panel;
    public UnityEngine.UI.Text winnerDisplay;

    void Awake()
    {
        instance = this;
    }

    void Start ()
    {
        pieces = new GameObject[8, 8];
        movedPawns = new List<GameObject>();

        white = new Player("white", true);
        black = new Player("black", false);

        currentPlayer = white;
        otherPlayer = black;

        socket = Socket.GetInstance();
        InitialSetup();
    }

    void Update()
    {
        if (socket.isConnected())
            connection.color = Color.green;
        else
            connection.color = Color.red;
    }

    private void InitialSetup()
    {
        AddPiece(whiteRook, white, 0, 0);
        AddPiece(whiteKnight, white, 1, 0);
        AddPiece(whiteBishop, white, 2, 0);
        AddPiece(whiteQueen, white, 3, 0);
        AddPiece(whiteKing, white, 4, 0);
        AddPiece(whiteBishop, white, 5, 0);
        AddPiece(whiteKnight, white, 6, 0);
        AddPiece(whiteRook, white, 7, 0);

        for (int i = 0; i < 8; i++)
        {
            AddPiece(whitePawn, white, i, 1);
        }

        AddPiece(blackRook, black, 0, 7);
        AddPiece(blackKnight, black, 1, 7);
        AddPiece(blackBishop, black, 2, 7);
        AddPiece(blackQueen, black, 3, 7);
        AddPiece(blackKing, black, 4, 7);
        AddPiece(blackBishop, black, 5, 7);
        AddPiece(blackKnight, black, 6, 7);
        AddPiece(blackRook, black, 7, 7);

        for (int i = 0; i < 8; i++)
        {
            AddPiece(blackPawn, black, i, 6);
        }
    }

    public void AddPiece(GameObject prefab, Player player, int col, int row)
    {
        GameObject pieceObject = board.AddPiece(prefab, col, row);
        player.pieces.Add(pieceObject);
        pieces[col, row] = pieceObject;
    }

    public GameObject AddPieceQT(GameObject prefab, Player player, int col, int row)
    {
        GameObject pieceObject = board.AddPiece(prefab, col, row);
        player.pieces.Add(pieceObject);
        pieces[col, row] = pieceObject;
        return pieceObject;
    }

    public void SelectPieceAtGrid(Vector2Int gridPoint)
    {
        GameObject selectedPiece = pieces[gridPoint.x, gridPoint.y];
        if (selectedPiece)
        {
            board.SelectPiece(selectedPiece);
        }
    }

    public List<Vector2Int> MovesForPiece(GameObject pieceObject)
    {
        Piece piece = pieceObject.GetComponent<Piece>();
        Vector2Int gridPoint = GridForPiece(pieceObject);
        List<Vector2Int> locations = piece.MoveLocations(gridPoint);

        // filter out offboard locations
        locations.RemoveAll(gp => gp.x < 0 || gp.x > 7 || gp.y < 0 || gp.y > 7);

        // filter out locations with friendly piece
        locations.RemoveAll(gp => FriendlyPieceAt(gp));

        return locations;
    }

    public void Move(GameObject piece, Vector2Int gridPoint)
    {
        if (piece.GetComponent<QuanticDouble>() != null)
        {
            GameObject copy = piece.GetComponent<QuanticDouble>().original;
            Vector2Int quantumGridPoint = GridForPiece(copy);
            pieces[quantumGridPoint.x, quantumGridPoint.y] = null;
            Destroy(copy);
            Destroy(piece.GetComponent<QuanticDouble>());
        }

        Piece pieceComponent = piece.GetComponent<Piece>();
        if (pieceComponent.type == PieceType.Pawn && !HasPawnMoved(piece))
        {
            movedPawns.Add(piece);
        }

        Vector2Int startGridPoint = GridForPiece(piece);
        pieces[startGridPoint.x, startGridPoint.y] = null;
        pieces[gridPoint.x, gridPoint.y] = piece;
        board.MovePiece(piece, gridPoint);
    }

    public void QuantumMove(GameObject piece, Vector2Int gridPoint, Vector2Int gridPointDual)
    {
        GameObject copy;
        if (piece.GetComponent<QuanticDouble>() != null)
        {
            copy = piece.GetComponent<QuanticDouble>().original;
            Vector2Int quantumGridPoint = GridForPiece(copy);
            pieces[quantumGridPoint.x, quantumGridPoint.y] = null;

            Destroy(copy);
        }

        Piece pieceComponent = piece.GetComponent<Piece>();
        if (pieceComponent.type == PieceType.Pawn && !HasPawnMoved(piece))
        {
            movedPawns.Add(piece);
        }
        copy = AddPieceQT(piece, currentPlayer, gridPointDual.x, gridPointDual.y);
        board.MakeQuantumPiece(piece);
        board.MakeQuantumPiece(copy);

        QuanticDouble Qt1; 
        if (copy.GetComponent<QuanticDouble>() != null)
            Qt1 = copy.GetComponent<QuanticDouble>();
        else
            Qt1 = copy.AddComponent<QuanticDouble>();
        QuanticDouble Qt2;
        if (piece.GetComponent<QuanticDouble>() != null)
            Qt2 = piece.GetComponent<QuanticDouble>();
        else
            Qt2 = piece.AddComponent<QuanticDouble>();
        Qt1.original = piece;
        Qt2.original = copy;

        Vector2Int startGridPoint = GridForPiece(piece);
        pieces[startGridPoint.x, startGridPoint.y] = null;
        pieces[gridPoint.x, gridPoint.y] = piece;
        board.MovePiece(piece, gridPoint);

        Vector2Int startGridPoint2 = GridForPiece(copy);
        pieces[gridPointDual.x, gridPointDual.y] = copy;
        board.MovePiece(copy, gridPointDual);
    }

    public void PawnMoved(GameObject pawn)
    {
        movedPawns.Add(pawn);
    }

    public bool HasPawnMoved(GameObject pawn)
    {
        return movedPawns.Contains(pawn);
    }

    public void CapturePieceAt(Vector2Int gridPoint)
    {
        GameObject pieceToCapture = PieceAtGrid(gridPoint);

        //QuantumCheck
        if (pieceToCapture.GetComponent<QuanticDouble>() != null)
        {
            string responseQT = Socket.SendMessageQT(pieceToCapture);
            string[] resp = responseQT.Split(';');
            Debug.Log(resp[1] + ';'+ resp[3]);

            //Piece is there
            if (int.Parse(resp[1]) > int.Parse(resp[3]))
            {
                Debug.Log("WasThere");
                if (pieceToCapture.GetComponent<Piece>().type == PieceType.King)
                {
                    Debug.Log(currentPlayer.name + " wins!");
                    Destroy(board.GetComponent<TileSelector>());
                    Destroy(board.GetComponent<MoveSelector>());
                }
                currentPlayer.capturedPieces.Add(pieceToCapture);

                GameObject copy = pieceToCapture.GetComponent<QuanticDouble>().original;
                Vector2Int quantumGridPoint = GridForPiece(copy);
                pieces[quantumGridPoint.x, quantumGridPoint.y] = null;
                Destroy(copy);
                Destroy(pieceToCapture.GetComponent<QuanticDouble>());

                pieces[gridPoint.x, gridPoint.y] = null;
                Destroy(pieceToCapture);
            }
            else
            {
                board.RevertQuantumPiece(pieceToCapture.GetComponent<QuanticDouble>().original);
                Destroy(pieceToCapture.GetComponent<QuanticDouble>().original.GetComponent<QuanticDouble>());

                pieces[gridPoint.x, gridPoint.y] = null;
                Destroy(pieceToCapture);
            }
        }
        else
        {
            if (pieceToCapture.GetComponent<Piece>().type == PieceType.King)
            {
                Debug.Log(currentPlayer.name + " wins!");
                Destroy(board.GetComponent<TileSelector>());
                Destroy(board.GetComponent<MoveSelector>());
            }
            pieces[gridPoint.x, gridPoint.y] = null;
            Destroy(pieceToCapture);
        }
    }

    public void SelectPiece(GameObject piece)
    {
        board.SelectPiece(piece);
    }

    public void DeselectPiece(GameObject piece)
    {
        board.DeselectPiece(piece);
    }

    public bool DoesPieceBelongToCurrentPlayer(GameObject piece)
    {
        return currentPlayer.pieces.Contains(piece);
    }

    public GameObject PieceAtGrid(Vector2Int gridPoint)
    {
        if (gridPoint.x > 7 || gridPoint.y > 7 || gridPoint.x < 0 || gridPoint.y < 0)
        {
            return null;
        }
        return pieces[gridPoint.x, gridPoint.y];
    }

    public Vector2Int GridForPiece(GameObject piece)
    {
        for (int i = 0; i < 8; i++) 
        {
            for (int j = 0; j < 8; j++)
            {
                if (pieces[i, j] == piece)
                {
                    return new Vector2Int(i, j);
                }
            }
        }

        return new Vector2Int(-1, -1);
    }

    public bool FriendlyPieceAt(Vector2Int gridPoint)
    {
        GameObject piece = PieceAtGrid(gridPoint);

        if (piece == null) {
            return false;
        }

        if (otherPlayer.pieces.Contains(piece))
        {
            return false;
        }

        return true;
    }

    public void NextPlayer()
    {
        Player tempPlayer = currentPlayer;
        currentPlayer = otherPlayer;
        otherPlayer = tempPlayer;
    }

    public void Win(Player winner)
    {
        panel.SetActive(true);
        winnerDisplay.text = currentPlayer.name + " wins!";
    }

    public void reload()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
    public void quit()
    {
        Application.Quit();
    }
}
