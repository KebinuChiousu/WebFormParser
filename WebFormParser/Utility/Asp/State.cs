using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace WebFormParser.Utility.Asp
{
    public class State
    {
        public bool IsTag { get; set; }
        public bool IsOpen { get; set; }
        public bool IsCode { get; set; }
        public bool PrevCode { get; set; }
        public int OpenCode { get; set; }
        public int CloseCode { get; set; }
        public int FuncCount { get; set; }
        public bool IsComment { get; set; }
        public bool IsScript { get; set; }

        [System.Diagnostics.DebuggerStepThrough]
        public State()
        {
            IsTag = false;
            IsOpen = false;
            IsCode = false;

            OpenCode = 0;
            CloseCode = 0;
        }

        private void Reset()
        {
            OpenCode = 0;
            CloseCode = 0;
            IsCode = false;
        }

        public void HandleCodeState(string value)
        {
            if (!value.Contains("%>"))
                return;
            
            if (OpenCode != CloseCode)
                return;

            Reset();
        }
    }
}
