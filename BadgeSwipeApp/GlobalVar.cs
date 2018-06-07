using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadgeSwipeApp
{
    public static class GlobalVar
    {
        static int _StartSwipe;
        static int _SwipeNum;
        static int _StartRef;
        static int _RefNum;

        public static int StartSwipe
        {
            get
            {
                return _StartSwipe;
            }
            set
            {
                _StartSwipe = value;
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
        
        public static int StartRef
        {
            get
            {
                return _StartRef;
            }
            set
            {
                _StartRef = value;
            }
        }

        public static int RefNum
        {
            get
            {
                return _RefNum;
            }
            set
            {
                _RefNum = value;
            }
        }

    }
}
