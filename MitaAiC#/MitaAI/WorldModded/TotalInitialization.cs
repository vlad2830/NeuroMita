using Il2Cpp;
using Il2CppEPOOutline;
using MelonLoader;
using System;
using System.Collections.Generic;
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
            GameObject sofa = MitaCore.TryfindChild(world, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/SofaChair");

            var objectAnimationPlayer =  sofa.AddComponent<ObjectAnimationPlayer>();
            var objectInteractive = sofa.AddComponent<ObjectInteractive>();
            Utils.CopyComponentValues(exampleComponent, objectInteractive);

            objectAnimationPlayer.animationStart = PlayerAnimationModded.getPlayerAnimationClip("Player StartSit");
            objectAnimationPlayer.animationLoop = PlayerAnimationModded.getPlayerAnimationClip("Player SitIdle");

            objectInteractive.eventClick = EventsProxy.ChangeAnimationEvent(sofa, "SofaSit");

            GameObject GameAihastion = MitaCore.TryfindChild(world, "Quests/Quest 1/Game Aihastion");
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
