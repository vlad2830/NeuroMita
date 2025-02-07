using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using UnityEngine.UI;
using static Il2CppRootMotion.FinalIK.InteractionObject;
using UnityEngine.Animations;
using UnityEngine.UIElements;
using Harmony;
using System.Diagnostics.Tracing;
using UnityEngine.Events;
using System.Reflection;
using static MelonLoader.InteropSupport;
using Microsoft.VisualBasic;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.Playables;
using System.Linq;
using UnityEngine.Networking.Match;
using System.Linq.Expressions;
using Il2CppColorful;
using Il2CppInterop.Runtime;
using static MelonLoader.ICSharpCode.SharpZipLib.Zip.ZipEntryFactory;
using System.Globalization;
using Il2CppInterop.Runtime.InteropTypes;
using static UnityEngine.UI.ScrollRect;
using UnityEngine.TextCore.Text;
using HarmonyLib;
using MelonLoader.Utils;
using System.Reflection.Metadata;
using UnityEngine.XR;
using UnityEngine.Profiling;
using UnityEngine.AI;
using static Il2CppRootMotion.FinalIK.IKSolverVR;
using static Il2CppSystem.Uri;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MitaAI.MitaAppereance;
using MitaAI.Mita;

[assembly: MelonInfo(typeof(MitaAI.MitaCore), "MitaAI", "1.0.0", "Dmitry", null)]
[assembly: MelonGame("AIHASTO", "MiSideFull")]

namespace MitaAI
{
    public class MitaCore : MelonMod
    {
        // Ссылка на экземпляр MitaCore, если он нужен
        public static MitaCore Instance;
        public MitaCore()
        {
            Instance = this;
        }
        // Метод, который будет вызываться после выполнения PrivateMethod

        public GameObject MitaObject;
        public GameObject MitaPersonObject;
        public MitaPerson Mita;
        Animator_FunctionsOverride MitaAnimatorFunctions;
        public Character_Look MitaLook;
        enum MovementStyles
        {
            walkNear = 0,
            follow = 1,
            stay = 2,
            noclip = 3

        }
        MovementStyles movementStyle = 0;
        enum MitaState
        {
            normal = 0,
            hunt = 1

        }
        MitaState mitaState = 0;

        GameObject knife;

        PlayerPerson playerPerson;
        GameObject playerObject;
        PlayerCameraEffects playerEffects;
        GameObject playerEffectsObject;
        public float distance = 0f;
        public string currentInfo = "";
        Location34_Communication Location34_Communication;
        Location21_World location21_World;

        public static Transform worldTogether;
        public static Transform worldHouse;
        public static Transform worldBasement;
        public static Transform worldBackrooms2;
        

        GameObject ManekenTemplate;
        List<GameObject> activeMakens = new List<GameObject>();

        public Menu MainMenu;
        private GameObject CustomDialog;
        private GameObject CustomDialogPlayer;
        GameObject playerCamera;
        GameObject AnimationKiller;
        BlackScreen blackScreen;



        private const float Interval = 0.35f;
        private float timer = 0f;
        public float blinkTimer = 7f;

        Vector3 lastPosition;

        //Queue<string> waitForSoundsQ = new Queue<string>();
        string waitForSounds = "0";
        //private readonly object waitForSoundsLock = new object();

        string playerMessage = "";
        public Queue<string> systemMessages = new Queue<string>();
        Queue<string> systemInfos = new Queue<string>();

        Queue<string> patches_to_sound_file = new Queue<string>();
        string patch_to_sound_file = "";

        public string hierarchy = "-";

        static List<AnimationClip> MitaAnims = new List<AnimationClip>();
        static Il2CppAssetBundle bundle;

        
        string requiredSceneName = "Scene 4 - StartSecret";
        string requiredSave = "SaveGame startsecret";
        string CurrentSceneName;

        private HashSet<string> additiveLoadedScenes = new HashSet<string>();
        private bool AllLoaded = false;


        private readonly object _lockObj = new object();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Синхронизируем доступ ко всем этим операциям


        private const float MitaBoringInterval = 75f;
        private float MitaBoringtimer = 0f;

        bool manekenGame = false;


        HarmonyLib.Harmony harmony;
        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();

            harmony = new HarmonyLib.Harmony("1");
            MitaClothesModded.init(harmony);
            NetworkController.Initialize();

            //Test2();
        }


        public void sendSystemMessage(string m)
        {
            systemMessages.Enqueue(m);
        }
        public void sendSystemInfo(string m)
        {
            systemInfos.Enqueue(m);
        }

        public void AddOtherScenes()
        {
            // Запускаем корутину для ожидания загрузки сцены
            string sceneToLoad;
            try
            {
                sceneToLoad = "Scene 6 - BasementFirst";
                additiveLoadedScenes.Add(sceneToLoad);
                MelonCoroutines.Start(WaitForSceneAndInstantiate(sceneToLoad));
            }
            catch (Exception)
            {


            }

            try
            {
                sceneToLoad = "Scene 11 - Backrooms";
                additiveLoadedScenes.Add(sceneToLoad);
                MelonCoroutines.Start(WaitForSceneAndInstantiate2(sceneToLoad));
            }
            catch (Exception)
            {


            }

            try
            {
                sceneToLoad = "Scene 3 - WeTogether";
                additiveLoadedScenes.Add(sceneToLoad);
                MelonCoroutines.Start(WaitForSceneAndInstantiate3(sceneToLoad));
            }
            catch (Exception)
            {


            }


        }

        private IEnumerator WaitForSceneAndInstantiate(string sceneToLoad)
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
            worldBasement = FindObjectInScene(scene.name, "World");
            if (worldBasement == null)
            {
                MelonLogger.Msg("World object not found.");
                yield break; // Прерываем выполнение, если объект не найден
            }

            MelonLogger.Msg($"Object found: {worldBasement.name}");



