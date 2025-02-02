using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using MelonLoader;
using UnityEngine;
namespace MitaAI.WorldModded
{
    public static class InteractionCases
    {

        public static void caseConsoleStart(GameObject console)
        {

            MelonCoroutines.Start(caseConsoleAct(console));
        }
        public static IEnumerator caseConsoleAct(GameObject console)
        {
            yield return new WaitForSeconds(1.5f);

            var Button1 = MitaCore.TryfindChild(MitaCore.worldBasement, "Quests/Quest1 Start/3D HintKey FindButton");
            Button1?.SetActive(true);

            while (Button1!=null)
            {
                yield return new WaitForSeconds(0.25f);
            }
            var Button2 = MitaCore.TryfindChild(MitaCore.worldBasement, "Quests/Quest1 Start/3D HintKey Holding");
            while (Button2 != null)
            {
                yield return new WaitForSeconds(0.25f);
            }

            yield return new WaitForSecondsRealtime(6f);
            PlayerAnimationModded.stopAnim();
            MitaCore.Instance.sendSystemMessage("Игрок просмотрел твой картридж другого игрока в подвале. У него могут быть вопросы, надо как-то уйти от темы. Если он будет наставить, придется сбросить маску.");
        }

    }
}
