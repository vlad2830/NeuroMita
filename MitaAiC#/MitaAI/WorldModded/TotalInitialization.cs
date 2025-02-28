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
using UnityEngine.SceneManagement;

namespace MitaAI
{
    // В теории сюда уйдет вся стартовая настройка
    public static class TotalInitialization
    {
        static ObjectInteractive exampleComponent;
        public static HashSet<string> additiveLoadedScenes = new HashSet<string>();

        // Инициализация шаблонного компонента
        public static void InitExampleComponent(Transform world)
        {
            GameObject pult = Utils.TryfindChild(world, "Quests/Quest 1/Addon/Interactive Aihastion");
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
        #region InitObjects
        public static void InitObjects()
        {
            //TotalInitialization.initTVGames(MitaCore.worldHouse);
            TotalInitialization.initCornerSofa(MitaCore.worldHouse);
        }


        public static void initCornerSofa(Transform world)
        {
            MelonLogger.Msg("initCornerSofa");
            GameObject sofa = Utils.TryfindChild(world, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/SofaChair");
            GameObject sofaChil = new GameObject("OI");
            sofaChil.transform.parent = sofa.transform;
            sofaChil.transform.localPosition = new Vector3(-0.4855f, 0.3255f, 0.0982f);
            sofaChil.transform.localRotation = Quaternion.Euler(10f, 90f, 90f);
            var objectAnimationPlayer = sofaChil.AddComponent<ObjectAnimationPlayer>();
            var objectInteractive = sofa.AddComponent<ObjectInteractive>();
            objectAnimationPlayer.angleHeadRotate = 70;
            Utils.CopyComponentValues(exampleComponent, objectInteractive);

            objectAnimationPlayer.animationStart = PlayerAnimationModded.getPlayerAnimationClip("Player StartSit1");
            objectAnimationPlayer.animationLoop = PlayerAnimationModded.getPlayerAnimationClip("Player Sit");
            objectAnimationPlayer.animationStop = PlayerAnimationModded.getPlayerAnimationClip("Player Stand");
            //objectInteractive.eventClick = EventsProxy.ChangeAnimationEvent(sofa, "SofaSit");

            //GameObject GameAihastion = Utils.TryfindChild(world, "Quests/Quest 1/Game Aihastion");

        }
    
        public static void initTVGames(Transform world)
        {
            GameObject pult = Utils.TryfindChild(world, "Quests/Quest 1/Addon/Interactive Aihastion");
            pult.active = false;
            pult.GetComponent<ObjectInteractive>().active = true;

            GameObject pultCopy= GameObject.Instantiate(pult, pult.transform.parent);

            MinigamesTelevisionController minigamesTelevisionController =  Utils.TryfindChild(world, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/TV/GameTelevision").GetComponent<MinigamesTelevisionController>();
            minigamesTelevisionController.destroyAfter = false;

         

            pultCopy.active = true;

            GameObject GameAihastion = Utils.TryfindChild(world, "Quests/Quest 1/Game Aihastion");
        }

        public static void initConsole(Transform worldBasement)
        {
            GameObject console = Utils.TryfindChild(MitaCore.worldBasement, "Act/Console");
            ObjectInteractive objectInteractive = console.GetComponent<ObjectInteractive>();
            console.GetComponent<Animator>().enabled = true;
            console.GetComponent<Outlinable>().enabled = true;

            Utils.TryTurnChild(MitaCore.worldBasement, "Quests/Quest1 Start/Dialogues Привет - Передай ключ",false);
            Utils.TryTurnChild(MitaCore.worldBasement, "Quests/Quest1 Start/Dialogues Console", false);
            objectInteractive.active = true;
            objectInteractive.destroyComponent = false;

            ObjectAnimationPlayer drop = Utils.TryfindChild(MitaCore.worldBasement, "Quests/Quest1 Start/AnimationPlayer Drop").GetComponent<ObjectAnimationPlayer>();
            //drop.eventsPlayer = new Il2CppSystem.Collections.Generic.List<UnityEngine.Events.UnityEvent>();
            //drop.animationStart.events = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<AnimationEvent>(0);

            //MitaCore.AddAnimationEvent(drop.gameObject, drop.animationStart,"ConsoleEnd");
            //drop.eventStartAnimaiton = null;
            //drop.eventStartLoop = null;
            drop.eventFinish = EventsProxy.ChangeAnimationEvent(drop.gameObject, "ConsoleEnd");
            //GameObject console = Utils.TryfindChild(MitaCore.worldBasement, "Act/Console");

        }
        #endregion



        #region getObjectsFromOtherScenes
        public static IEnumerator AddOtherScenes()
        {
            // Запускаем корутину для ожидания загрузки сцены
            string sceneToLoad;

            sceneToLoad = "Scene 7 - Backrooms";
            additiveLoadedScenes.Add(sceneToLoad);
            yield return MelonCoroutines.Start(WaitForSceneAndInstantiateWorldBackrooms(sceneToLoad));

            sceneToLoad = "Scene 10 - ManekenWorld";
            additiveLoadedScenes.Add(sceneToLoad);
            yield return MelonCoroutines.Start(WaitForSceneAndInstantiateWorldManekenWorld(sceneToLoad));

            sceneToLoad = "Scene 6 - BasementFirst";
            additiveLoadedScenes.Add(sceneToLoad);
            yield return MelonCoroutines.Start(WaitForSceneAndInstantiateWorldBasement(sceneToLoad));
     

      
            sceneToLoad = "Scene 11 - Backrooms";
            additiveLoadedScenes.Add(sceneToLoad);
            yield return MelonCoroutines.Start(WaitForSceneAndInstantiateWorldBackrooms2(sceneToLoad));
        
    
            sceneToLoad = "Scene 3 - WeTogether";
            additiveLoadedScenes.Add(sceneToLoad);
            yield return MelonCoroutines.Start(WaitForSceneAndInstantiateWorldTogether(sceneToLoad));


            yield return MelonCoroutines.Start(AfterAllLoadded());


        }
        private static IEnumerator WaitForSceneAndInstantiateWorldBasement(string sceneToLoad)
        {
            // Загружаем сцену
            MelonLogger.Msg($"Loading scene: {sceneToLoad}");
            additiveLoadedScenes.Add(sceneToLoad);
            SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);

            // Ожидание завершения загрузки сцены
            Scene scene;
            do
            {
                scene = SceneManager.GetSceneByName(sceneToLoad);
                yield return null; // Ждем следующий кадр
            } while (!scene.isLoaded);

            MelonLogger.Msg($"Scene {sceneToLoad} loaded.");

            // Находим объект в загруженной сцене
            MitaCore.worldBasement = FindObjectInScene(scene.name, "World");
            if (MitaCore.worldBasement == null)
            {
                MelonLogger.Msg("World object not found.");
                yield break; // Прерываем выполнение, если объект не найден
            }

            MelonLogger.Msg($"Object found: {MitaCore.worldBasement.name}");



            InitializeGameObjectsWhenReady();
        }
        private static IEnumerator WaitForSceneAndInstantiateWorldBackrooms2(string sceneToLoad)
        {
            // Загружаем сцену
            MelonLogger.Msg($"Loading scene: {sceneToLoad}");
            additiveLoadedScenes.Add(sceneToLoad);
            SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);

            // Ожидание завершения загрузки сцены
            Scene scene;
            do
            {
                scene = SceneManager.GetSceneByName(sceneToLoad);
                yield return null; // Ждем следующий кадр
            } while (!scene.isLoaded);

            MelonLogger.Msg($"Scene {sceneToLoad} loaded.");

            // Находим объект в загруженной сцене
            MitaCore.worldBackrooms2 = FindObjectInScene(scene.name, "World");
            if (MitaCore.worldBackrooms2 == null)
            {
                MelonLogger.Msg("World object not found.");
                yield break; // Прерываем выполнение, если объект не найден
            }
            MitaCore.worldBackrooms2.gameObject.SetActive(false);
            MelonLogger.Msg($"Object found: {MitaCore.worldBackrooms2.name}");
            try
            {
                MitaGames.ManekenTemplate = GameObject.Instantiate(Utils.TryfindChild(MitaCore.worldBackrooms2, "Quest/Quest 1 (Room 1 - Room 6)/Mita Maneken 1"), MitaCore.worldHouse);

                MitaGames.ManekenTemplate.transform.position = Vector3.zero;
                MitaGames.ManekenTemplate.transform.Find("MitaManeken 1").gameObject.GetComponent<Mob_Maneken>().speedNav = 4;



            }
            catch (Exception ex)
            {

                MelonLogger.Msg($"WaitForSceneAndInstantiate2 found: {ex}");
            }

            //SceneManager.UnloadScene(sceneToLoad);

        }
        private static IEnumerator WaitForSceneAndInstantiateWorldTogether(string sceneToLoad)
        {
            // Загружаем сцену
            MelonLogger.Msg($"Loading scene: {sceneToLoad}");
            additiveLoadedScenes.Add(sceneToLoad);
            SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);

            // Ожидание завершения загрузки сцены
            Scene scene;
            do
            {
                scene = SceneManager.GetSceneByName(sceneToLoad);
                yield return null; // Ждем следующий кадр
            } while (!scene.isLoaded);

            MelonLogger.Msg($"Scene {sceneToLoad} loaded.");

            // Находим объект в загруженной сцене
            MitaCore.worldTogether = FindObjectInScene(scene.name, "World");
            if (MitaCore.worldTogether == null)
            {
                MelonLogger.Msg("MitaCore.worldTogether object not found.");
                yield break; // Прерываем выполнение, если объект не найден
            }
            MitaCore.worldTogether.gameObject.SetActive(false);
            PlayerAnimationModded.FindPlayerAnimationsRecursive(MitaCore.worldTogether.transform);
            PlayerAnimationModded.Check();

            InitObjects();


            //SceneManager.UnloadScene(sceneToLoad);

        }
        private static IEnumerator WaitForSceneAndInstantiateWorldBackrooms(string sceneToLoad)
        {
            // Загружаем сцену
            MelonLogger.Msg($"Loading scene: {sceneToLoad}");
            additiveLoadedScenes.Add(sceneToLoad);
            SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);

