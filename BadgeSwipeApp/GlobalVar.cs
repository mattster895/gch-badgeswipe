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
        static bool _Debug;
        static bool _SwipeAgent;
        static bool _RefAgent;
        static bool _SendFrames;

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

        public static bool Debug
        {
            get
            {
                return _Debug;
            }
            set
            {
                _Debug = value;
            }
        }

        public static bool SwipeAgent
        {
            get
            {
                return _SwipeAgent;
            }
            set
            {
                _SwipeAgent = value;
            }
        }

        public static bool RefAgent
        {
            get
            {
                return _RefAgent;
            }
            set
            {
                _RefAgent = value;
            }
        }
        
        public static bool SendFrames
        {
            get
            {
                return _SendFrames;
            }
            set
            {
                _SendFrames = value;
            }
        }

    }
}
