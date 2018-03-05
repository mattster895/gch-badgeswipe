using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadgeSwipeApp
{
    public static class GlobalVar
    {
        static int _startValue;
        static int _SwipeNum;

        public static int StartValue
        {
            get
            {
                return _startValue;
            }
            set
            {
                _startValue = value;
            }
        }

        public static int SwipeNum
        {
            get
            {
                return _SwipeNum;
            }
            set
            {
                _SwipeNum = value;
            }
        }
        

    }
}