            // Ожидание завершения загрузки сцены
            Scene scene;
            do
            {
                scene = SceneManager.GetSceneByName(sceneToLoad);
                yield return null; // Ждем следующий кадр
            } while (!scene.isLoaded);

            MelonLogger.Msg($"Scene {sceneToLoad} loaded.");

            // Находим объект в загруженной сцене
            MitaCore.worldBackrooms = FindObjectInScene(scene.name, "World");
            if (MitaCore.worldBackrooms == null)
            {
                MelonLogger.Msg("World object not found.");
                yield break; // Прерываем выполнение, если объект не найден
            }
            MitaCore.worldBackrooms.gameObject.SetActive(false);

            MelonLogger.Msg($"Object found: {MitaCore.worldBackrooms.name}");
            try
            {
                MitaCore.CappyObject = GameObject.Instantiate(Utils.TryfindChild(MitaCore.worldBackrooms, "Acts/Mita Кепка"), MitaCore.worldHouse);
                //MitaCore.CappyObject.transform.position = Vector3.zero;
                MitaCore.KindObject = GameObject.Instantiate(Utils.TryfindChild(MitaCore.worldBackrooms, "Acts/Mita Добрая"), MitaCore.worldHouse);
                //MitaCore.KindObject.transform.position = Vector3.zero;

                //MitaCore.Instance.changeMita(MitaCore.KindObject);

                //MitaCore.Instance.changeMita(MitaCore.CappyObject, MitaCore.character.Cappy);
            }
       
