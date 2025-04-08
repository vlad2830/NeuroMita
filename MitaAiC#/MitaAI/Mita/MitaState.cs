using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MitaAI
{
    public enum MitaStateType
    {
        normal,
        hunt,
        interaction

    }


    public class MitaState
    {
        public static MitaStateType currentMitaState = MitaStateType.normal;
        

        public static bool isMovingAvailble()
        {
            return currentMitaState == MitaStateType.normal;
        }

        public static bool isAnimationAvailble()
        {
            return currentMitaState == MitaStateType.normal;
        }
    }
}
