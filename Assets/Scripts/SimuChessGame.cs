using System;
using System.Collections;
using System.Collections.Generic;

public class SimuChessGame
{
    //7 a8 b8 c8 d8 e8 f8 g8 h8   
    //6 a7
    //5 a6
    //4 a5
    //3 a4
    //2 a3
    //1 a2
    //0 a1
    //  0  1  2  3  4  5  6  7

    //x = rivit, eli x++ on oikealle laudalla, y = sarakkeet eli y++ on ylöspäin laudalla. Miinukset luonnollisesti päinvastaiseen suuntaan. Valkoiset aloittavat alhaalta ja mustat ylhäältä

    //pelilauta on string,string array, missä on 64 ruutua. "" tarkoittaa tyhjää ruutua ja pelinappula merkitään stringeinä tyyliin "väri_nappula"
    //esim valkoinen torni on "white_rook"
    private string[,] board = new string[8, 8]; //row, column

    //laitetaan kaikki pelilaudalla olevat mahdolliset siirrot yhteen listaan
    //listalla jokainen itemi on yksi mahdollinen siirto eroteltuna väliyönneillä. 
    private List<List<int>> allAvailableMoves = new List<List<int>>(); //int list: currentPositionX, currentPositionY, moveToX, moveToY
    private List<List<int>> anticipatoryMovePositions = new List<List<int>>();

    //points for capturing, eka input on string, eli nappula joka syö vastustajan ja toka input on toinen string joka on se kohde. 
    public Dictionary<string, Dictionary<string, int>> pointsForCapturing =
    new Dictionary<string, Dictionary<string, int>>
    {
        {"Pawn", new Dictionary<string, int>    {{"Pawn", 1},{"Rook", 3},{"Bishop", 3},{"Knight", 3},{"Queen", 5},{"King", 10}}},
        {"Knight", new Dictionary<string, int>  {{"Pawn", 1},{"Rook", 2},{"Bishop", 3},{"Knight", 4},{"Queen", 5},{"King", 5}}},
        {"Bishop", new Dictionary<string, int>  {{"Pawn", 1},{"Rook", 2},{"Bishop", 3},{"Knight", 4},{"Queen", 5},{"King", 5}}},
        {"Rook", new Dictionary<string, int>    {{"Pawn", 1},{"Rook", 2},{"Bishop", 3},{"Knight", 4},{"Queen", 5},{"King", 5}}},
        {"Queen", new Dictionary<string, int>   {{"Pawn", 1},{"Rook", 1},{"Bishop", 1},{"Knight", 1},{"Queen", 2},{"King", 3}}},
        {"King", new Dictionary<string, int>    {{"Pawn", 5},{"Rook", 5},{"Bishop", 5},{"Knight", 5},{"Queen", 5},{"King", 10}}},
    };

    //jos kaksi nappulaa siirtyy samaan ruutuun, tämä taulukko ratkaisee kumpi syö kumman
    public Dictionary<string, int> pieceCollisionStregth = new Dictionary<string, int>()
    {
        {"pawn",    5},            //Determines who captures who, if pieces move to the same square. bigger eats smaller
        {"knight",  4},          
        {"bishop",  3},          
        {"rook",    2},
        {"queen",   1},
        {"king",    0},
    };

    bool whiteMoved = false;
    bool blackMoved = false;
    List<int> whiteMove = null;
    List<int> blackMove = null;
    string promotedWhitePiece = "";
    string promotedBlackPiece = "";
    public int whitePoints = 0;
    public int blackPoints = 0;
    public List<string> capturedWhitePieces;
    public List<string> capturedBlackPieces;

