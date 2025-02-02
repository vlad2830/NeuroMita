using Il2Cpp;
using Il2CppSteamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MitaAI
{
    
    public static class PlayerAnimationModded
    {
        static ObjectAnimationPlayer objectAnimationPlayer;
        public static void init(GameObject player)
        {
            objectAnimationPlayer = player.AddComponent<ObjectAnimationPlayer>();
        }


        public static playPlayerAnimation(string animName)
        {

        }



    }
}
