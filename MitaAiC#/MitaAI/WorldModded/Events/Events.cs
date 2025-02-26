using Il2Cpp;
using MelonLoader;
using System;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using MitaAI.Mita;

namespace MitaAI
{
    public static class EventsModded
    {
        public static void HandleAnimationEvent(UnityEngine.AnimationEvent evt)
        {
            switch (evt.stringParameter)
            {
                case "TakePlayer":

                    MelonCoroutines.Start(playerTaken());

                    break;
            }
            MelonLogger.Msg($"AnimationEvent triggered! Time: {evt.time}, String: {evt.stringParameter}, Int: {evt.intParameter}, Float: {evt.floatParameter}");
        }
        static IEnumerator playerTaken()
        {
            MelonLogger.Msg("Player taken!");
            PlayerAnimationModded.currentPlayerMovement = PlayerAnimationModded.PlayerMovement.taken;
            PlayerAnimationModded.playerMove.dontMove = true;
            MitaCore.Instance.playerObject.GetComponent<Rigidbody>().useGravity = false;
            MitaCore.Instance.playerObject.transform.SetParent(MitaCore.Instance.Mita.boneRightItem.transform, true);


            while (PlayerAnimationModded.currentPlayerMovement == PlayerAnimationModded.PlayerMovement.taken)
            {
                float playerSize = MitaCore.Instance.playerObject.transform.localScale.x;
                MitaCore.Instance.playerObject.transform.localPosition = new UnityEngine.Vector3(-0.7f,-1.2f,-0.7f)*playerSize; //-0,7 -1,2 -0,7
                yield return null;
            }
            MitaCore.Instance.playerObject.GetComponent<Rigidbody>().useGravity = true;
            PlayerAnimationModded.playerMove.dontMove = false;
            MitaCore.Instance.playerObject.transform.SetParent(MitaCore.Instance.playerControllerObject.transform);
            MelonLogger.Msg("Player throwned!");
            MitaAnimationModded.resetToIdleAnimation();
        }

        public static void HandleCustomEvent(string eventName)
        {
            MelonLogger.Msg($"HandleCustomEvent called with event: {eventName}");

            switch (eventName)
            {
                case "ConsoleEnd":
                    PlayerAnimationModded.stopAnim();
                    break;
                case "SofaSit":

                    break;
                default:
                    break;
            }
        }


    }




    [HarmonyLib.HarmonyPatch]
    public static class Console
    {

    }

    [HarmonyLib.HarmonyPatch(typeof(Mob_Maneken), "StartKillPlayer")]
    public static class Maneken
    {
        private static void Postfix()
        {
            MelonLogger.Msg("TRYING TRYING");
            if (MitaCore.Instance != null)
            {
                MelonLogger.Msg("MitaCore.Instance is NOT  null.))");
                MitaCore.Instance.playerKilled(); // Вызов метода playerKilled из экземпляра MitaCore
            }
            else
            {
                MelonLogger.Msg("MitaCore.Instance is null.");
            }
        }
    }
    
    [HarmonyLib.HarmonyPatch]
    public static class Safe
    {
        [HarmonyLib.HarmonyPatch(typeof(Basement_Safe), "ClickButton", new Type[] { typeof(int) })]
        [HarmonyLib.HarmonyPostfix]
        private static void Postfix()
        {

            MitaCore.Instance?.playerClickSafe();

        }

        [HarmonyLib.HarmonyPatch(typeof(Basement_Safe), "RightPassword")]
        [HarmonyLib.HarmonyPostfix]
        private static void Postfix2()
        { 
            
            MitaCore.Instance?.sendSystemMessage("Игрок открыл сейф"); 
   
        }
    }
 

}
