using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
    class AvailableMove
    {
        public Piece Piece;
        public int[] Move;
        double rating = 0;
        public double Rating
        {
            get { return rating; }
            set { rating = value; }
        }
    }


}
