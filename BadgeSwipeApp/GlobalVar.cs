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
        static int _StartSwipe411;
        static int _SwipeNum411;
        static int _StartSwipe416;
        static int _SwipeNum416;
        static int _StartSwipeLaser;
        static int _SwipeNumLaser;
        static int _StartRef;
        static int _RefNum;
        static bool _Debug;
        static bool _SwipeAgent411;
        static bool _SwipeAgent416;
        static bool _SwipeAgentLaser;
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

        public static int StartSwipe411
        {
            get
            {
                return _StartSwipe411;
            }
            set
            {
                _StartSwipe411 = value;
            }
        }

        public static int SwipeNum411
        {
            get
            {
                return _SwipeNum411;
            }
            set
            {
                _SwipeNum411 = value;
            }
        }

        public static int StartSwipe416
        {
            get
            {
                return _StartSwipe416;
            }
            set
            {
                _StartSwipe416 = value;
            }
        }

        public static int SwipeNum416
        {
            get
            {
                return _SwipeNum416;
            }
            set
            {
                _SwipeNum416 = value;
            }
        }

        public static int StartSwipeLaser
        {
            get
            {
                return _StartSwipeLaser;
            }
            set
            {
                _StartSwipeLaser = value;
            }
        }

        public static int SwipeNumLaser
        {
            get
            {
                return _SwipeNumLaser;
            }
            set
            {
                _SwipeNumLaser = value;
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

        public static bool SwipeAgent411
        {
            get
            {
                return _SwipeAgent411;
            }
            set
            {
                _SwipeAgent411 = value;
            }
        }

        public static bool SwipeAgent416
        {
            get
            {
                return _SwipeAgent416;
            }
            set
            {
                _SwipeAgent416 = value;
            }
        }

        public static bool SwipeAgentLaser
        {
            get
            {
                return _SwipeAgentLaser;
            }
            set
            {
                _SwipeAgentLaser = value;
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
