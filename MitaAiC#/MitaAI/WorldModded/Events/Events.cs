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
        static float lastEventTime = 0f;
        static float timeBeetweenEvents = 30f;

        public static void regEvent()
        {
            lastEventTime = Time.unscaledTime;
        }


        public static void HandleAnimationEvent(UnityEngine.AnimationEvent evt)
        {
            switch (evt.stringParameter)
            {
                case "TakePlayer":
                    MelonCoroutines.Start(playerTaken());
                    break;
                case "Mita Kick":
                    MelonCoroutines.Start(MitaKickEnd());
                    break;
                case "Mita Click":
                    AudioControl.playFingerClick();
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
        static IEnumerator MitaKickEnd()
        {
            MelonCoroutines.Start( MitaCore.Instance.AddRemoveBloodEffect(5));
            yield return new WaitForSeconds(1.5667f);
            
            MitaAnimationModded.bat.active = false;
        }

        public static void HandleCustomEvent(string eventName)
        {
            MelonLogger.Msg($"HandleCustomEvent called with event: {eventName}");

            if (eventName.StartsWith("MENU|")){
                UINeuroMita.MenuEventsCases(eventName);
            }


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


        #region EventsEnter
        // Место, где в теории смогу уловить все, что будет вызывать генерацию реакции миты

        static Dictionary<string, float> eventRepeatBlock = new Dictionary<string, float>();

        static bool TimeBlock(string name, float needed = 8)
        {
            float currentTime = Time.unscaledTime;

            if (currentTime - lastEventTime < timeBeetweenEvents)
            {
                return true;
            }


            // Если ключ существует и время еще не прошло
            if (eventRepeatBlock.TryGetValue(name, out float lastTime) && currentTime - lastTime < needed)
            {
                return true; // Блокируем выполнение
            }

            // Обновляем время последнего вызова
            eventRepeatBlock[name] = currentTime;
            lastEventTime = currentTime;
            return false; // Не блокируем выполнение
        }

        // Если долго на что-то смотрит
        public static void LongWatching(string objectName,float time)
        {
            if (TimeBlock("LongWatching", 40f)) return;
            MelonLogger.Msg("Event LongWatching");


            MitaCore.Instance.sendSystem($"Игрок на протяжении {time} секунд смотрел на {objectName}",false);

        }

        #region Position
        // Отошел от Миты
        public static void leaveMita()
        {


        }
        // Отошел от Миты
        public static void enterMita()
        {


        }

        // Зашел в комнату
        public static void roomEnter(MitaCore.Rooms room, MitaCore.Rooms lastRoom)
        {
            MelonLogger.Msg($"Event roomEnter {room}");

            if (room == MitaCore.Rooms.Unknown) return;

            bool isInfo = TimeBlock("roomEnter", 15f);

            MitaCore.Rooms mitaRoom = MitaCore.Instance.GetRoomID(MitaCore.Instance.MitaPersonObject.transform);
            string message = $"Игрок только что зашел в {room} из {lastRoom}, а ты сейчас находишься в {mitaRoom}. " +
                $"Реагируй резко, только если это обоснованно, в общем случае можешь это не заметить или продолжить по теме разговора";
            
            //if (mitaRoom != room) message += $"";


            MitaCore.Instance.sendSystem(message, isInfo);

        }


        #endregion

        // Долго ничего не делал
        public static void boringTime()
        {

        }

        // Загрузился
        public static void sceneEnter(MitaCore.character character)
        {
            string HelloMessage = "Игрок только что загрузился в твой уровень";

            if (Utils.Random(1, 4)) HelloMessage += ", подбери музыку для его встречи";
            if (Utils.Random(1, 7)) HelloMessage += ", можешь удивить его новым костюмом";
            if (Utils.Random(1, 7)) HelloMessage += ", можешь удивить его новым цветом волос";


            MitaCore.Instance.sendSystemMessage(HelloMessage,character);
        }





        #endregion



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