            catch (Exception ex)
            {

                MelonLogger.Error($"Cappy founding error: {ex}");
            }
            //yield return new WaitForSeconds(1f);
            SceneManager.UnloadScene(sceneToLoad);

        }
        private static IEnumerator WaitForSceneAndInstantiateWorldManekenWorld(string sceneToLoad)
        {
            // Загружаем сцену
            MelonLogger.Msg($"Loading scene: {sceneToLoad}");
            additiveLoadedScenes.Add(sceneToLoad);
            SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);

            // Ожидание завершения загрузки сцены
            Scene scene;
            do
            {
                scene = SceneManager.GetSceneByName(sceneToLoad);
                yield return null; // Ждем следующий кадр
            } while (!scene.isLoaded);

            MelonLogger.Msg($"Scene {sceneToLoad} loaded.");

            // Находим объект в загруженной сцене
            Transform worldManekenWorld = FindObjectInScene(scene.name, "World");
            if (worldManekenWorld == null)
            {
                MelonLogger.Msg("World object not found.");
                yield break; // Прерываем выполнение, если объект не найден
            }
            worldManekenWorld.gameObject.SetActive(false);

            MelonLogger.Msg($"Object found: {worldManekenWorld.name}");
            try
            {
                MitaCore.ShortHairObject = GameObject.Instantiate(Utils.TryfindChild(worldManekenWorld, "General/Mita Old"), MitaCore.worldHouse);


            }

