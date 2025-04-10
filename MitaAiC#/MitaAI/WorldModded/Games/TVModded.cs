using Il2Cpp;
using MelonLoader;
using MitaAI.Mita;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using static MitaAI.EventsModded;


namespace MitaAI
{
    public static class TVModded
    {

        public static MinigamesTelevisionController minigamesTelevisionController;


        public static void SetTVController()
        {
            minigamesTelevisionController = MitaCore.worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/TV/GameTelevision").GetComponent<MinigamesTelevisionController>();
            minigamesTelevisionController.CanTalkAboutGame(false);
            minigamesTelevisionController.destroyAfter = false;

            var Menu = MitaCore.worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/TV/Interface Hint/Menu");
            
            
            var ExitEvent = Menu.transform.Find("Exit").GetComponent<Interface_KeyHint_Key>().eventKeyDown;
            ExitEvent.RemoveAllListeners();
            ExitEvent.AddListener((UnityAction)turnTV);
            //minigamesTelevisionController.keysMenu
        }

        public static void turnTV()
        {
            if (minigamesTelevisionController.activation) minigamesTelevisionController.StartTelevision();
            else minigamesTelevisionController.StopTelevision();

        }



    }
}
