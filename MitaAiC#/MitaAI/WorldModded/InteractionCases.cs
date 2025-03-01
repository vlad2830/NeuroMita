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
            console.GetComponent<ObjectInteractive>().active = false;
            MelonCoroutines.Start(caseConsoleAct(console));
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
            yield return new WaitForSeconds(1.75f);

            var Button1 = Utils.TryfindChild(MitaCore.worldBasement, "Quests/Quest1 Start/3D HintKey FindButton");
            Button1?.SetActive(true);

            while (Button1!=null)
            {
                yield return new WaitForSeconds(0.25f);
            }
            var Button2 = Utils.TryfindChild(MitaCore.worldBasement, "Quests/Quest1 Start/3D HintKey Holding");
            while (Button2 != null)
            {
                yield return new WaitForSeconds(0.25f);
            }
            
            if (MitaCore.Instance.getDistanceToPlayer()<6f) MitaCore.Instance.sendSystemMessage("Игрок при тебе просмотрел твой картридж другого игрока в подвале. У него могут быть вопросы, надо как-то уйти от темы. Если он будет наставить, придется сбросить маску.");
            else MitaCore.Instance.sendSystemInfo("Игрок просмотрел твой картридж другого игрока в подвале пока ты была не рядом. У него могут быть вопросы, если он спросит надо как-то уйти от темы. Если он будет наставить, придется сбросить маску.");
        }

    }
}
