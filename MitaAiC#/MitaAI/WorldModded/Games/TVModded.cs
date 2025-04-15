using Il2Cpp;
using Il2CppSteamworks;
using MelonLoader;
using MitaAI.Mita;
using System;
using System.Collections;
using System.ComponentModel;
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
        static bool KeysActive = false;
        private static MinigamesTelevisionGame minigamesTelevisionGame;
        private static MT_GameCnowballs mT_GameCnowballs;
        private static MT_GameFly mT_GameFly;
        public static void SetTVController()
        {
            minigamesTelevisionController = MitaCore.worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/TV/GameTelevision").GetComponent<MinigamesTelevisionController>();
            minigamesTelevisionController.CanTalkAboutGame(false);
            minigamesTelevisionController.destroyAfter = false;

            var Menu = MitaCore.worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/TV/Interface Hint/Menu");
            
            
            var ExitEvent = Menu.transform.Find("Exit").GetComponent<Interface_KeyHint_Key>().eventKeyDown;
            ExitEvent.RemoveAllListeners();

            ExitEvent.AddListener((UnityAction)TurnControlKeys);
            //minigamesTelevisionController.keysMenu

            minigamesTelevisionController.interfaceKeys.Find("Menu/Change").GetComponent<Interface_KeyHint_Key>().eventKeyDown.AddListener((UnityAction)minigamesTelevisionController.PlayGame);
        }

        public static void turnTV()
        {

            if (!minigamesTelevisionController.activation)
            {
                minigamesTelevisionController.StartTelevision();
                //MelonCoroutines.Start(startKeysMenu());
            }
            else minigamesTelevisionController.StopTelevision();

        }
        public static void TurnControlKeys()
        {
            KeysActive = !KeysActive;

            minigamesTelevisionController.KeysMenuActive(KeysActive);

            
        }

        static IEnumerator startKeysMenu()
        {
            yield return new WaitForSeconds(1f);
            TurnControlKeys();
        }

        static IEnumerator startGame()
        {
            yield return new WaitForSeconds(1f);
            TurnControlKeys();
            yield return new WaitForSeconds(1f);
            minigamesTelevisionController.PlayGame();

            minigamesTelevisionGame = UnityEngine.Object.FindObjectOfType<MinigamesTelevisionGame>();
            while (minigamesTelevisionGame == null)
            {
                minigamesTelevisionGame = UnityEngine.Object.FindObjectOfType<MinigamesTelevisionGame>();
                yield return new WaitForSeconds(1);
            }




        }

 

    }

    //[HarmonyLib.HarmonyPatch]
    //public static class MinigamesTelevisionControllerModdded
    //{


    //    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.MinigamesTelevisionController), "Start")]
    //    [HarmonyLib.HarmonyPostfix]
    //    private static void Postfix2()
    //    {
    //        TVModded.minigamesTelevisionController.PlayGame();

    //    }
    //}
}