            catch (Exception ex)
            {

                MelonLogger.Error($"Shorthair founding error: {ex}");
            }
            //yield return new WaitForSeconds(1f);
            SceneManager.UnloadScene(sceneToLoad);

        }

        private static IEnumerator AfterAllLoadded()
        {
            MelonLogger.Msg("After all loaded");
            MitaCore.character MitaToStart = Settings.MitaType.Value;
            MelonLogger.Msg($"Mita from settings {MitaToStart}");
            if (MitaCore.Instance.currentCharacter != MitaToStart)
            {
                MitaCore.Instance.changeMita(null,character : MitaToStart);
            }
            yield return new WaitForSeconds(0.25f);
            if (Utils.Random(1, 7)) MitaCore.Instance.sendSystemMessage("Игрок только что загрузился в твой уровень, можешь удивить его новым костюмом", MitaToStart);
            else MitaCore.Instance.sendSystemMessage("Игрок только что загрузился в твой уровень.", MitaToStart);
        }

        private static void InitializeGameObjectsWhenReady()
        {

            // Ваши действия после инициализации MitaCore.worldBasement
            try
            {
                // Пробуем найти и безопасно преобразовать объект в Transform
                var wardrobeTransform = MitaCore.worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Bedroom/Bedroom Wardrobe");

                try
                {
                    var wardrobeGameObject = wardrobeTransform.gameObject;
                    wardrobeGameObject.GetComponent<BoxCollider>().enabled = false;
                    Utils.TryTurnChild(wardrobeGameObject.transform, "Bedroom WardrobeDoorL", false);
                    Utils.TryTurnChild(wardrobeGameObject.transform, "Bedroom WardrobeDoorR", false);
                }
                catch (Exception)
                {
                    MelonLogger.Msg("Error while handling wardrobe transform.");
                }

                try
                {
                    // TotalInitialization.initConsole(MitaCore.worldBasement);
                }
                catch (Exception ex)
                {

                    MelonLogger.Msg("Error while handling initConsole: " + ex);
                }

                Utils.TryfindChild(MitaCore.worldBasement, "Act/ContinueScene").SetActive(false);
                Utils.TryfindChild(MitaCore.worldBasement, "Quests/Quest1 Start").SetActive(true);
                Utils.TryfindChild(MitaCore.worldBasement, "Quests/Quest1 Start/Trigger Near").SetActive(false);
                //Utils.TryfindChild(MitaCore.worldBasement, "/Act/ContinueScene\"").SetActive(false);

                //Utils.TryTurnChild(MitaCore.worldBasement, "Quests/Quest1 Start",false);
                Utils.TryTurnChild(MitaCore.worldBasement, "Mita Future", false);
                try
                {
                    var door = Utils.TryfindChild(MitaCore.worldBasement, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/General/BasementDoorFrame");
                    door.SetActive(true);
                    Utils.TryTurnChild(door.transform, "BasementDoor", false);
                }
                catch (Exception)
                {


                }
                // Работа с AnimationKiller
                MelonLogger.Msg("AnimationKiller start");
                string objectPath = "Quests/Quest2 HideAndSeek/Animation Killer";
                MitaCore.Instance.AnimationKiller = FindAndInstantiateObject(MitaCore.worldBasement, objectPath, "222");

                MitaCore.Instance.knife = FindAndInstantiateObject(MitaCore.Instance.AnimationKiller.transform, "Mita/MitaPerson Mita/Armature/Hips/Spine/Chest/Right shoulder/Right arm/Right elbow/Right wrist/Right item/Knife", "333");
                MitaCore.Instance.knife.transform.SetParent(Utils.TryfindChild(MitaCore.Instance.MitaPersonObject.transform, "Armature/Hips/Spine/Chest/Right shoulder/Right arm/Right elbow/Right wrist/Right item").transform);
                MitaCore.Instance.knife.transform.localPosition = new Vector3(0, 0, 0);
                MitaCore.Instance.knife.transform.localRotation = Quaternion.identity;
                MitaCore.Instance.knife.SetActive(false);

                //AnimationKiller.GetComponent<Location6_MitaKiller>().mita = Mita.transform;
                Utils.TryfindChild(MitaCore.worldBasement, "Sounds/Ambient 1").transform.parent = MitaCore.worldHouse.FindChild("Audio");
                MitaCore.Instance.AnimationKiller.SetActive(false);
                //DeleteChildren(AnimationKiller.transform.Find("PositionsKill").gameObject);

                if (MitaCore.Instance.AnimationKiller != null)
                {
                    MelonLogger.Msg("AnimationKiller instantiated and ready for use.");
                    var mitaChild = MitaCore.Instance.AnimationKiller.transform.Find("Mita");

                    if (mitaChild != null)
                    {
                        //mitaChild.gameObject.SetActive(false);
                        //Mita.transform.SetParent(AnimationKiller.transform, true);
                        MelonLogger.Msg("Child object 'Mita' deactivated.");
                    }
                    else
                    {
                        MelonLogger.Msg("Child object 'Mita' not found.");
                    }
                }
                else
                {
                    MelonLogger.Msg("Failed to initialize AnimationKiller.");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error in InitializeGameObjectsWhenReady: {e.Message}");
            }
        }

        public static Transform FindObjectInScene(string sceneName, string objectPath)
        {
            // Получаем сцену по имени
            Scene scene = SceneManager.GetSceneByName(sceneName);

            // Проверяем, загружена ли сцена
            if (!scene.IsValid() || !scene.isLoaded)
            {
                MelonLoader.MelonLogger.Msg($"Scene {sceneName} not loaded or broken");
                return null;
            }

            // Получаем корневые объекты сцены
            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                MelonLogger.Msg(rootObject.name);
                if (rootObject.name == "World")
                {
                    return rootObject.transform;
                }
            }

            MelonLoader.MelonLogger.Msg($" {objectPath} not found in {sceneName}.");
            return null;
        }
        private static GameObject FindAndInstantiateObject(Transform parent, string path, string logPrefix)
        {
            try
            {
                MelonLogger.Msg($"{logPrefix}: Attempting to find object at path: {path}");

                // Проверяем, что родитель не null
                if (parent == null)
                {
                    MelonLogger.Msg($"{logPrefix}: Parent is null. Cannot search for object.");
                    return null;
                }

                // Ищем объект по указанному пути
                Transform target = parent.Find(path);
                if (target == null)
                {
                    MelonLogger.Msg($"{logPrefix}: Object not found at path: {path}");
                    return null;
                }

                // Логируем успешный поиск
                MelonLogger.Msg($"{logPrefix}: Object found. Instantiating...");
                GameObject instance = GameObject.Instantiate(target.gameObject);

                // Проверяем успешную инстанциацию
                if (instance == null)
                {
                    MelonLogger.Msg($"{logPrefix}: Failed to instantiate object.");
                    return null;
                }

                MelonLogger.Msg($"{logPrefix}: Object instantiated successfully.");
                return instance;
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"{logPrefix}: Exception occurred - {e.Message}");
                return null;
            }
        }

        #endregion

    }

}