            InitializeGameObjectsWhenReady();
        }
        private IEnumerator WaitForSceneAndInstantiate2(string sceneToLoad)
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
            worldBackrooms2 = FindObjectInScene(scene.name, "World");
            if (worldBackrooms2 == null)
            {
                MelonLogger.Msg("World object not found.");
                yield break; // Прерываем выполнение, если объект не найден
            }
            worldBackrooms2.gameObject.SetActive(false);
            MelonLogger.Msg($"Object found: {worldBackrooms2.name}");
            try
            {
                ManekenTemplate = GameObject.Instantiate(TryfindChild(worldBackrooms2, "Quest/Quest 1 (Room 1 - Room 6)/Mita Maneken 1"), worldHouse);

                ManekenTemplate.transform.position = Vector3.zero;
                ManekenTemplate.transform.Find("MitaManeken 1").gameObject.GetComponent<Mob_Maneken>().speedNav = 4;



            }
            catch (Exception ex)
            {

                MelonLogger.Msg($"WaitForSceneAndInstantiate2 found: {ex}");
            }

        }

        private IEnumerator WaitForSceneAndInstantiate3(string sceneToLoad)
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
            worldTogether = FindObjectInScene(scene.name, "World");
            if (worldTogether == null)
            {
                MelonLogger.Msg("worldTogether object not found.");
                yield break; // Прерываем выполнение, если объект не найден
            }
            worldTogether.gameObject.SetActive(false);
            PlayerAnimationModded.FindPlayerAnimationsRecursive(worldTogether.transform);
            PlayerAnimationModded.Check();
        }

        public void playerKilled()
        {
            sendSystemMessage("Игрок был укушен манекеном. Манекен выключился (его можно перезапустить)");
            playerPerson.transform.parent.position = GetRandomLoc().position;
            Component effectComponent = playerEffectsObject.GetComponentByName("Glitch");
            if (effectComponent is Il2CppObjectBase il2cppComponent)
            {
                // Если это Il2CppObjectBase
                LoggerInstance.Msg($"Il2Cpp component detected: {il2cppComponent.GetType().Name}");

                // Проверяем, имеет ли компонент свойство enabled
                var enabledProperty = il2cppComponent.GetType().GetProperty("enabled");
                var behaviour = il2cppComponent.TryCast<Behaviour>();
                behaviour.enabled = true;

                // Запускаем корутину, передавая Il2Cpp-компонент
                MelonCoroutines.Start(HandleIl2CppComponent(il2cppComponent, 5f));

            }
        }
        public void playerClickSafe()
        {
            sendSystemMessage("Игрок кликает на кнопку твоего сейфа");
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

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (sceneName == requiredSceneName)
            {
                sendSystemInfo("Игрок покинул твой уровень");
            }
            base.OnSceneWasUnloaded(buildIndex, sceneName);
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)

        {
            ;

            LoggerInstance.Msg("Scene loaded " + sceneName);
            if (!additiveLoadedScenes.Contains(sceneName))
            {
                LoggerInstance.Msg("Scene loaded not addictive" + sceneName);
                CurrentSceneName = sceneName;
            }
            else
            {
                LoggerInstance.Msg("Scene loaded addictive!!! " + sceneName);
                return;
            }


            if (CurrentSceneName == "AiHasto")
            {

                //GameObject GameObject = GameObject.Find("MenuGame")>
            }

            else if (CurrentSceneName == "SceneMenu")
            {

                GameObject NeuroMitaButton = GameObject.Instantiate(GameObject.Find("MenuGame/Canvas/FrameMenu/Location Menu/Button Continue").gameObject);


                sendSystemInfo("Игрок в меню");
                MainMenu = GameObject.Find("MenuGame").GetComponent<Menu>();
                //MainMenu.Alternative();
                MainMenu.ButtonLoadScene(requiredSave);
                //MainMenu.ButtonLoadScene("Scene 4 - StartSecret");
                //MainMenu.ButtonLoadScene("Scene 3 - WeTogether");

            }

            else if (requiredSceneName == CurrentSceneName)
            {
                try
                {
                    GameObject Trigger = GameObject.Find("World/Quests/Quest 1/Trigger Entry Kitchen");
                    Trigger.SetActive(false);


                    GameObject Location34_CommunicationObject = GameObject.Find("World/Quests/Quest 1/Addon");
                    Location34_Communication = GameObject.Find("World/Quests/Quest 1/Addon").GetComponent<Location34_Communication>();

                    Location34_Communication.mitaCanWalk = true;
                    Location34_Communication.indexSwitchAnimation = 1;
                    Location34_Communication.play = true;



                    CollectChildObjects(Location34_CommunicationObject);



                }
                catch (Exception ex)
                {
                    LoggerInstance.Error(ex);
                }
                InitializeGameObjects();
            }
        }

        public int roomIDPlayer = -1;
        public int roomIDMita = -1;
        // Первая функция, которая принимает PlayerMove
        public void CheckRoom(PlayerMove playerMove)
        {
            if (playerMove == null)
            {
                return;
            }

            // Передаем трансформ игрока во вторую функцию
            roomIDPlayer = GetRoomID(playerMove.transform);
        }

        // Вторая функция, которая принимает Transform и возвращает roomID
        public int GetRoomID(Transform playerTransform)
        {
            if (playerTransform == null)
            {
                return -1;
            }

            var posX = playerTransform.position.x;
            var posZ = playerTransform.position.z;
            var posY = playerTransform.position.y;

            if (posY <= -0.0002f)
            {
                return 4; // basement
            }
            else
            {
                // Логика определения комнаты
                if (posX > 5.3000002f && posZ >= 0) return 0; // Kitchen
                else if (posX > 5.3000002f && posZ < 0) return 2; // Bedroom
                else if (posX > -4 && posX < 5) return 1; // Main
                else if (posX > -11.0f && posX < -4.3000002f) return 3; // Toilet 
            }
            return -1; // Если не нашли подходящей комнаты
        }

        EyeGlowModifier eyeModifier;
        private void InitializeGameObjects()
        {
            Mita = GameObject.Find("Mita")?.GetComponent<MitaPerson>();
            MitaObject = GameObject.Find("Mita").gameObject;
            
            MitaPersonObject = MitaObject.transform.Find("MitaPerson Mita").gameObject;

            MitaLook = MitaObject.transform.Find("MitaPerson Mita/IKLifeCharacter").gameObject.GetComponent<Character_Look>();
            MitaAnimatorFunctions = MitaPersonObject.GetComponent<Animator_FunctionsOverride>();
            MitaAnimationModded.init(MitaAnimatorFunctions, Location34_Communication);
            Mita.AiShraplyStop();

            //GameObject eyeObject = TryfindChild(MitaPersonObject.transform, "Armature/Hips/Spine/Chest/Neck2/Neck1/Head/Right Eye");
            //eyeModifier = new EyeGlowModifier(eyeObject);
            //eyeObject = TryfindChild(MitaPersonObject.transform, "Armature/Hips/Spine/Chest/Neck2/Neck1/Head/Left Eye");
            //eyeModifier = new EyeGlowModifier(eyeObject);

            //Mita.gameObject.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = true;

            playerPerson = GameObject.Find("Person")?.GetComponent<PlayerPerson>();
            playerObject = playerPerson.transform.parent.gameObject;

            
            playerObject.GetComponent<PlayerMove>().speedPlayer = 1f;
            playerObject.GetComponent<PlayerMove>().canRun = true;


            playerEffects = playerPerson.transform.parent.Find("HeadPlayer/MainCamera").gameObject.GetComponent<PlayerCameraEffects>();
            playerEffectsObject = playerPerson.transform.parent.Find("HeadPlayer/MainCamera/CameraPersons").gameObject;
            blackScreen = GameObject.Find("Game/Interface/BlackScreen").GetComponent<BlackScreen>();
            try
            {
                playerCamera = playerPerson.transform.parent.gameObject.transform.FindChild("HeadPlayer/MainCamera").gameObject;
                LoggerInstance.Msg("Camera found" + playerCamera.name);
            }
            catch (Exception)
            {

                LoggerInstance.Msg("Camera not LoggerInstance.Msg(\"Camera found\" + playerCamera.name);found" + playerCamera.name);
            }



            LoggerInstance.Msg(Mita != null ? "Mita found!" : "Mita not found.");
            LoggerInstance.Msg(playerPerson != null ? "Player found!" : "Player not found.");

            if (Mita == null || playerPerson == null) return;


            CommandProcessor.Initialize(this, playerObject.transform,MitaObject.transform,Location34_Communication);

                
            worldHouse = GameObject.Find("World")?.transform;
            World worldSettings = worldHouse.gameObject.GetComponent<World>();
            MitaObject.transform.SetParent(worldHouse);
            location21_World = worldHouse.gameObject.AddComponent<Location21_World>();

            PlayerAnimationModded.Init(playerObject, worldHouse, playerObject.GetComponent<PlayerMove>());
            LightingAndDaytime.Init(location21_World, worldHouse);
            MelonCoroutines.Start(StartDayTime());
            //MelonCoroutines.Start(UpdateLighitng());


            TotalInitialization.initTVGames(worldHouse);
            TotalInitialization.initCornerSofa(worldHouse);


            try
            {
                AudioControl.Init(worldHouse);
            }
            catch (Exception ex) { LoggerInstance.Error(ex); }

            worldSettings.limitFloor = -200f;
            if (worldHouse == null)
            {
                LoggerInstance.Msg("World object not found.");

            }


            Transform dialogOriginal = worldHouse.Find("Quests/Quest 1/Dialogues/Dialogue Mita/Dialogue Finish Aihastion/DMita 2");

            if (dialogOriginal == null)
            {
                LoggerInstance.Msg("Target object 'DMita 2' not found.");
                dialogOriginal = worldHouse.Find("Quests/Quest 1 Start/3D Text 5");

                if (dialogOriginal == null)
                {

                    LoggerInstance.Msg("Target object '3D Text 5' not found.");
                }

            }

            CustomDialog = GameObject.Instantiate(dialogOriginal.gameObject, worldHouse.Find("Quests/Quest 1/Dialogues"));
            CustomDialog.name = "Custom Dialogue";

            // Опускаем объект CustomDialog на 200 единиц по оси Y
            Vector3 newPosition = CustomDialog.transform.position; // Получаем текущую позицию
            newPosition.y -= 200; // Уменьшаем Y на 200
            CustomDialog.transform.position = newPosition; // Применяем новую позицию


            Dialogue_3DText CustomDialogText = CustomDialog.GetComponent<Dialogue_3DText>();

            CustomDialogText.nextText = null;
            CustomDialogText.sizeHeight = 0.0687f;
            CustomDialogText.sizeSymbol = 0.0014f;
            CustomDialogText.sizeWidth = 0.75f;
            CustomDialogText.xPrint = 0.413f;
            CustomDialogText.indexString = -1;

            dialogOriginal = worldHouse.Find("Quests/Quest 1/Dialogues/Dialogue Player/Dialogue Hello/DPlayer 3");
            CustomDialogPlayer = GameObject.Instantiate(dialogOriginal.gameObject, worldHouse.Find("Quests/Quest 1/Dialogues"));
            CustomDialogPlayer.name = "Custom Dialogue Player";

            CustomDialogText = CustomDialogPlayer.GetComponent<Dialogue_3DText>();

            CustomDialogText.nextText = null;
            CustomDialogText.sizeHeight = 0.0687f;
            CustomDialogText.sizeSymbol = 0.0014f;
            CustomDialogText.sizeWidth = 0.75f;
            CustomDialogText.xPrint = 0.413f;
            CustomDialogText.indexString = -1;
            CustomDialogText.showSubtitles = true;

            MelonLogger.Msg($"Attempt Interactions before");
            Interactions.CreateObjectInteractable(TryfindChild(worldHouse, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/LivingTable").gameObject);
            
            Interactions.CreateObjectInteractable(TryfindChild(worldHouse, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/CornerSofa").gameObject);
            Interactions.CreateObjectInteractable(TryfindChild(worldHouse, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Kitchen/Kitchen Table").gameObject);
            Interactions.CreateObjectInteractable(TryfindChild(worldHouse, "Quests/Quest 1/Addon/Interactive Aihastion").gameObject);
            //MelonLogger.Msg($"Attempt after");
            MelonLogger.Msg($"Attempt Interactions end");
            try
            {
                AddOtherScenes();
            }
            catch { }

            try
            {
                bundle = AssetBundleLoader.LoadAssetBundle("assetbundle");
            }
            catch (Exception)
            {

            }

            AllLoaded = true;
            //Interactions.Test(GameObject.Find("Table"));
            MelonCoroutines.Start(RealTimer());

            sendSystemMessage("Игрок только что загрузился в твой уровень.");




            //TestBigMita();
        }
        void TestBigMita()
        {
            LoggerInstance.Msg("Start TestBigMita");
            MitaObject.transform.FindChild("MitaPerson Mita").localScale = new Vector3(15f,15f,15f);
            
            Vector3 direction = (MitaObject.transform.position - playerObject.transform.position).normalized;

            MitaObject.transform.SetPositionAndRotation(new Vector3(15f, 0f, 15f), Quaternion.LookRotation(direction));

            try
            {
                GameObject floor = GameObject.Instantiate(TryfindChild(worldHouse, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Bedroom/FloorBedroom").gameObject);
                floor.transform.localScale = new Vector3(30f, 1, 30f);
            }
            catch (Exception ex)
            {
                LoggerInstance.Msg("TestBigMita end " + ex);
            }
            
            worldHouse.Find("House").gameObject.SetActive(false);
            worldBasement.Find("House").gameObject.SetActive(false);

        }
        private void InitializeGameObjectsWhenReady()
        {

            // Ваши действия после инициализации worldBasement
            try
            {
                // Пробуем найти и безопасно преобразовать объект в Transform
                var wardrobeTransform = worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Bedroom/Bedroom Wardrobe");

                try
                {
                        var wardrobeGameObject = wardrobeTransform.gameObject;
                        wardrobeGameObject.GetComponent<BoxCollider>().enabled = false;
                    TryTurnChild(wardrobeGameObject.transform, "Bedroom WardrobeDoorL", false);
                    TryTurnChild(wardrobeGameObject.transform, "Bedroom WardrobeDoorR", false);
                }
                catch (Exception)
                {
                    LoggerInstance.Msg("Error while handling wardrobe transform.");
                }

                try
                {
                    TotalInitialization.initConsole(worldBasement);
                }
                catch (Exception ex)
                {

                    LoggerInstance.Msg("Error while handling initConsole: "+ex);
                }
               
                TryfindChild(worldBasement, "Act/ContinueScene").SetActive(false);
                TryfindChild(worldBasement, "Quests/Quest1 Start").SetActive(true);
                TryfindChild(worldBasement, "Quests/Quest1 Start/Trigger Near").SetActive(false);
                //TryfindChild(worldBasement, "/Act/ContinueScene\"").SetActive(false);

                //TryTurnChild(worldBasement, "Quests/Quest1 Start",false);
                TryTurnChild(worldBasement, "Mita Future", false);
                try
                {
                    var door = TryfindChild(worldBasement, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/General/BasementDoorFrame");
                    door.SetActive(true);
                    TryTurnChild(door.transform, "BasementDoor", false);
                }
                catch (Exception)
                {

                    
                } 
                // Работа с AnimationKiller
                LoggerInstance.Msg("AnimationKiller start");
                string objectPath = "Quests/Quest2 HideAndSeek/Animation Killer";
                AnimationKiller = FindAndInstantiateObject(worldBasement, objectPath, "222");

                knife = FindAndInstantiateObject(AnimationKiller.transform, "Mita/MitaPerson Mita/Armature/Hips/Spine/Chest/Right shoulder/Right arm/Right elbow/Right wrist/Right item/Knife", "333");
                knife.transform.SetParent(TryfindChild(MitaPersonObject.transform, "Armature/Hips/Spine/Chest/Right shoulder/Right arm/Right elbow/Right wrist/Right item").transform);
                knife.transform.localPosition = new Vector3(0,0,0);
                knife.transform.rotation = Quaternion.identity;
                knife.SetActive(false);

                //AnimationKiller.GetComponent<Location6_MitaKiller>().mita = Mita.transform;
                TryfindChild(worldBasement, "Sounds/Ambient 1").transform.parent = worldHouse.FindChild("Audio");
                AnimationKiller.SetActive(false);
                //DeleteChildren(AnimationKiller.transform.Find("PositionsKill").gameObject);

                if (AnimationKiller != null)
                {
                    LoggerInstance.Msg("AnimationKiller instantiated and ready for use.");
                    var mitaChild = AnimationKiller.transform.Find("Mita");

                    if (mitaChild != null)
                    {
                        //mitaChild.gameObject.SetActive(false);
                        //Mita.transform.SetParent(AnimationKiller.transform, true);
                        LoggerInstance.Msg("Child object 'Mita' deactivated.");
                    }
                    else
                    {
                        LoggerInstance.Msg("Child object 'Mita' not found.");
                    }
                }
                else
                {
                    LoggerInstance.Msg("Failed to initialize AnimationKiller.");
                }
            }
            catch (Exception e)
            {
                LoggerInstance.Msg($"Error in InitializeGameObjectsWhenReady: {e.Message}");
            }
        }
        public static GameObject TryfindChild(Transform parent, string path)
        {
            try
            {
                return parent.Find(path).gameObject;
            }
            catch (Exception e)
            {

                MelonLogger.Msg($"Tried found {path} but {e}");
                return null;
            }
        }
        public static void TryTurnChild(Transform parent, string path, bool on)
        {
            try
            {
                TryfindChild(parent, path).gameObject.SetActive(on);
            }
            catch (Exception e)
            {

                MelonLogger.Msg("Tried turn "+ path +" "+ e);
                return;
            }
        }

        private GameObject FindAndInstantiateObject(Transform parent, string path, string logPrefix)
        {
            try
            {
                LoggerInstance.Msg($"{logPrefix}: Attempting to find object at path: {path}");

                // Проверяем, что родитель не null
                if (parent == null)
                {
                    LoggerInstance.Msg($"{logPrefix}: Parent is null. Cannot search for object.");
                    return null;
                }

                // Ищем объект по указанному пути
                Transform target = parent.Find(path);
                if (target == null)
                {
                    LoggerInstance.Msg($"{logPrefix}: Object not found at path: {path}");
                    return null;
                }

                // Логируем успешный поиск
                LoggerInstance.Msg($"{logPrefix}: Object found. Instantiating...");
                GameObject instance = GameObject.Instantiate(target.gameObject);

                // Проверяем успешную инстанциацию
                if (instance == null)
                {
                    LoggerInstance.Msg($"{logPrefix}: Failed to instantiate object.");
                    return null;
                }

                LoggerInstance.Msg($"{logPrefix}: Object instantiated successfully.");
                return instance;
            }
            catch (Exception e)
            {
                LoggerInstance.Msg($"{logPrefix}: Exception occurred - {e.Message}");
                return null;
            }
        }

        public static void DeleteChildren(GameObject parent)
        {
            if (parent == null)
            {
                MelonLoader.MelonLogger.Error("Parent object is null.");
                return;
            }

            // Создаем временный массив, чтобы избежать изменения итерируемой коллекции
            Transform[] children = new Transform[parent.transform.childCount];
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                children[i] = parent.transform.GetChild(i);
            }

            // Удаляем каждого ребенка
            foreach (Transform child in children)
            {
                GameObject.Destroy(child.gameObject);
            }

            MelonLoader.MelonLogger.Msg($"All children of {parent.name} have been deleted.");
        }


        bool FirstTime = true;

        public override void OnLateUpdate()
        {
            
            base.OnLateUpdate();
            Interactions.Update();
            // if (eyeModifier!=null) eyeModifier.OnUpdateTest();
        }
        public IEnumerator RealTimer()
        {
            while (true)
            {
                // Проверяем условия
                if (CurrentSceneName != "Scene 4 - StartSecret" || !AllLoaded)
                {
                    yield return null; // Пропускаем итерацию, если условия не выполнены
                    continue;
                }

                // Обновляем таймеры
                timer += Time.deltaTime;


                // Проверяем, достиг ли timer значения Interval
                if (timer >= Interval)
                {
                    timer = 0f; // Сбрасываем таймер
                    yield return HandleDialogue(); // Запускаем HandleDialogue и ждем его завершения
                }

                yield return null; // Ждем следующего кадра
            }
        }

        private IEnumerator UpdateLighitng()
        {
            yield return new WaitForSeconds(1f);
            while (true)
            {
                try
                {
                    LightingAndDaytime.CheckDay();

                }
                catch (Exception e)
                {

                    LoggerInstance.Msg("Error LightingAndDaytime CheckDay" + e);
                }
                yield return new WaitForSeconds(2.3f); // Ждем 7 секунд перед следующим циклом
            }
        }

        private IEnumerator StartDayTime()
        {

            yield return new WaitForSeconds(1f);
            LightingAndDaytime.setTimeDay(0.5f);
        }


        private IEnumerator CheckManekenGame()
        {
            while (true)
            {
                try
                {
                    if (!manekenGame) yield break;

                    if (blackScreen != null && playerCamera != null)
                    {
                        blackScreen.BlackScreenAlpha(0.75f);
                        playerCamera.GetComponent<Camera>().enabled = false;
                        MelonCoroutines.Start(ToggleComponentAfterTime(playerCamera.GetComponent<Camera>(), 0.75f)); // Отключит playerCamera через 1 секунду
                    }

                    yield return new WaitForSeconds(blinkTimer); // Ждем 7 секунд перед следующим циклом
                }
                finally { }

            }
        }


        public IEnumerator ToggleObjectActiveAfterTime(GameObject obj, float delay)
        {
            // Проверяем, не null ли объект
            if (obj == null)
            {
                LoggerInstance.Msg("GameObject is null. Cannot toggle.");
                yield break;
            }

            // Ждём заданное время
            yield return new WaitForSeconds(delay);

            // Переключаем активность объекта
            obj.SetActive(!obj.activeSelf);
            LoggerInstance.Msg($"GameObject {obj.name} is now {(obj.activeSelf ? "active" : "inactive")}");
        }

        public IEnumerator DestroyObjecteAfterTime(GameObject obj, float delay)
        {
            // Проверяем, не null ли объект
            if (obj == null)
            {
                LoggerInstance.Msg("GameObject is null. Cannot toggle.");
                yield break;
            }

            // Ждём заданное время
            yield return new WaitForSeconds(delay);

            GameObject.Destroy(obj);
        }


        public float getDistance()
        {
            if (Mita == null || playerPerson == null) { return 0f; }
            return Vector3.Distance(Mita.transform.GetChild(0).position, playerPerson.transform.position);
        }

        private float lastActionTime = -Mathf.Infinity;  // Для отслеживания времени последнего действия
        private const float actionCooldown = 9f;  // Интервал в секундах
        private IEnumerator HandleDialogue()
        {
            MitaBoringtimer += Time.deltaTime;

            string dataToSent = "waiting";
            string dataToSentSystem = "-";
            string info = "-";

            float currentTime = Time.time;
            if (currentTime - lastActionTime > actionCooldown)
            {
                if (playerMessage != "")
                {
                    MitaBoringtimer = 0f;
                    dataToSent = playerMessage;
                    playerMessage = "";
                    lastActionTime = Time.time;
                }
                else if (systemMessages.Count > 0)
                {
                    LoggerInstance.Msg("HAS SYSTEM MESSAGES");
                    MitaBoringtimer = 0f;

                    //Отправляю залпом.
                    while (systemMessages.Count() > 0)
                    {
                        dataToSentSystem += systemMessages.Dequeue() + "\n";
                    }
                    lastActionTime = Time.time;

                }
                else if (MitaBoringtimer >= MitaBoringInterval && mitaState == MitaState.normal)
                {
                    MitaBoringtimer = 0f;
                    dataToSentSystem = "boring";
                    lastActionTime = Time.time;
                }
            }



            string response = "";

            if (systemInfos.Count > 0)
            {
                LoggerInstance.Msg("HAS SYSTEM INFOS");
                //Отправляю залпом.
                while (systemInfos.Count() > 0)
                {
                    info += systemInfos.Dequeue()+ "\n";
                }

            }


            prepareForSend();
            Task<(string,string)> responseTask = NetworkController.GetResponseFromPythonSocketAsync(dataToSent, dataToSentSystem, info);
            while (!responseTask.IsCompleted)
                yield return null;

            response = responseTask.Result.Item1;
            string patch = responseTask.Result.Item2;
            if (!string.IsNullOrEmpty(patch)) patches_to_sound_file.Enqueue(patch);
            if (response != "")
            {
                LoggerInstance.Msg("after GetResponseFromPythonSocketAsync");

                MelonCoroutines.Start(DisplayResponseAndEmotionCoroutine(response));
            }
            


        }
        public void prepareForSend()
        {
            if (Vector3.Distance(Mita.transform.GetChild(0).position, lastPosition) > 2f)
            {
                try
                {
                    lastPosition = Mita.transform.GetChild(0).position;
                    List<string> excludedNames = new List<string> { "Hips", "Maneken" };
                    if (roomIDMita == 4) hierarchy = ObjectHierarchyHelper.GetObjectsInRadiusAsTree(Mita.gameObject, 10f, worldBasement.Find("House").transform, excludedNames);
                    else hierarchy = ObjectHierarchyHelper.GetObjectsInRadiusAsTree(Mita.gameObject, 10f, worldHouse.Find("House").transform, excludedNames);

                    //LoggerInstance.Msg(hierarchy);
                }
                catch (Exception e) { LoggerInstance.Msg("hierarchy error " + e); }
            }
            if (string.IsNullOrEmpty(hierarchy)) hierarchy = "-";

            distance = getDistance();
            roomIDPlayer = GetRoomID(playerPerson.transform);
            roomIDMita = GetRoomID(Mita.transform);
            currentInfo = formCurrentInfo();
        }

        // Глобальный список для хранения дочерних объектов
        List<GameObject> globalChildObjects = new List<GameObject>();

        // Функция для получения дочерних объектов и добавления их в глобальный список
        void CollectChildObjects(GameObject parentObject)
        {
            // Проверяем, что объект не null
            if (parentObject == null)
            {
                LoggerInstance.Error("Parent object is null!");
                return;
            }

            // Получаем Transform родительского объекта
            Transform parentTransform = parentObject.transform;

            // Перебираем всех детей
            for (int i = 0; i < parentTransform.childCount; i++)
            {
                Transform childTransform = parentTransform.GetChild(i);

                if (childTransform != null)
                {
                    // Добавляем дочерний объект в глобальный список
                    globalChildObjects.Add(childTransform.gameObject);

                    if (i == 0)
                    {
                        GameObject newPoint = GameObject.Instantiate(childTransform.gameObject,new Vector3(12.8382f,-2.9941f,-16.8005f),Quaternion.identity, childTransform.parent);
                        newPoint.name = "Point Basement 1";
                        globalChildObjects.Add(newPoint);

                        remakeArrayl34(Location34_Communication, newPoint, "b");

                        newPoint = GameObject.Instantiate(childTransform.gameObject, new Vector3(17.0068f, -2.9941f, -13.2256f), Quaternion.identity, childTransform.parent);
                        newPoint.name = "Point Basement 2";
                        globalChildObjects.Add(newPoint);

                        remakeArrayl34(Location34_Communication, newPoint,"b");
                    }

                }
            }
            
            // Выводим общее количество детей
            LoggerInstance.Msg($"Total children collected: {globalChildObjects.Count}");
        }

        
        public void remakeArrayl34(Location34_Communication Location34_Communication, GameObject newPoint, string room)
        {
            LoggerInstance.Msg($"Start Il2CppReferenceArray {Location34_Communication} 33 {newPoint} ");
            // Создаем новый массив с размером на 1 больше
            Il2CppReferenceArray<Location34_PositionForMita> newArray = new Il2CppReferenceArray<Location34_PositionForMita>(Location34_Communication.positionsForMita.Length + 1);
            LoggerInstance.Msg($" Il2CppReferenceArray222");
            // Копируем старые данные
            for (int i = 0; i < Location34_Communication.positionsForMita.Length; i++)
            {
                newArray[i] = Location34_Communication.positionsForMita[i];
            }

            LoggerInstance.Msg($" Il2CppReferenceArray333");
            // Добавляем новый элемент
            Location34_PositionForMita l = new Location34_PositionForMita();
            LoggerInstance.Msg($" Il2CppReferenceArray444");
            l.target = newPoint.transform;
            l.room = room;
            LoggerInstance.Msg($" Il2CppReferenceArray5");
            newArray[newArray.Length - 1] = l;

            Location34_Communication.positionsForMita = newArray;
            LoggerInstance.Msg($"End");
            
        }

        public Transform GetRandomLoc()
        {
            LoggerInstance.Msg("Before try Tring GetRandomLoc");
            try
            {
                LoggerInstance.Msg("Tring GetRandomLoc");
                // Проверяем, что список не пустой
                if (globalChildObjects == null || globalChildObjects.Count == 0)
                {
                    LoggerInstance.Error("globalChildObjects is null or empty!");
                    return null; // Возвращаем null, если список пуст
                }

                // Генерируем случайный индекс
                int randomIndex = UnityEngine.Random.Range(0, globalChildObjects.Count);

                // Получаем случайный объект по индексу
                GameObject randomObject = globalChildObjects[randomIndex];

                // Проверяем, что объект действительно существует и имеет компонент Transform
                if (randomObject == null)
                {
                    LoggerInstance.Error("Random object is null!");
                    return null; // Возвращаем null, если объект не найден
                }

                // Логируем имя объекта
                LoggerInstance.Msg($"Random object selected: {randomObject.name}");

                // Возвращаем компонент Transform
                return randomObject.transform;
            }
            catch (Exception)
            {
                LoggerInstance.Msg("Error with random loc");
                return null;
            }

        }
        bool dialogActive = false;
        private IEnumerator DisplayResponseAndEmotionCoroutine(string response)
        {
            while (dialogActive) { yield return null; }
            dialogActive = true;

            LoggerInstance.Msg("DisplayResponseAndEmotionCoroutine");
            int CountPathesWere = patches_to_sound_file.Count;
            // Пример кода, который будет выполняться на главном потоке
            yield return null; // Это нужно для того, чтобы выполнение произошло после завершения текущего кадра

            float elapsedTime = 0f; // Счетчик времени
            float timeout = 6.5f;     // Лимит времени ожидания


            // Ждем, пока patch_to_sound_file перестанет быть пустым или не истечет время ожидания
            while (string.IsNullOrEmpty(patch_to_sound_file) && elapsedTime < timeout) //&& waitForSounds=="1")
            {
                //LoggerInstance.Msg("DisplayResponseAndEmotionCicle");
                if (patches_to_sound_file.Count > CountPathesWere)
                {
                    patch_to_sound_file = patches_to_sound_file.Dequeue();
                    patches_to_sound_file.Clear();
                    break;
                }

                elapsedTime += Time.unscaledDeltaTime; // Увеличиваем счетчик времени
                yield return null;             // Пауза до следующего кадра
            }

            yield return null;
            // Если время ожидания истекло, можно выполнить какой-то fallback-лог
            if (string.IsNullOrEmpty(patch_to_sound_file))
            {
                LoggerInstance.Msg("Timeout reached, patch_to_sound_file is still empty.");
            }

            // После того как patch_to_sound_file стал не пустым, вызываем метод DisplayResponseAndEmotion
            DisplayResponseAndEmotion(response);

            dialogActive = false;
        }
        public static string PopLast(List<string> list)
        {
            if (list.Count == 0) throw new InvalidOperationException("List is empty");
            string last = list[^1]; // Или list[list.Count - 1]
            list.RemoveAt(list.Count - 1);
            return last;
        }
        private void DisplayResponseAndEmotion(string response)
        {
            LoggerInstance.Msg("DisplayResponseAndEmotion");

            TurnBlockInputField(true);
            try
            {

                string modifiedResponse = SetMovementStyle(response);

                AudioClip audioClip = null;

                if (!string.IsNullOrEmpty(patch_to_sound_file))
                {
                    try
                    {
                        LoggerInstance.Msg("patch_to_sound_file not null");
                        audioClip =NetworkController.LoadAudioClipFromFileAsync(patch_to_sound_file).Result;
                        patch_to_sound_file = "";
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Error($"Error loading audio file: {ex.Message}");
                    }
                }

                int delay = 3500 * modifiedResponse.Length / 50;

                MelonCoroutines.Start(PlayMitaSound(delay, audioClip));

                List<string> dialogueParts = SplitText(modifiedResponse, maxLength: 50);

                // Запуск диалогов последовательно, с использованием await или вложенных корутин
                MelonCoroutines.Start(ShowDialoguesSequentially(dialogueParts));
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in DisplayResponseAndEmotion: {ex.Message}");
            }
        }

        private IEnumerator ShowDialoguesSequentially(List<string> dialogueParts)
        {
            foreach (string part in dialogueParts)
            {
                LoggerInstance.Msg("foreach foreach " + part);

                string partCleaned = Regex.Replace(part, @"<[^>]+>.*?</[^>]+>", ""); // Очищаем от всех тегов
                int delay = Mathf.Max(3500 * partCleaned.Length / 50, 500);
                yield return MelonCoroutines.Start(ShowDialogue(part, delay));
            }

        }


        private IEnumerator ShowDialogue(string part, int delay)
        {

            LoggerInstance.Msg("ShowDialogue");

            string modifiedPart = part;
            List<string> commands;
            EmotionType emotion = EmotionType.none;
            try
            {
                LoggerInstance.Msg("Begin try:" + modifiedPart);
                modifiedPart = SetFaceStyle(modifiedPart);
                modifiedPart = MitaClothesModded.ProcessClothes(modifiedPart);
                modifiedPart = ProcessPlayerEffects(modifiedPart);
                modifiedPart = MitaAnimationModded.setAnimation(modifiedPart);
                modifiedPart = AudioControl.ProcessMusic(modifiedPart);
                (emotion, modifiedPart) = SetEmotionBasedOnResponse(modifiedPart);
                LoggerInstance.Msg("After SetEmotionBasedOnResponse " + modifiedPart);

                (commands, modifiedPart) = CommandProcessor.ExtractCommands(modifiedPart);
                if (commands.Count > 0)
                {
                    CommandProcessor.ProcessCommands(commands);
                }
                LoggerInstance.Msg("After ExtractCommands " + modifiedPart);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error processing part of response: {ex.Message}");
            }


            GameObject currentDialog = InstantiateDialog();

            Dialogue_3DText answer = currentDialog.GetComponent<Dialogue_3DText>();


            answer.textPrint = modifiedPart;
            answer.themeDialogue = Dialogue_3DText.Dialogue3DTheme.Mita;
            answer.timeShow = delay;
            answer.speaker = Mita?.gameObject;
            if (emotion != EmotionType.none) answer.emotionFinish = emotion;

            currentDialog.SetActive(true);
            yield return new WaitForSeconds(delay / 1000f);

            GameObject.Destroy(currentDialog);
            LoggerInstance.Msg("Dialogue part finished and destroyed.");

            TurnBlockInputField(false);
        }

        private IEnumerator PlayMitaSound(int delay, AudioClip audioClip)
        {
            LoggerInstance.Msg("PlayMitaSound");

            GameObject currentDialog = InstantiateDialog();
            currentDialog.SetActive(true);
            Dialogue_3DText answer = currentDialog.GetComponent<Dialogue_3DText>();

            // Если есть аудио, проигрываем его до начала текста
            if (audioClip != null)
            {
                LoggerInstance.Msg("Loading voice...");
                answer.timeSound = delay;
                answer.LoadVoice(audioClip);
                audioClip = null;
            }
            answer.speaker = Mita?.gameObject;

            yield return new WaitForSeconds(delay);

            GameObject.Destroy(currentDialog);
            LoggerInstance.Msg("Dialogue part finished and destroyed.");
        }

        private void PlayerTalk(string text)
        {
            GameObject currentDialog = null;

            try
            {

                int delay = 4000 * text.Length / 50;

                currentDialog = InstantiateDialog(false);
                if (currentDialog != null)
                {
                    Dialogue_3DText answer = currentDialog.GetComponent<Dialogue_3DText>();
                    if (answer == null)
                    {
                        throw new Exception("Dialogue_3DText component not found.");
                    }

                    answer.speaker = playerPerson.gameObject;
                    answer.textPrint = text;
                    answer.themeDialogue = Dialogue_3DText.Dialogue3DTheme.Player;
                    answer.timeShow = delay;

                    currentDialog.SetActive(true);

                    MitaBoringtimer = 0f;
                }
                else
                {
                    LoggerInstance.Msg("currentDialog is null.");
                }

            }
            catch (Exception ex)
            {
                LoggerInstance.Msg($"PlayerTalk: {ex.Message}");
            }
            finally
            {
                if (currentDialog != null)
                {

                }
            }
        }

        public void beginHunt()
        {
            try
            {
                LoggerInstance.Msg("beginHunt ");
                mitaState = MitaState.hunt;
                MelonCoroutines.Start(hunting());
                Location34_Communication.ActivationCanWalk(false);
                MitaPersonObject.GetComponent<Animator_FunctionsOverride>().AnimationClipWalk(AssetBundleLoader.LoadAnimationClipByName(bundle, "Mita RunWalkKnife")); //
                knife.SetActive(true);
                knife.transform.rotation = Quaternion.identity;
                MitaAnimationModded.EnqueueAnimation("Mita TakeKnife_0");
            }
            catch (Exception ex)
            {

                LoggerInstance.Error("beginHunt " + ex);
            }
            
        }
        IEnumerator hunting()
        {
            float startTime = Time.time; // Запоминаем время старта корутины
            float lastMessageTime = -45f; // Чтобы сообщение появилось сразу через 15 секунд

            yield return new WaitForSeconds(1f);

            while (mitaState == MitaState.hunt)
            {
                if (getDistance() > 1f)
                {
                    Mita.AiWalkToTarget(playerPerson.transform);
                }
                else
                {
                    try
                    {
                        MelonCoroutines.Start(ActivateAndDisableKiller(3));
                    }
                    catch (Exception)
                    {
                    }

                    yield break;
                }

                // Вычисляем время с начала корутины
                float elapsedTime = Time.time - startTime;

                // Каждые 15 секунд вызываем функцию (если прошло время)
                if (elapsedTime - lastMessageTime >= 45f)
                {
                    string message = $"Игрок жив уже {elapsedTime.ToString("F2")} секунд. Скажи что-нибудь короткое. ";
                    if (Mathf.FloorToInt(elapsedTime) % 60 == 0) message += "Может быть, пора усложнять игру... (Менять скорости или спавнить манекенов или применять эффекты)";
                    sendSystemMessage(message);

                    lastMessageTime = elapsedTime; // Обновляем время последнего вызова
                }

                yield return new WaitForSeconds(0.5f);
            }

        }

        public void endHunt()
        {
            //MitaPersonObject.GetComponent<Animator_FunctionsOverride>().AnimationClipWalk(AssetBundleLoader.LoadAnimationClipByName(bundle, "Mita Walk"));
            knife.SetActive(false);
            movementStyle = MovementStyles.walkNear;
            Location34_Communication.ActivationCanWalk(true);
            mitaState = MitaState.normal;
            Mita.AiShraplyStop();
        }
        

        public void spawnManeken()
        {
            GameObject someManeken = GameObject.Instantiate(ManekenTemplate, worldHouse.Find("House"));
            someManeken.SetActive(true);
            activeMakens.Add(someManeken);





            if (manekenGame == false)
            {
                manekenGame = true;
                MelonCoroutines.Start(CheckManekenGame());
            }
            someManeken.transform.SetPositionAndRotation(GetRandomLoc().position, GetRandomLoc().rotation);


        }
        public void TurnAllMenekens(bool on)
        {
            if (activeMakens.Count <= 0) return;

            foreach (GameObject m in activeMakens)
            {
                if (on) m.transform.FindChild("MitaManeken 1").gameObject.GetComponent<Mob_Maneken>().ResetManeken();
                else m.transform.FindChild("MitaManeken 1").gameObject.GetComponent<Mob_Maneken>().DeactivationManeken();

            }
            manekenGame = on;
        }
        public void removeAllMenekens()
        {
            foreach (GameObject m in activeMakens)
            {
                GameObject.Destroy(m);
            }
            activeMakens.Clear();
            manekenGame = false;
        }

        // Корутин для активации и деактивации объекта
        public IEnumerator ActivateAndDisableKiller(float delay)
        {
            

            if (AnimationKiller.transform.Find("PositionsKill").childCount > 0)
            {
                AnimationKiller.transform.Find("PositionsKill").GetChild(0).SetPositionAndRotation(playerPerson.transform.position, playerPerson.transform.rotation);
            }
            AnimationKiller.SetActive(true); // Включаем объект


            // Сохраняем исходную позицию и ориентацию Миты
            Vector3 originalPosition = Mita.transform.position;
            Quaternion originalRotation = Mita.transform.rotation;
            Mita.transform.SetPositionAndRotation(new Vector3(500, 500, 500), Quaternion.identity);
            yield return new WaitForSeconds(0.1f);
            AnimationKiller.GetComponent<Location6_MitaKiller>().Kill(); // Вызываем метод Kill()
            endHunt();
            yield return new WaitForSeconds(3f);
            systemMessages.Enqueue("Ты успешно зарезала игрока и он где-то зареспавнился");
            AnimationKiller.SetActive(false); // Включаем объект
            // Возвращаем Миту в исходное положение
            TryTurnChild(worldHouse, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Bedroom", true);
            Mita.transform.SetPositionAndRotation(originalPosition, originalRotation);
            Mita.AiShraplyStop();
        }




        private List<string> SplitText(string text, int maxLength)
        {
            List<string> parts = new List<string>();
            Dictionary<string, string> placeholders = new Dictionary<string, string>();

            // Регулярное выражение для поиска служебных частей
            string pattern = @"<.*?>.*?</.*?>";
            int placeholderCounter = 0;

            // Заменяем служебные части на уникальные маркеры
            string processedText = Regex.Replace(text, pattern, match =>
            {
                string placeholder = $"@@{placeholderCounter}@@";
                placeholders[placeholder] = match.Value; // Сохраняем оригинальный текст
                placeholderCounter++;
                return placeholder;
            });

            // Разделяем по строкам
            string[] lines = processedText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                // Если длина строки меньше maxLength, добавляем её сразу
                if (line.Length <= maxLength)
                {
                    parts.Add(line.Trim());
                }
                else
                {
                    // Разделяем на предложения
                    string[] sentences = line.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                    string currentPart = "";

                    foreach (string sentence in sentences)
                    {
                        string trimmedSentence = sentence.Trim();
                        if ((currentPart.Length + trimmedSentence.Length + 1) <= maxLength)
                        {
                            currentPart += (currentPart.Length > 0 ? " " : "") + trimmedSentence + ".";
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(currentPart)) parts.Add(currentPart.Trim());
                            currentPart = trimmedSentence + ".";
                        }
                    }

                    // Добавляем оставшийся текст
                    if (!string.IsNullOrWhiteSpace(currentPart)) parts.Add(currentPart.Trim());
                }
            }

            // Восстанавливаем служебные части
            for (int i = 0; i < parts.Count; i++)
            {
                foreach (var placeholder in placeholders)
                {
                    parts[i] = parts[i].Replace(placeholder.Key, placeholder.Value);
                }
            }

            return parts;
        }





        private GameObject InstantiateDialog(bool Mita = true)
        {
            Transform world = GameObject.Find("World")?.transform;
            if (world == null)
            {
                LoggerInstance.Msg("World object not found.");
                return null;
            }
            if (Mita)
            {
                return GameObject.Instantiate(CustomDialog, world.Find("Quests/Quest 1/Dialogues"));
            }
            else
            {
                return GameObject.Instantiate(CustomDialogPlayer, world.Find("Quests/Quest 1/Dialogues"));
            }
        }

        private (EmotionType, string) SetEmotionBasedOnResponse(string response)
        {
            LoggerInstance.Msg($"Inside SetEmotionBasedOnResponse: " + response);
            try
            {
                var (emotions, cleanedResponse) = ExtractEmotions(response);
                EmotionType emotion = EmotionType.none;
                if (emotions.Count > 0)
                {
                    Enum.TryParse<EmotionType>(emotions[0], true, out var result);

                    Mita.FaceEmotionType(result);
                    if (emotions.Count > 1)
                    {
                        Enum.TryParse<EmotionType>(emotions[1], true, out var result2);
                        emotion = result2;
                    }

                }
                LoggerInstance.Msg($"Inside SetEmotionBasedOnResponse end: " + cleanedResponse);
                return (emotion, cleanedResponse);
            }
            catch (Exception ex)
            {
                LoggerInstance.Msg($"Error extracting emotion: {ex.Message}");
                //Mita.FaceEmotionType(GetRandomEmotion());
                return (EmotionType.none, response); // Если произошла ошибка, возвращаем исходный текст
            }
        }


        public static (List<string>, string) ExtractEmotions(string response)
        {

            MelonLogger.Msg($"Inside ExtractEmotions start: " + response);
            List<string> emotions = new List<string>();
            string pattern = @"<e>(.*?)</e>";
            MatchCollection matches = Regex.Matches(response, pattern);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    emotions.Add(match.Groups[1].Value);
                }
            }

            // Удаляем теги эмоций из текста
            string cleanedResponse = Regex.Replace(response, @"<e>.*?</e>", "");

            MelonLogger.Msg($"Inside ExtractEmotions end: " + cleanedResponse);
            return (emotions, cleanedResponse);

        }
        public string SetFaceStyle(string response)
        {

            // Регулярное выражение для извлечения эмоций
            string pattern = @"<f>(.*?)</f>";
            Match match = Regex.Match(response, pattern);

            string faceStyle = string.Empty;
            string cleanedResponse = Regex.Replace(response, @"<f>.*?</f>", ""); // Очищаем от всех тегов

            if (match.Success)
            {
                // Если эмоция найдена, устанавливаем её в переменную faceStyle
                faceStyle = match.Groups[1].Value;
            }

            try
            {
                // Проверка на наличие объекта Mita перед применением эмоции
                if (Mita == null || Mita.gameObject == null)
                {
                    LoggerInstance.Error("Mita object is null or Mita.gameObject is not active.");
                    return cleanedResponse; // Возвращаем faceStyle и очищенный текст
                }

                // Устанавливаем лицо, если оно найдено
                switch (faceStyle)
                {
                    case "Смущаться":
                        //Mita.FaceColorUpdate();
                        Mita.FaceLayer(1);
                        break;
                    case "Маска грусти":
                        //Mita.FaceColorUpdate();
                        Mita.FaceLayer(2);
                        break;
                    default:
                        //Mita.FaceColorUpdate();
                        Mita.FaceLayer(0);
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Problem with FaceStyle: {ex.Message}");
            }

            // Возвращаем кортеж: лицо и очищенный текст
            return cleanedResponse;
        }

        public string SetMovementStyle(string response)
        {
            // Регулярное выражение для извлечения эмоций
            string pattern = @"<m>(.*?)</m>";
            Match match = Regex.Match(response, pattern);

            string MovementStyle = string.Empty;
            string cleanedResponse = Regex.Replace(response, @"<m>.*?</m>", ""); // Очищаем от всех тегов

            if (match.Success)
            {
                // Если эмоция найдена, устанавливаем её в переменную faceStyle
                MovementStyle = match.Groups[1].Value;
            }
            try
            {
                // Проверка на наличие объекта Mita перед применением эмоции
                if (Mita == null || Mita.gameObject == null)
                {
                    LoggerInstance.Error("Mita object is null or Mita.gameObject is not active.");
                    return cleanedResponse; // Возвращаем faceStyle и очищенный текст
                }
                // Устанавливаем лицо, если оно найдено
                switch (MovementStyle)
                {
                    case "Следовать рядом с игроком":
                        movementStyle = MovementStyles.walkNear;
                        Location34_Communication.ActivationCanWalk(true);
                        break;
                    case "Следовать за игроком":
                        movementStyle = MovementStyles.follow;
                        Location34_Communication.ActivationCanWalk(false);
                        MelonCoroutines.Start(FollowPlayer());
                        break;
                    case "Стоять на месте":
                        movementStyle = MovementStyles.stay;
                        Location34_Communication.ActivationCanWalk(false);
                        break;
                    case "NoClip":
                        movementStyle = MovementStyles.noclip;
                        Location34_Communication.ActivationCanWalk(false);
                        MelonCoroutines.Start(FollowPlayerNoclip());
                        break;
                    default:
                        //Mita.FaceColorUpdate();
                        //Mita.FaceLayer(0);
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Problem with SetMovementStyle: {ex.Message}");
            }

            // Возвращаем кортеж: лицо и очищенный текст
            return cleanedResponse;
        }
        

        public IEnumerator FollowPlayer()
        {

            while (movementStyle == MovementStyles.follow)
            {
                if (getDistance() > 1f)
                {
                    Mita.AiWalkToTarget(playerPerson.transform);
                    
                }
                else
                {
                    Mita.AiShraplyStop();
                    yield break;
                }

                yield return new WaitForSeconds(0.25f);
            }


        }
        public IEnumerator FollowPlayerNoclip()
        {

            while (movementStyle == MovementStyles.noclip && getDistance() > 0.9f)
            {

                MoveToPositionNoClip(5);

                yield return new WaitForSeconds(2f);
            }


        }
        private IEnumerator MoveToPositionNoClip(float speed)
        {
            while (movementStyle == MovementStyles.noclip && getDistance() > 0.9f)
            {
                Vector3 targetPosition = playerPerson.gameObject.transform.position;
                // Двигаем персонажа напрямую к цели (без учета препятствий)
                Mita.transform.position = Vector3.MoveTowards(Mita.transform.position, targetPosition, speed * Time.deltaTime);

                // Можно добавить поворот персонажа в направлении движения (опционально)
                Vector3 direction = (targetPosition - Mita.transform.position).normalized;
                if (direction != Vector3.zero)
                    Mita.transform.rotation = Quaternion.LookRotation(direction);

                yield return null; // Ждем следующий кадр
            }

            // Когда достигли цели
            Debug.Log("NoClip movement completed!");
        }

        private string ProcessPlayerEffects(string response)
        {
            LoggerInstance.Msg("Starting ProcessPlayerEffects...");

            List<string> effects = new List<string>();
            string pattern = @"<v>(.*?)</v>";
            MatchCollection matches = Regex.Matches(response, pattern);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    effects.Add(match.Groups[1].Value);
                    LoggerInstance.Msg($"Found effect tag: {match.Groups[1].Value}");
                }
            }

            string result = Regex.Replace(response, @"<v>.*?</v>", "");
            LoggerInstance.Msg("Removed effect tags from response.");

            try
            {
                foreach (string effectAndTime in effects)
                {
                    LoggerInstance.Msg($"Processing effect and time: {effectAndTime}");

                    string[] parts = effectAndTime.Split(',');
                    string effect = "";
                    float time = 1f;

                    if (parts.Length == 2 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out time))
                    {
                        effect = parts[0];
                        LoggerInstance.Msg($"Parsed effect: {effect}, time: {time}");
                    }
                    else
                    {
                        LoggerInstance.Msg($"Invalid format for effect and time: {effectAndTime}");
                        continue;
                    }

                    Component effectComponent = null;
                    try
                    {
                        LoggerInstance.Msg($"Attempting to find component for effect: {effect}");
                        switch (effect.ToLower())
                        {
                            case "глитч":
                                effectComponent = playerEffectsObject.GetComponentByName("Glitch");
                                break;
                            case "телемост":
                                playerEffects.EffectDatamosh(true);
                                MelonCoroutines.Start(DisableEffectAfterDelay(playerEffects, "EffectDatamosh", time)); // Запускаем корутину для выключения эффекта
                                break;
                            case "тв-удар":
                                playerEffects.EffectClickTelevision();
                                break;
                            case "помехи":
                                effectComponent = playerEffectsObject.GetComponentByName("Noise");
                                break;
                            case "негатив":
                                effectComponent = playerEffectsObject.GetComponentByName("Negative");
                                break;
                            case "кровь":
                                effectComponent = playerEffectsObject.GetComponentByName("FastVignette");
                                break;
                            default:
                                LoggerInstance.Msg($"Unknown effect: {effect}");
                                continue;
                        }

                        if (effectComponent != null)
                        {
                            if (effectComponent is MonoBehaviour monoBehaviourComponent)
                            {
                                // Если это стандартный MonoBehaviour
                                monoBehaviourComponent.enabled = true; // Включаем компонент
                                MelonCoroutines.Start(ToggleComponentAfterTime(monoBehaviourComponent, time)); // Запускаем корутину
                            }
                            else if (effectComponent is Il2CppObjectBase il2cppComponent)
                            {
                                // Если это Il2CppObjectBase
                                LoggerInstance.Msg($"Il2Cpp component detected: {il2cppComponent.GetType().Name}");

                                // Проверяем, имеет ли компонент свойство enabled
                                var enabledProperty = il2cppComponent.GetType().GetProperty("enabled");
                                var behaviour = il2cppComponent.TryCast<Behaviour>();
                                behaviour.enabled = true;

                                // Запускаем корутину, передавая Il2Cpp-компонент
                                MelonCoroutines.Start(HandleIl2CppComponent(il2cppComponent, time));

                            }
                            else
                            {
                                LoggerInstance.Warning($"Component {effectComponent?.GetType().Name} is not a MonoBehaviour or Il2CppObjectBase.");
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Msg($"Error processing effect '{effect}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Msg($"Error in ProcessPlayerEffects: {ex.Message}");
            }

            LoggerInstance.Msg("Finished ProcessPlayerEffects.");
            return result;
        }

        // Корутинa для переключения активности компоненты
        public IEnumerator ToggleComponentAfterTime(Component component, float delay)
        {
            LoggerInstance.Msg($"Starting ToggleComponentAfterTime for {component?.GetType().Name} with delay {delay}...");

            if (component == null)
            {
                LoggerInstance.Msg("Component is null. Cannot toggle.");
                yield break;
            }

            if (component is MonoBehaviour monoBehaviourComponent)
            {
                yield return new WaitForSeconds(delay);

                monoBehaviourComponent.enabled = !monoBehaviourComponent.enabled;
                LoggerInstance.Msg($"{monoBehaviourComponent.GetType().Name} is now {(monoBehaviourComponent.enabled ? "enabled" : "disabled")}");
            }
            else if (component is Il2CppObjectBase il2cppComponent)
            {
                LoggerInstance.Msg($"Detected Il2Cpp component: {il2cppComponent.GetType().Name}");

                // Проверяем, есть ли у компонента свойство enabled
                var enabledProperty = il2cppComponent.GetType().GetProperty("enabled");
                if (enabledProperty != null && enabledProperty.PropertyType == typeof(bool))
                {
                    // Читаем текущее значение свойства
                    bool isEnabled = (bool)enabledProperty.GetValue(il2cppComponent);
                    LoggerInstance.Msg($"Current enabled state of {il2cppComponent.GetType().Name}: {isEnabled}");

                    yield return new WaitForSeconds(delay);

                    // Переключаем значение
                    enabledProperty.SetValue(il2cppComponent, !isEnabled);
                    LoggerInstance.Msg($"{il2cppComponent.GetType().Name} is now {(!isEnabled ? "enabled" : "disabled")}");
                }
                else
                {
                    LoggerInstance.Warning($"The component {il2cppComponent.GetType().Name} does not have an 'enabled' property.");
                }
            }
            else
            {
                LoggerInstance.Warning($"Component {component?.GetType().Name} is not a MonoBehaviour or Il2CppObjectBase. Cannot toggle.");
            }

            LoggerInstance.Msg($"Finished ToggleComponentAfterTime for {component?.GetType().Name}.");
        }

        public IEnumerator HandleIl2CppComponent(Il2CppObjectBase il2cppComponent, float delay)
        {
            LoggerInstance.Msg($"Starting HandleIl2CppComponent for {il2cppComponent?.GetType().Name} with delay {delay}...");

            if (il2cppComponent == null)
            {
                LoggerInstance.Msg("Il2CppComponent is null. Cannot toggle.");
                yield break;
            }

            // Пробуем преобразовать объект в Behaviour
            var behaviour = il2cppComponent.TryCast<Behaviour>();
            if (behaviour != null)
            {
                LoggerInstance.Msg($"Il2Cpp Behaviour detected: {behaviour.GetType().Name}");

                // Читаем текущее состояние и переключаем
                bool currentState = behaviour.enabled;
                LoggerInstance.Msg($"Current enabled state of {behaviour.GetType().Name}: {currentState}");

                yield return new WaitForSeconds(delay);

                behaviour.enabled = !currentState;
                LoggerInstance.Msg($"{behaviour.GetType().Name} is now {(behaviour.enabled ? "enabled" : "disabled")}");
            }
            else
            {
                LoggerInstance.Warning($"The Il2Cpp component {il2cppComponent.GetType().Name} is not a Behaviour or does not support 'enabled' property.");
            }

            LoggerInstance.Msg($"Finished HandleIl2CppComponent for {il2cppComponent?.GetType().Name}.");
        }

        private IEnumerator DisableEffectAfterDelay(Il2CppObjectBase il2cppComponent, string effectMethodName, float delay)
        {
            yield return new WaitForSeconds(delay); // Ожидаем заданное время

            // Получаем тип компонента Il2Cpp
            var componentType = il2cppComponent.GetType();

            // Используем метод GetMethod для получения метода с именем effectMethodName
            var method = componentType.GetMethod(effectMethodName);

            if (method != null)
            {
                // Передаём параметр false для выключения эффекта
                method.Invoke(il2cppComponent, new object[] { false });
                LoggerInstance.Msg($"Effect {effectMethodName} has been disabled after {delay} seconds.");
            }
            else
            {
                LoggerInstance.Warning($"Effect method {effectMethodName} not found on Il2Cpp component.");
            }
        }

        private EmotionType GetRandomEmotion()
        {
            EmotionType[] emotions = (EmotionType[])Enum.GetValues(typeof(EmotionType));
            int randomIndex = UnityEngine.Random.Range(0, emotions.Length);
            return emotions[randomIndex];
        }

       
        public string formCurrentInfo()
        {

            string info = "-";
            try
            {
                info += $"Current movement type: {movementStyle.ToString()}\n";
                
                if (mitaState == MitaState.hunt) info += $"You are hunting player with knife:\n";

                info += $"Your size: {MitaPersonObject.transform.localScale.x}\n";
                info += $"Your speed: {MitaPersonObject.GetComponent<NavMeshAgent>().speed}\n";

                info += $"Player size: {playerObject.transform.localScale.x}\n";
                info += $"Player speed: {playerObject.GetComponent<PlayerMove>().speedPlayer}\n";


                if (false)
                    {
                    info += $"Game house time (%): {location21_World.dayNow}\n";
                    info += $"Current lighing color: {location21_World.timeDay.colorDay}\n";
                    }
                
                if (activeMakens.Count>0) info = info + $"Menekens count: {activeMakens.Count}\n";
                info += $"Current music: {AudioControl.getCurrrentMusic()}\n";
                info += $"Your clothes: {MitaClothesModded.currentClothes}\n";

                if (PlayerAnimationModded.currentPlayerMovement == PlayerAnimationModded.PlayerMovement.sit) info += $"Player is sitting";
                
                info += Interactions.getObservedObjects();


            }
            catch (Exception ex)
            {

                LoggerInstance.Msg(ex);
            }
            return info;
        }

       


        // Ввод

        private bool isInputBlocked = false; // Флаг для блокировки
        private static GameObject InputFieldComponent;

        public override void OnUpdate()
        {


            // Обрабатываем нажатие Tab для переключения InputField
            if (Input.GetKeyDown(KeyCode.Tab)) // Используем GetKeyDown для одноразового срабатывания
            {
                if (InputFieldComponent == null)
                {
                    try
                    {
                        CreateInputComponent();
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Msg("CreateInputComponent ex:" + ex);
                    }
                }
                else
                {

                    if (isInputBlocked) return;

                    // Переключаем видимость InputField
                    bool isActive = InputFieldComponent.activeSelf;
                    InputFieldComponent.SetActive(!isActive);

                    // Если объект стал активным, активируем InputField
                    if (InputFieldComponent.activeSelf)
                    {
                        var ifc = InputFieldComponent.GetComponent<InputField>();
                        if (ifc != null)
                        {
                            ifc.Select();
                            ifc.ActivateInputField();
                        }
                    }
                }
            }

            // Обрабатываем нажатие Enter для передачи текста в функцию
            else if (Input.GetKeyDown(KeyCode.Return) && InputFieldComponent != null)
            {
                var ifc = InputFieldComponent.GetComponent<InputField>();
                if (ifc.text != "")
                {
                    ProcessInput(ifc.text); // Пустышка для обработки текста
                    ifc.text = "";
                }
            }


            // Обрабатываем нажатие Enter для передачи текста в функцию
            else if (Input.GetKeyDown(KeyCode.C) && !InputFieldComponent.activeSelf)
            {
                playerPerson.transform.parent.GetComponent<PlayerMove>().canSit = true;
            }
            else if (Input.GetKeyUp(KeyCode.C))
            {
                playerPerson.transform.parent.GetComponent<PlayerMove>().canSit = false;
            }
            else if (Input.GetKeyDown(KeyCode.Y))
            {
                if (PlayerAnimationModded.currentPlayerMovement==PlayerAnimationModded.PlayerMovement.sit) PlayerAnimationModded.stopAnim();
                PlayerAnimationModded.currentPlayerMovement = PlayerAnimationModded.PlayerMovement.normal;
            }

        }
        private void TurnBlockInputField(bool blocked)
        {
            isInputBlocked = blocked; // Устанавливаем блокировку
            if (InputFieldComponent != null)
            {
                InputFieldComponent.SetActive(!blocked); // Отключаем поле ввода, если оно активно
            }

        }

        private void CreateInputComponent()
        {


            // Создаем объект InputField
            InputFieldComponent = new GameObject("InputFieldComponent");

            var ifc = InputFieldComponent.AddComponent<InputField>();
            var _interface = GameObject.Find("Interface");
            if (_interface == null) return;

            InputFieldComponent.transform.parent = _interface.transform;


            var rect = InputFieldComponent.AddComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;

            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);



            var image = InputFieldComponent.AddComponent<UnityEngine.UI.Image>();
            /*            try
                        {
                            var KeyRun = _interface.transform.Find("GameController/Interface/SubtitlesFrame/Text 2").GetComponent<UnityEngine.UI.Image>();
                            LoggerInstance.Msg("KeyRun");
                            image.sprite = KeyRun.sprite;
                        }
                        catch (Exception ex)
                        {*/
            Sprite blackSprite = CreateBlackSprite(100, 100);
            image.sprite = blackSprite;
            //}

            image.color = new Color(0f, 0f, 0f, 0.7f);
            ifc.image = image;


            var TextLegacy = new GameObject("TextLegacy");
            var textComponent = TextLegacy.AddComponent<Text>();
            TextLegacy.transform.parent = InputFieldComponent.transform;
            var rectText = TextLegacy.GetComponent<RectTransform>();
            rectText.sizeDelta = new Vector2(500, 100);
            rectText.anchoredPosition = Vector2.zero;
            var texts = GameObject.FindObjectsOfType<Text>();



            foreach (var text in texts)
            {
                textComponent.font = text.font;
                textComponent.fontStyle = text.fontStyle;
                textComponent.fontSize = 35;
                if (textComponent.font != null) break;
            }


            var textInputField = InputFieldComponent.GetComponent<InputField>();
            textInputField.textComponent = TextLegacy.GetComponent<Text>();
            textInputField.text = "Введи текст";
            textInputField.textComponent.color = Color.yellow;
            textInputField.textComponent.alignment = TextAnchor.MiddleCenter;



            // Устанавливаем 70% ширины от родителя
            RectTransform parentRect = _interface.GetComponent<RectTransform>();
            float parentWidth = parentRect.rect.width;
            rect.sizeDelta = new Vector2(parentWidth * 0.7f, rect.sizeDelta.y);
            rectText.sizeDelta = rect.sizeDelta;
            textInputField.Select();
            textInputField.ActivateInputField();

        }


        bool Test = false;
        // Пустышка для обработки ввода
        private void ProcessInput(string inputText)
        {
            LoggerInstance.Msg("Input received: " + inputText);
            PlayerTalk(inputText);
            playerMessage += $"{inputText}\n";

            //PlayerAnimationModded.EnqueueAnimation(inputText);

            //PlayMitaAnim(inputText);
            //MitaAnimation.EnqueueAnimation(inputText);
            //MitaAnimationModded.EnqueueAnimation("Mita StartShow Knifes");
            //MitaAnimationModded.EnqueueAnimation("Mita Throw Knifes");
            //MitaAnimationModded.EnqueueAnimation("Mita StartDisappointment");
            //MitaAnimationModded.EnqueueAnimation("Mita Hide 2");
            //MitaAnimationModded.EnqueueAnimation("Mita StartDisappointment");

            //PlayMitaAnim("Mita StartShow Knifes");
            //PlayMitaAnim("Mita Click_1");
            //PlayMitaAnim("Mita Click_2");
            //PlayMitaAnim("Mita ThrowPlayer");
            //List<AnimationClip> r = Test2();
            // LoggerInstance.Msg(r[0].name);
            // AnimationClip randimAnim = r[UnityEngine.Random.Range(0, r.Count)];
            //LoggerInstance.Msg(randimAnim.name);
            // Log the start of the operation


        }
        public Sprite CreateBlackSprite(int width, int height)
        {
            // Создаем текстуру с заданными размерами
            Texture2D texture = new Texture2D(width, height);

            // Задаем все пиксели как черные
            Color darkColor = new Color(0f, 0f, 0f, 0f);  // Черный цвет
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    texture.SetPixel(x, y, darkColor);
                }
            }

            // Применяем изменения
            texture.Apply();

            // Создаем и возвращаем спрайт из текстуры
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }




        // Метод для обработки события


    }



}
