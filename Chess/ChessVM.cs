using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
    class ChessVM
    {
        public bool WhiteTurn = true, gameDone = false;
        public Piece[,] Playfield = new Piece[8, 8];
        public Piece[,] WhiteTaken = new Piece[8, 2];
        public Piece[,] BlackTaken = new Piece[8, 2];
        Piece selectedPiece = new Piece();
        public int[] selectedIndeces, whiteTakenIndeces = { 0, 0 }, blackTakenIndeces = { 0, 0 };
        const string pawn = "Pawn", rook = "Rook", knight = "Knight", bishop = "Bishop", queen = "Queen", king = "King", white = "White", black = "Black", empty = "empty";
        public List<int[]> Highlight = new List<int[]>();

        //function classes
        UtilityFunctions uf = new UtilityFunctions();
        DisplayFunctions df = DisplayFunctions.GetInstance();
        MovementFunctions mf = new MovementFunctions();
        MoveHighlightFunctions mhf = new MoveHighlightFunctions();
        ThreatenHighlightFunctions thf = new ThreatenHighlightFunctions();
        CheckFunctions cf = new CheckFunctions();
        CheckDisplayFunctions cdf = new CheckDisplayFunctions();

        public void InitializePlayfield()
        {
            uf.InitializePlayfield(ref WhiteTurn, ref gameDone, Highlight, Playfield, WhiteTaken, BlackTaken, ref whiteTakenIndeces, ref blackTakenIndeces);
        }

        public void SelectOrMove(int row, int col, List<int[]> moveable, List<int[]> threatened, List<int[]> enPassant, ref bool promote, ref bool moved)
        {
            bool doSelect = true;
            //enforce turn order for move
            if ((WhiteTurn && selectedPiece.Colour == white) || (!WhiteTurn && selectedPiece.Colour == black))
                foreach (int[] pair in Highlight)
                    if (pair[0] == row && pair[1] == col)
                    {
                        move(row, col, ref promote, ref doSelect, ref moved);
                        break;
                    }
            if (doSelect)
                select(row, col, moveable, threatened, enPassant);
        }

        void select(int row, int col, List<int[]> moveable, List<int[]> threatened, List<int[]> enPassant)
        {
            Highlight.Clear();
            {
                //enforce turn order for select
                if ((WhiteTurn && Playfield[row, col].Colour == white) || (!WhiteTurn && Playfield[row, col].Colour == black))
                {
                    selectedPiece = Playfield[row, col];
                    selectedIndeces = new int[2] { row, col };
                    mhf.highlightMoveable(Playfield[row, col], Playfield, row, col, moveable);
                    thf.highlightThreatened(Playfield[row, col], row, col, threatened, enPassant, Playfield);
                    foreach (int[] pair in moveable)
                        if (!cf.createCheck(row, col, pair[0], pair[1], Playfield))
                            Highlight.AddRange(moveable);
                    foreach (int[] pair in threatened)
                        if (Playfield[pair[0], pair[1]].Exists && !cf.createCheck(row, col, pair[0], pair[1], Playfield))
                            Highlight.Add(pair);
                    foreach (int[] pair in enPassant)
                        if (!cf.createCheck(row, col, pair[0], pair[1], Playfield))
                            Highlight.AddRange(enPassant);
                }
            }
        }

        void move(int row, int col, ref bool promote, ref bool doSelect, ref bool moved)
        {
            Highlight.Clear();
            if (Playfield[selectedIndeces[0], selectedIndeces[1]].Type == king && Math.Abs(selectedIndeces[1] - col) == 2)
                mf.castle(col, Playfield, selectedIndeces);
            if (Playfield[selectedIndeces[0], selectedIndeces[1]].Type == pawn && (Playfield[3, col].EnPassant || Playfield[4, col].EnPassant))
                mf.performEnPassant(row, col, Playfield);
            //castling and En Passant can't create an opportunity for en passant, so we can remove all the en passant flags here
            for (int rows = 0; rows < 8; rows++)
            {
                for (int cols = 0; cols < 8; cols++)
                    Playfield[rows, cols].EnPassant = false;
            }
            //move piece happens after the en passant flag clearing because it can raise en passant flags
            mf.movePiece(selectedPiece, Playfield, WhiteTaken, BlackTaken, whiteTakenIndeces, blackTakenIndeces, selectedIndeces, row, col);
            moved = true;
            cdf.checkForCheck(Playfield, selectedPiece.Colour);
            cdf.checkForCheckmate(Playfield, selectedPiece.Colour, gameDone);

            if (selectedPiece.Type == pawn && (row == 0 || row == 7))
                promote = true;
            //switch turns
            WhiteTurn = !WhiteTurn;
            doSelect = false;
        }
    }
}
