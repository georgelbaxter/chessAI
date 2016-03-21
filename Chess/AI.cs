using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* Feb 22
 * initialized move rating
 * AI is now functional if a bit brute force, takes a noticeable amount of time to make a move now
 * todo: prune branches and add more to rating boards, fix check notifications
 * Feb 12
 * There is no tree structure in C#, made one to try to keep track of moves
 * nodes should get re-rated based on their children
 * Feb 9
 * trying to add more thinking to the AI
 * get board, find pieces on board, find moves from pieces (based on turn), create theoretical boards
 * need to count depth
 * rewriting functions to do less
 * Feb 8
 * AI can make moves randomly
 * AI now prioritizes taking pieces over everything
 */

namespace Chess
{
    class AI
    {
        const string pawn = "Pawn", rook = "Rook", knight = "Knight", bishop = "Bishop", queen = "Queen", king = "King", white = "White", black = "Black", empty = "empty";
        string currentColour = black;
        //layers of moves to go through, 0 is random, 1 is just the immediate move, 2 is black then white, etc... (hopefully)
        int depthOfThought = 3;
        List<Piece> pieceList = new List<Piece>();
        List<AvailableMove> allMovesList = new List<AvailableMove>();
        List<Piece[,]> theoreticalBoards = new List<Piece[,]>();
        MoveNode topNode;
        //other classes
        Random rand = new Random();
        MoveHighlightFunctions mhf = new MoveHighlightFunctions();
        ThreatenHighlightFunctions thf = new ThreatenHighlightFunctions();
        CheckFunctions cf = new CheckFunctions();
        MovementFunctions mf = new MovementFunctions();
        UtilityFunctions uf = new UtilityFunctions();
        DisplayFunctions df = DisplayFunctions.GetInstance();
        Dictionary<string, double> pieceWeighting = new Dictionary<string, double>();

        public AI()
        {
            pieceWeighting.Add(pawn, 1);
            pieceWeighting.Add(rook, 5);
            pieceWeighting.Add(knight, 3);
            pieceWeighting.Add(bishop, 3);
            pieceWeighting.Add(queen, 9);
            pieceWeighting.Add(king, 1000);
        }
        public void TakeTurn(Piece[,] board, Piece[,] WhiteTaken, Piece[,] BlackTaken, int[] whiteTakenIndeces, int[] blackTakenIndeces, int depth)
        {
            topNode = new MoveNode(board);
            think(depth, topNode);
            bool mate = false;
            AvailableMove move = selectMove(allMovesList, board, ref mate, currentColour);
            if (!mate)
                makeMove(move, board, WhiteTaken, BlackTaken, whiteTakenIndeces, blackTakenIndeces);
        }

        private void think(int depth, MoveNode currentNode)
        {
            //get piece list for whoever is moving
            if (depth % 2 == 0)
                updatePieceList(pieceList, currentNode.Board, currentColour);
            else
                updatePieceList(pieceList, currentNode.Board, uf.OtherColour(currentColour));
            //get list of moveable pieces
            updateMoveList(pieceList, currentNode.Board);
            //make each move and add it to the tree
            foreach (AvailableMove move in allMovesList)
            {
                Piece[,] tmpBoard = makeTheoreticalMove(move, currentNode.Board);
                move.Rating = evaluateBoard(tmpBoard, currentColour);
                currentNode.AddChildren(currentNode, move, tmpBoard);
            }
            if (depth < depthOfThought)
                foreach (MoveNode nextNode in currentNode.Children)
                {
                    think(depth + 1, nextNode);
                    currentNode.Move.Rating = evaluateChildren(currentNode, depth);
                }
            if (depth == 0)
            {
                allMovesList.Clear();
                foreach (MoveNode child in currentNode.Children)
                    allMovesList.Add(child.Move);
            }
        }

        private double evaluateChildren(MoveNode currentNode, int depth)
        {
            List<double> ratings = new List<double>();
            foreach (MoveNode child in currentNode.Children)
                ratings.Add(child.Move.Rating);
            if (depth % 2 == 1)
                return ratings.Min();
            else
                return ratings.Max();
        }

        private void makeMove(AvailableMove move, Piece[,] board, Piece[,] WhiteTaken, Piece[,] BlackTaken, int[] whiteTakenIndeces, int[] blackTakenIndeces)
        {
            mf.movePiece(move.Piece, board, WhiteTaken, BlackTaken, whiteTakenIndeces, blackTakenIndeces, new int[] { move.Piece.Row, move.Piece.Col }, move.Move[0], move.Move[1]);
        }

