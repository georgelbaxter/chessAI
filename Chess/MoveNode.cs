using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
    class MoveNode
    {
        //the move to get to this board
        public AvailableMove Move { get; set; }
        //theoretical board
        public Piece[,] Board { get; set; }
        //board that this one came from
        public MoveNode Parent { get; set; }
        //boards that can result from this one
        public List<MoveNode> Children { get; set; }

        public MoveNode()
        {
            Move = new AvailableMove();
            Children = new List<MoveNode>();
        }

        public MoveNode(Piece[,] board)
        {
            Move = new AvailableMove();
            Children = new List<MoveNode>();
            Board = board;
        }

        public MoveNode(AvailableMove move, Piece[,] board)
        {
            Children = new List<MoveNode>();
            Move = move;
            Board = board;
        }

        public void AddChildren(MoveNode node, AvailableMove childMove, Piece[,] board)
        {
            MoveNode child = new MoveNode(childMove, board);
            child.Parent = node;
            node.Children.Add(child);
        }
    }
}
