using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesoloCompiler
{
    public record struct Pointer(int PointerV, int PointerValueArray, bool Char)
    {
        public override string ToString()
        {
            string ToModify = "";
            switch (PointerValueArray)
            {
                case 0:
                    ToModify = PointerV.ToString(); break;
                case 1:
                    ToModify = "p" + PointerV.ToString(); break;
                case 2:
                    ToModify = "a" + PointerV.ToString(); break;
                case 3:
                    ToModify = "f" + PointerV.ToString(); break;
                case 4:
                    ToModify = "fa" + PointerV.ToString(); break;
                default:
                    throw new Exception("Invalid pointer type.");
            }
            if (Char)
            {
                return ToModify + "\"";
            }
            else
            {
                return ToModify;
            }
        }
    }
}