        private AvailableMove selectMove(List<AvailableMove> allMovesList, Piece[,] board, ref bool mate, string colour)
        {
            AvailableMove move = new AvailableMove();
            if (allMovesList.Count > 0)
            {
                var maxRating = allMovesList.Max(x => x.Rating);
                move = allMovesList.Where(x => x.Rating == maxRating).First();
            }
            else if (cf.isThreatened(uf.FindKing(colour, board), uf.OtherColour(colour), board))
            {
                df.Checkmate(colour);
                mate = true;
            }
            else
            {
                df.Stalemate();
                mate = true;
            }
            return move;
        }

        private Piece[,] makeTheoreticalMove(AvailableMove move, Piece[,] board)
        {
            //clone board
            Piece[,] tmpBoard = new Piece[8, 8];
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                    tmpBoard[row, col] = new Piece(board[row, col]);
            //make theoretical move
            tmpBoard[move.Move[0], move.Move[1]] = new Piece(move.Piece);
            tmpBoard[tmpBoard[move.Move[0], move.Move[1]].Row, tmpBoard[move.Move[0], move.Move[1]].Col] = new Piece();
            tmpBoard[move.Move[0], move.Move[1]].Row = move.Move[0];
            tmpBoard[move.Move[0], move.Move[1]].Col = move.Move[1];
            return tmpBoard;
        }

        private double evaluateBoard(Piece[,] board, string colour)
        {
            //rate move
            double rating = 0;
            //count pieces
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                {
                    if (board[row, col].Colour == colour)
                        rating += pieceWeighting[board[row, col].Type] + rand.NextDouble() / 1000;
                    if (board[row, col].Colour == uf.OtherColour(colour))
                        rating -= pieceWeighting[board[row, col].Type] * 1.1 + rand.NextDouble() / 1000;
                }
            //advancement (encourage computer to push forwards)
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                {
                    if (board[row, col].Colour == white)
                        rating += (7 - row) / 7;
                    if (board[row, col].Colour == black)
                        rating -= (row - 7) / 7;
                }
            //pawn structure (encourage protecting pawns with pawns)
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                {
                    if (board[row, col].Colour == white && board[row, col].Type == pawn &&
                        (uf.IsInBounds(row - 1, col - 1) && board[row - 1, col - 1].Colour == white && board[row - 1, col - 1].Type == pawn) ||
                        (uf.IsInBounds(row - 1, col + 1) && board[row - 1, col + 1].Colour == white && board[row - 1, col + 1].Type == pawn))
                    {
                        if (colour == white)
                            rating += 0.5;
                        else
                            rating -= 0.5;
                    }
                    if (board[row, col].Colour == black && board[row, col].Type == pawn &&
                        (uf.IsInBounds(row + 1, col - 1) && board[row + 1, col - 1].Colour == white && board[row + 1, col - 1].Type == pawn) ||
                        (uf.IsInBounds(row + 1, col + 1) && board[row + 1, col + 1].Colour == white && board[row + 1, col + 1].Type == pawn))
                    {
                        if (colour == black)
                            rating += 0.5;
                        else
                            rating -= 0.5;
                    }
                }
            return rating;
        }

        private void updateMoveList(List<Piece> pieces, Piece[,] board)
        {
            allMovesList.Clear();
            List<int[]> moveable = new List<int[]>();
            List<int[]> threatList = new List<int[]>();
            List<int[]> enPassantList = new List<int[]>();
            foreach (Piece piece in pieces)
            {
                moveable.Clear();
                threatList.Clear();
                enPassantList.Clear();
                mhf.highlightMoveable(board[piece.Row, piece.Col], board, piece.Row, piece.Col, moveable);
                thf.highlightThreatened(board[piece.Row, piece.Col], piece.Row, piece.Col, threatList, enPassantList, board);
                foreach (int[] pair in moveable)
                    if (!cf.createCheck(piece.Row, piece.Col, pair[0], pair[1], board))
                        allMovesList.Add(new AvailableMove() { Move = pair, Piece = piece });
                foreach (int[] pair in threatList)
                    if (board[pair[0], pair[1]].Exists && !cf.createCheck(piece.Row, piece.Col, pair[0], pair[1], board))
                        allMovesList.Add(new AvailableMove() { Move = pair, Piece = piece });
                foreach (int[] pair in enPassantList)
                    if (!cf.createCheck(piece.Row, piece.Col, pair[0], pair[1], board))
                        allMovesList.Add(new AvailableMove() { Move = pair, Piece = piece });
            }
        }

        private void updatePieceList(List<Piece> pieces, Piece[,] board, string colour)
        {
            pieces.Clear();
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                {
                    if (board[row, col].Colour == colour)
                        pieces.Add(board[row, col]);
                }
        }
    }
}
