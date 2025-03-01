using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using MelonLoader;
using UnityEngine;
using Il2Cpp;
namespace MitaAI.WorldModded
{
    public static class InteractionCases
    {
        public static void caseConsoleStart(GameObject console)
        {
            if (console.GetComponent<ObjectInteractive>().active)
            {
                console.GetComponent<ObjectInteractive>().active = false;
                MelonCoroutines.Start(caseConsoleAct(console));
            }

        }

        public static void sofaStart(GameObject gameObject)
        {
            if (Utils.getDistanceBetweenObjects(gameObject, MitaCore.Instance.playerObject) > 1.5f) return;
            MelonLogger.Msg($"SofaSit");
            gameObject.GetComponent<ObjectInteractive>().active = false;

            if (PlayerAnimationModded.currentPlayerMovement != PlayerAnimationModded.PlayerMovement.sit)
            {
                PlayerAnimationModded.currentPlayerMovement = PlayerAnimationModded.PlayerMovement.sit;
                PlayerAnimationModded.playAnimObject(gameObject);
            }         
           
        }


        public static IEnumerator caseConsoleAct(GameObject console)
        {
            try
            {
                MitaCore.Instance.playerObject.transform.Find("LeftItem FixPosition/Console/ConsoleCup/ScreenWork").gameObject.active = false;
            }
            catch (Exception e)
            {

                MelonLogger.Error(e);
            }
            
            
            yield return new WaitForSeconds(1.5f);

            MitaCore.Instance.sendSystemMessage("Твою консоль только что открыли.", Characters.get_cart() );

            var Button1 = Utils.TryfindChild(MitaCore.worldBasement, "Quests/Quest1 Start/3D HintKey FindButton");
            var Button2 = Utils.TryfindChild(MitaCore.worldBasement, "Quests/Quest1 Start/3D HintKey Holding");

            var Button1_res = GameObject.Instantiate(Button1);
            var Button2_res = GameObject.Instantiate(Button2);

            Button1_res.active = false; Button2_res.active = false;


            Button1?.SetActive(true);

            while (Button1!=null)
            {
                yield return new WaitForSeconds(0.25f);
            }
            MitaCore.Instance.sendSystemMessage("Игрок ищет кнопку выключения консоли", Characters.get_cart());
            
            while (Button2 != null)
            {
                yield return new WaitForSeconds(0.25f);
            }
            
            if (MitaCore.Instance.getDistanceToPlayer()<6f) MitaCore.Instance.sendSystemMessage("Игрок при тебе просмотрел твой картридж другого игрока в подвале. У него могут быть вопросы, надо как-то уйти от темы. Если он будет наставить, придется сбросить маску.");
            else MitaCore.Instance.sendSystemInfo("Игрок просмотрел твой картридж другого игрока в подвале пока ты была не рядом. У него могут быть вопросы, если он спросит надо как-то уйти от темы. Если он будет наставить, придется сбросить маску.");
            MitaCore.Instance.sendSystemInfo("Игрок выключил тебя", Characters.get_cart());

            yield return new WaitForSeconds(1f);
            console.GetComponent<ObjectInteractive>().active = true;

            Button1.transform.SetParent(MitaCore.worldBasement.Find("Quests/Quest1 Start"));
            Button2_res.transform.SetParent(MitaCore.worldBasement.Find("Quests/Quest1 Start"));
            Button1_res.name = "3D HintKey FindButton";
            Button2_res.name = "3D HintKey Holding";

            Transform consoleParent = console.transform.parent;

            GameObject console_res = consoleParent.Find(console.name + "_res").gameObject;
            GameObject.Destroy(console);
            console_res.active = true;


        }

    }
}
