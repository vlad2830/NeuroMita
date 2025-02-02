using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MitaAI
{
    public static class TotalInitialization
    {


        public static void initTVGames(Transform world)
        {
            GameObject pult = MitaCore.TryfindChild(world, "World/Quests/Quest 1/Addon/Interactive Aihastion");
            GameObject pultCopy= GameObject.Instantiate(pult, pult.transform.parent);
            pultCopy.GetComponent<ObjectInteractive>().active = true;

            GameObject GameAihastion = MitaCore.TryfindChild(world, "World/Quests/Quest 1/Game Aihastion");
        }
        

    }
}