    //alustetaan pelilauta, laitetaan palikat aloituspaikkoihin jne
    public void InitializeGame()
    {   
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                board[x, y] = "";
            }
        }

        //white pawns & black pawns
        for (int p = 0; p < 8; p++)
        {
            board[p, 1] = "white_pawn";
            board[p, 6] = "black_pawn";
        }

        board[0, 0] = board[7, 0] = "white_rook";
        board[1, 0] = board[6, 0] = "white_knight";
        board[2, 0] = board[5, 0] = "white_bishop";
        board[3, 0] = "white_queen";
        board[4, 0] = "white_king";

        board[0, 7] = board[7, 7] = "black_rook";
        board[1, 7] = board[6, 7] = "black_knight";
        board[2, 7] = board[5, 7] = "black_bishop";
        board[3, 7] = "black_queen";
        board[4, 7] = "black_king";

        CheckAllAvailableMoves();
    }

    //käydään koko pelilauta läpi, katsotaan joka nappulan kaikki mahdolliset siirrot ja lisätään ne yhteen listaan.
    private void CheckAllAvailableMoves()
    {
        allAvailableMoves.Clear();
        anticipatoryMovePositions.Clear();
        //loop through all board positions
        //y = rows
        for (int y = 0; y < 8; y++)
        {
            //x = columns
            for (int x = 0; x < 8; x++)
            {
                //blank or piece
                string currentPiece = board[x, y];

                if (currentPiece != "")
                {
                    string currentColor = currentPiece.Substring(0, 5);
                    string currentUnit = currentPiece.Substring(6, currentPiece.Length - 6);

                    //pawns
                    if (currentUnit == "pawn")
                    {
                        AddAvailableMovesForPawns(currentColor, x, y);
                    }

                    if (currentUnit == "rook")
                    {
                        AddAvailableMovesForRooks(x, y);
                    }

                    if (currentUnit == "knight")
                    {
                        AddAvailableMovesForKnights(x, y);
                    }

                    if (currentUnit == "bishop")
                    {
                        AddAvailableMovesForBishops(x, y);
                    }

                    if (currentUnit == "king")
                    {
                        AddAvailableMovesForKings(x, y);
                    }

                    if (currentUnit == "queen")
                    {
                        AddAvailableMovesForRooks(x, y);
                        AddAvailableMovesForBishops(x, y);
                    }
                }
            }
        }
    }

    //solttujen siirtojen tarkistus
    private void AddAvailableMovesForPawns(string color, int xPos, int yPos)
    {
        //whites: 
        if (color == "white")
        {
            //one step forward
            if (yPos < 7)
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos, yPos + 1 });
            }

            //try to capture above and to the right or anticipate opponent move
            if (xPos < 7 && yPos < 7 && board[xPos + 1, yPos + 1] != "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + 1, yPos + 1 });
            }

            //capture above and to the left
            if (xPos > 0 && yPos < 7 && board[xPos - 1, yPos + 1] != "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - 1, yPos + 1 });
            }

            //still in starting position
            if (yPos == 1)
            {   
                //if one of the squares is empty in front, double move is a possibility
                if (board[xPos, yPos + 1] == "" || board[xPos, yPos + 2] == "")
                {
                    allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos, yPos + 2 });
                }
            }
        }
        else //blacks
        {
            //one step forward if it's empty
            if (yPos > 0)
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos, yPos - 1 });
            }

            //try to capture below and to the left (from whites view)
            if (xPos > 0 && yPos > 0 && board[xPos - 1, yPos - 1] != "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - 1, yPos - 1 });
            }

            //capture below and to the right (viewing from white side)
            if (xPos < 7 && yPos > 0 && board[xPos + 1, yPos - 1] != "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + 1, yPos - 1 });
            }

            //if at starting position
            if (yPos == 6) 
            {
                //if one of the squares is empty in front, double move is a possibility
                if (board[xPos, yPos - 1] == "" || board[xPos, yPos - 2] == "")
                {
                    allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos, yPos - 2 });
                }

            }
        }
    }

    //tornien siirtojen tarkistus
    private void AddAvailableMovesForRooks(int xPos, int yPos)
    {
        //right---------------------------------------------------------------------------------
        for (int move = 1; move < 8; move++)
        {
            //if we are currently on the right edge
            if (xPos == 7)
                break;

            //next move would be over the right edge
            if (xPos + move == 8)
                break;

            //next is empty square, add it and continue
            if (board[xPos + move, yPos] == "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + move, yPos });
                continue;
            }

            //next is not empty, add it, then break
            if (board[xPos + move, yPos] != "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + move, yPos });
                
                //check if there's a square after that and add it as anticipatory
                if (xPos + move + 1 < 8)
                {
                    allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + move + 1, yPos });
                }
                break;
            }
        }

        //left---------------------------------------------------------------------------------
        for (int move = 1; move < 8; move++)
        {
            //if we are currently on the left edge
            if (xPos == 0)
                break;

            //next move would be over the left edge
            if (xPos - move == -1)
                break;

            //next is empty square, add it and continue
            if (board[xPos - move, yPos] == "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - move, yPos });
                continue;
            }

            //next is not empty, add it, then break
            if (board[xPos - move, yPos] != "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - move, yPos });
                
                //check if there's a square after that and add it as anticipatory
                if (xPos - move - 1 >= 0)
                {
                    allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - move - 1, yPos });
                }
                break;
            }
        }

        //up---------------------------------------------------------------------------------
        for (int move = 1; move < 8; move++)
        {
            //if we are currently on the upper edge
            if (yPos == 7)
                break;

            //next move would be over the upper edge
            if (yPos + move == 8)
                break;

            //next is empty square, add it and continue
            if (board[xPos, yPos + move] == "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos, yPos + move});
                continue;
            }

            //next is not empty, add it, then break
            if (board[xPos, yPos + move] != "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos, yPos + move });

                //check if there's a square after that and add it as anticipatory
                if (yPos + move + 1 < 8)
                {
                    allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos, yPos + move + 1 });
                }
                break;
            }
        }

        //down---------------------------------------------------------------------------------
        for (int move = 1; move < 8; move++)
        {
            //if we are currently on the lower edge
            if (yPos == 0)
                break;

            //next move would be over the lower edge
            if (yPos - move == -1)
                break;

            //next is empty square, add it and continue
            if (board[xPos, yPos - move] == "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos, yPos - move});
                continue;
            }

            //next is not empty, add it, then break
            if (board[xPos, yPos - move] != "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos, yPos - move });

                //check if there's a square after that and add it as anticipatory
                if (yPos - move - 1 >= 0)
                {
                    allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos, yPos - move - 1});
                }
                break;
            }
        }
    }

    //Ratsujen siirtojen tarkistus
    private void AddAvailableMovesForKnights(int xPos, int yPos)
    {
        //1 right, 2 up
        if (xPos + 1 < 8 && yPos + 2 < 8)
        {
            allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + 1, yPos + 2 });
        }

        //2 right, 1 up
        if (xPos + 2 < 8 && yPos + 1 < 8)
        {
            allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + 2, yPos + 1 });
        }

        //2 right, 1 down
        if (xPos + 2 < 8 && yPos - 1 > -1)
        {
            allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + 2, yPos - 1 });
        }

        //1 right, 2 down
        if (xPos + 1 < 8 && yPos - 2 > -1)
        {
            allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + 1, yPos - 2 });
        }

        //1 left, 2 down
        if (xPos - 1 > -1 && yPos - 2 > -1)
        {
            allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - 1, yPos - 2 });
        }

        //2 left, 1 down
        if (xPos - 2 > -1 && yPos - 1 > -1)
        {
            allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - 2, yPos - 1 });
        }

        //2 left, 1 up
        if (xPos - 2 > -1 && yPos + 1 < 8)
        {
            allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - 2, yPos + 1 });
        }

        //1 left, 2 up
        if (xPos - 1 > -1 && yPos + 2 < 8)
        {
            allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - 1, yPos + 2 });
        }
    }

    //lähettien siirtojen tarkistus
    private void AddAvailableMovesForBishops(int xPos, int yPos)
    {
        //move right & up
        for (int move = 1; move < 8; move++)
        {
            if (xPos == 7 || yPos == 7)
                break;

            if (xPos + move == 8 || yPos + move == 8)
                break;

            //next is empty square, add it and continue
            if (board[xPos + move, yPos + move] == "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + move, yPos + move });
                continue;
            }

            //next is not empty, add it, then break
            if (board[xPos + move, yPos + move] != "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + move, yPos + move });

                //check if there's a square after that and add it as anticipatory
                if (xPos + move + 1 == 8 || yPos + move + 1 == 8)
                {
                    allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + move + 1, yPos + move + 1});
                }
                break;
            }
        }

        //move right & down
        for (int move = 1; move < 8; move++)
        {
            if (xPos == 7 || yPos == 0)
                break;

            if (xPos + move == 8 || yPos - move == -1)
                break;

            //next is empty square, add it and continue
            if (board[xPos + move, yPos - move] == "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + move, yPos - move });
                continue;
            }

            //next is not empty, add it, then break
            if (board[xPos + move, yPos - move] != "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + move, yPos - move });

                //check if there's a square after that and add it as anticipatory
                if (xPos + move + 1 == 8 || yPos - move - 1 == -1)
                {
                    allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + move + 1, yPos - move - 1});
                }
                break;
            }
        }

        //move left & down
        for (int move = 1; move < 8; move++)
        {
            if (xPos == 0 || yPos == 0)
                break;

            if (xPos - move == -1 || yPos - move == -1)
                break;

            //next is empty square, add it and continue
            if (board[xPos - move, yPos - move] == "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - move, yPos - move });
                continue;
            }

            //next is not empty, add it, then break
            if (board[xPos - move, yPos - move] != "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - move, yPos - move });

                //check if there's a square after that and add it as anticipatory
                if (xPos - move - 1 == -1 || yPos - move - 1 == -1)
                {
                    allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - move - 1, yPos - move - 1});
                }
                break;
            }
        }

        //move left & up
        for (int move = 1; move < 8; move++)
        {
            if (xPos == 0 || yPos == 7)
                break;

            if (xPos - move == -1 || yPos + move == 8)
                break;

            //next is empty square, add it and continue
            if (board[xPos - move, yPos + move] == "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - move, yPos + move });
                continue;
            }

            //next is not empty, add it, then break
            if (board[xPos - move, yPos + move] != "")
            {
                allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - move, yPos + move });

                //check if there's a square after that and add it as anticipatory
                if (xPos - move - 1 == -1 || yPos + move + 1 == 8)
                {
                    allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - move - 1, yPos + move + 1});
                }
                break;
            }
        }

    }

    //kunkkujen siirtojen tarkistus
    private void AddAvailableMovesForKings(int xPos, int yPos)
    {
        //up
        if (yPos != 7)
        {
            allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos, yPos + 1});
        }

        //right & up
        if (xPos != 7 && yPos != 7)
        {
            allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + 1, yPos + 1 });
        }

        //right
        if (xPos != 7)
        {
            allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + 1, yPos });
        }

        //right & down
        if (xPos != 7 && yPos != 0)
        {
            allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos + 1, yPos - 1});
        }

        //down
        if (yPos != 0)
        {
            allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos, yPos - 1 });
        }

        //left & down
        if (xPos != 0 && yPos != 0)
        {
            allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - 1, yPos - 1 });
        }

        //left
        if (xPos != 0)
        {
            allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - 1, yPos});
        }

        //left & up
        if (xPos != 0 && yPos != 7)
        {
            allAvailableMoves.Add(new List<int>() { xPos, yPos, xPos - 1, yPos + 1});
        }
    }

    //funktio jolla valitaan seuraava liike. tarvitseeko molemmat pelaajat oman funktion.
    public void SelectMove(int fromX, int fromY, int toX, int toY, string promotedPiece)
    {
        if (board[fromX, fromY].Substring(0, 5) == "white")
        {
            whiteMove = new List<int>() { fromX, fromY, toX, toY };

            promotedWhitePiece = promotedPiece;
        }
        else
        {
            blackMove = new List<int>() { fromX, fromY, toX, toY };

            promotedBlackPiece = promotedPiece;
        }
    }

    public void CancelMove(string color)
    {
        if (color == "white")
        {
            whiteMove = null;
            promotedWhitePiece = "";
        }
        else if (color == "black")
        {
            blackMove = null;
            promotedBlackPiece = "";
        }
    }

    private void ResolveMoves()
    {
        string whitePieceMoving = "";
        string blackPieceMoving = "";
        string whiteUnitMoving = "";
        string blackUnitMoving = "";
        int whitePieceValue = 0;
        int blackPieceValue = 0;
        string capturedWhiteUnit;
        string capturedBlackUnit;
        int valueOfCapturedWhitePiece = 0;
        int valueOfCapturedBlackPiece = 0;
        int whiteFromX = 0;
        int whiteFromY = 0;
        int whiteToX = 0;
        int whiteToY = 0;
        int blackFromX = 0;
        int blackFromY = 0;
        int blackToX = 0;
        int blackToY = 0;
        string capturedWhitePiece = "";
        string capturedBlackPiece = "";

        if (whiteMove != null)
        {
            whiteFromX = whiteMove[0];
            whiteFromY = whiteMove[1];
            whiteToX = whiteMove[2];
            whiteToY = whiteMove[3];

            whitePieceMoving = board[whiteFromX, whiteFromY];
            whiteUnitMoving = whitePieceMoving.Substring(6, whitePieceMoving.Length - 6);
            whitePieceValue = pieceCollisionStregth[whiteUnitMoving];
        }

        if (blackMove != null)
        {
            blackFromX = blackMove[0];
            blackFromY = blackMove[1];
            blackToX = blackMove[2];
            blackToY = blackMove[3];

            blackPieceMoving = board[blackFromX, blackFromY];
            blackUnitMoving = blackPieceMoving.Substring(6, blackPieceMoving.Length - 6);
            blackPieceValue = pieceCollisionStregth[blackUnitMoving];
        }

        //anticipatory moves
        if (whiteMove != null)
        {
            if (whitePieceMoving == "pawn")
            {
                //ollaan vielä aloitusruudussa
                if (whiteFromY == 1)
                {


                }
                else // ei olla aloitusruudussa
                {
                    if (board[whiteToX, whiteToY].Substring(0, 5) == "white")
                    {

                    }
                }

            }

        }


        //clear the square white is moving from
        board[whiteFromX, whiteFromY] = "";
        //clear the square black is moving from
        board[blackFromX, blackFromY] = "";

        //both have moved
        if (whiteMove != null && blackMove != null)
        {
            //both are moving to the same square
            if (whiteToX == blackToX && whiteToY == blackToY)
            {
                //same units are moving to the same square, the move is voided and removed from available moves for next turn
                if (whitePieceValue == blackPieceValue)
                {
                    allAvailableMoves.Remove(whiteMove);
                    allAvailableMoves.Remove(blackMove);
                    //currentState = GameState.TURN_STARTED;
                }
                //white captures black when they try to move to same square because white piece value > black piece value
                else if (whitePieceValue > blackPieceValue)
                {
                    capturedBlackPiece = blackPieceMoving;
                    capturedBlackPieces.Add(capturedBlackPiece);
                    board[whiteToX, whiteToY] = whitePieceMoving;
                }
                //black captures white when they try to move to same square because balck piece value > white piece value
                else if (blackPieceValue > whitePieceValue)
                {
                    capturedWhitePiece = whitePieceMoving;
                    capturedWhitePieces.Add(capturedWhitePiece);
                    board[blackToX, blackToY] = blackPieceMoving;
                }
            }
            //they are not moving to the same square, but both are moving
            else
            {
                //white captures a non moving black
                if (board[whiteToX, whiteToY] != "")
                {
                    capturedBlackPiece = board[whiteToX, whiteToY];
                    capturedBlackPieces.Add(capturedBlackPiece);
                    board[whiteToX, whiteToY] = whitePieceMoving;
                }
                //white moves to an empty square
                else
                {
                    board[whiteToX, whiteToY] = whitePieceMoving;
                }

                //black captures a non moving white
                if (board[blackToX, blackToY] != "")
                {
                    capturedWhitePiece = board[blackToX, blackToY];
                    capturedWhitePieces.Add(capturedWhitePiece);
                    board[blackToX, blackToY] = blackPieceMoving;
                }
                //black moves to an empty square
                else
                {
                    board[blackToX, blackToY] = blackPieceMoving; ;
                }
            }
        }
        //only white is moving
        else if (whiteMove != null)
        {
            //white captures a non moving black
            if (board[whiteToX, whiteToY] != "")
            {
                capturedBlackPiece = board[whiteToX, whiteToY];
                capturedBlackPieces.Add(capturedBlackPiece);
                board[whiteToX, whiteToY] = whitePieceMoving;
            }
            //white moves to an empty square
            else
            {
                board[whiteToX, whiteToY] = whitePieceMoving;
            }

        }
        else if (blackMove != null)
        {
            //black captures a non moving white
            if (board[blackToX, blackToY] != "")
            {
                capturedWhitePiece = board[blackToX, blackToY];
                capturedWhitePieces.Add(capturedWhitePiece);
                board[blackToX, blackToY] = blackPieceMoving;
            }
            //black moves to an empty square
            else
            {
                board[blackToX, blackToY] = blackPieceMoving;
            }
        }

        //check points and possible win
        if (capturedWhitePiece != "")
        {
            capturedWhiteUnit = capturedWhitePiece.Substring(6, capturedWhitePiece.Length - 6);
            valueOfCapturedWhitePiece = pieceCollisionStregth[capturedWhiteUnit];
        }

        if (capturedBlackPiece != "")
        {
            capturedBlackUnit = capturedBlackPiece.Substring(6, capturedBlackPiece.Length - 6);
            valueOfCapturedBlackPiece = pieceCollisionStregth[capturedBlackUnit];
        }
        
            blackPoints += valueOfCapturedWhitePiece;
            whitePoints += valueOfCapturedBlackPiece;

    }

}
