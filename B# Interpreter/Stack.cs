using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DesoloCompiler
{
    public class Stack
    {
        List<StackPart> Main;
        public Stack()
        {
            Main = new List<StackPart>();
        }
        public void Push(StackPart ToPush)
        {
            Main.Add(ToPush);
        }
        public StackPart ReadTop()
        {
            return Main[Main.Count - 1];
        }
        public StackPart Pop()
        {
            StackPart ToReturn = ReadTop();
            Main.RemoveAt(Main.Count - 1);
            return ToReturn;
        }
    }
    public record StackPart(List<int> PassedOn, int ReturnLine)
    {
        public override string ToString()
        {
            string Totality = "";
            for (int i = 0; i < PassedOn.Count; i++)
            {
                Totality += "f" + i + "=" + PassedOn[i].ToString();
                Totality += ", ";
            }
            Totality += "rl=" + ReturnLine;
            return Totality;
        }
    }
    public class OrderStack
    {
        List<CType> Main;
        public OrderStack()
        {
            Main = new List<CType>();
        }
        public void Push(CType ToPush)
        {
            Main.Add(ToPush);
        }
        public CType ReadTop()
        {
            return Main[^1];
        }
        public CType Pop()
        {
            CType ToReturn = ReadTop();
            Main.RemoveAt(Main.Count - 1);
            return ToReturn;
        }
        public int Size => Main.Count;
    }
}
