using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesoloCompiler
{
    public interface IJumpSpot
    {

    }
    public record JumpString(string Place) : IJumpSpot
    {
        public override string ToString()
        {
            return "Place(" + Place + ")";
        }
    }
    public record JumpSpotProper(int Place) : IJumpSpot
    {
        public override string ToString()
        {
            return "Place(" + Place + ")";
        }
    }
}
