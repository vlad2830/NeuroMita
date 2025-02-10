using Il2Cpp;
using Il2CppEPOOutline;
using MelonLoader;
using System;
using System.Collections;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MitaAI
{
    // В теории сюда уйдет вся стартовая настройка
    public static class TotalInitialization
    {
        static ObjectInteractive exampleComponent;

        // Инициализация шаблонного компонента
        public static void InitExampleComponent(Transform world)
        {
            GameObject pult = MitaCore.TryfindChild(world, "Quests/Quest 1/Addon/Interactive Aihastion");
            if (pult != null)
            {
                exampleComponent = ObjectInteractive.Instantiate(pult.GetComponent<ObjectInteractive>());
                MelonLogger.Msg("Example component initialized!");
            }
            else
            {
                MelonLogger.Msg("Failed to find template object!");
            }
        }

        public static void initCornerSofa(Transform world)
        {
            MelonLogger.Msg("initCornerSofa");
            GameObject sofa = MitaCore.TryfindChild(world, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/SofaChair");
            GameObject sofaChil = new GameObject("OI");
            sofaChil.transform.parent = sofa.transform;
            sofaChil.transform.localPosition = new Vector3(-0.4855f, 0.3255f, 0.0982f);
            sofaChil.transform.localRotation = Quaternion.Euler(10f, 90f, 90f);
            var objectAnimationPlayer = sofaChil.AddComponent<ObjectAnimationPlayer>();
            var objectInteractive = sofa.AddComponent<ObjectInteractive>();
            objectAnimationPlayer.angleHeadRotate = 90;
            Utils.CopyComponentValues(exampleComponent, objectInteractive);

            objectAnimationPlayer.animationStart = PlayerAnimationModded.getPlayerAnimationClip("Player StartSit1");
            objectAnimationPlayer.animationLoop = PlayerAnimationModded.getPlayerAnimationClip("Player Sit");

            //objectInteractive.eventClick = EventsProxy.ChangeAnimationEvent(sofa, "SofaSit");

            //GameObject GameAihastion = MitaCore.TryfindChild(world, "Quests/Quest 1/Game Aihastion");
          
        }


        public static void initTVGames(Transform world)
        {
            GameObject pult = MitaCore.TryfindChild(world, "Quests/Quest 1/Addon/Interactive Aihastion");
            pult.active = false;
            pult.GetComponent<ObjectInteractive>().active = true;

            GameObject pultCopy= GameObject.Instantiate(pult, pult.transform.parent);

            MinigamesTelevisionController minigamesTelevisionController =  MitaCore.TryfindChild(world, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/TV/GameTelevision").GetComponent<MinigamesTelevisionController>();
            minigamesTelevisionController.destroyAfter = false;

         

            pultCopy.active = true;

            GameObject GameAihastion = MitaCore.TryfindChild(world, "Quests/Quest 1/Game Aihastion");
        }

        public static void initConsole(Transform worldBasement)
        {
            GameObject console = MitaCore.TryfindChild(worldBasement, "Act/Console");
            ObjectInteractive objectInteractive = console.GetComponent<ObjectInteractive>();
            console.GetComponent<Animator>().enabled = true;
            console.GetComponent<Outlinable>().enabled = true;

            MitaCore.TryTurnChild(worldBasement, "Quests/Quest1 Start/Dialogues Привет - Передай ключ",false);
            MitaCore.TryTurnChild(worldBasement, "Quests/Quest1 Start/Dialogues Console", false);
            objectInteractive.active = true;
            objectInteractive.destroyComponent = false;

            ObjectAnimationPlayer drop = MitaCore.TryfindChild(worldBasement, "Quests/Quest1 Start/AnimationPlayer Drop").GetComponent<ObjectAnimationPlayer>();
            //drop.eventsPlayer = new Il2CppSystem.Collections.Generic.List<UnityEngine.Events.UnityEvent>();
            //drop.animationStart.events = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<AnimationEvent>(0);

            //MitaCore.AddAnimationEvent(drop.gameObject, drop.animationStart,"ConsoleEnd");
            //drop.eventStartAnimaiton = null;
            //drop.eventStartLoop = null;
            drop.eventFinish = EventsProxy.ChangeAnimationEvent(drop.gameObject, "ConsoleEnd");
            //GameObject console = MitaCore.TryfindChild(worldBasement, "Act/Console");

        }

    }
}
