using Il2Cpp;
using MelonLoader;
using System.Collections;
using UnityEngine;
using System.Text;
using MitaAI.WorldModded;
using Il2CppEPOOutline;
using UnityEngine.Events;

using UnityEngine.UI;
using UnityEngine.Bindings;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using MitaAI.Mita;


namespace MitaAI
{
    public static class Interactions
    {
        public static GameObject tipTemplate;

        public static GameObject InteractionContainer;

        public static GameObject GetInteractionContainer()
        {
            if (InteractionContainer == null)
            {
                InteractionContainer = new GameObject("InteractionContainer");
                InteractionContainer.transform.parent = MitaCore.worldHouse;
            }
            return InteractionContainer;
        }

        public static void init()
        {
            tipTemplate = GameObject.Instantiate(GameObject.Find("Addon/Interactive Aihastion/Canvas"));
            tipTemplate.active = false;


            init_interactions();
        }
        public static void init_interactions() { 
            

            for (int i = 1; i < 5; i++)
            {

                try
                {
                    var chair = MitaCore.worldHouse.Find($"House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Kitchen/Kitchen Chair {i}"); 
                    var comp = chair.gameObject.AddComponent<Chair>();

                    var oam1 = ObjectAnimationMita.Create(chair.gameObject, $"Kitchen Chair {i} sit", "Сесть за стул", freeCase:(UnityAction)comp.moveChair);
                    oam1.setStartPos(new Vector3(-0.1f, 0.6f, -0.1f), new Vector3(90, 0, 0));
                    oam1.setFinalPos(Vector3.zero, new Vector3(90, 0, 0));
                    // oam.addMoveRotateAction(new Vector3(0.4f, 0, 0f), Quaternion.Euler(0, 0, 0));

                    oam1.setIdleAnimation("Mita SitIdle");
                    oam1.addEnqueAnimationAction("Mita SitIdle");
                    oam1.setRevertAOM($"Kitchen Chair {i} stand up", "Слезть со стула", NeedMovingToIdle: true);



                    var chairAP = PlayerAnimationModded.CopyObjectAmimationPlayerTo(chair, "Interactive SitOnChair");
                    chairAP.transform.localEulerAngles = new Vector3(0, 270, 270);
                    chairAP.transform.localPosition = new Vector3(0.5f, 0.2f, 0);

                    
                    oam1.addSimpleAction((UnityAction)comp.moveChair);

                    MelonLogger.Msg("Before FindOrCreateObjectInteractable");
                    var obj = Interactions.FindOrCreateObjectInteractable(chairAP.gameObject, false, 1, "Сесть", false, useParent: true,CanvasPosition:new Vector3(0,0.9f,0.5f));
                    obj.eventClick.AddListener((UnityAction)comp.moveChair);
                    obj.eventClick.AddListener((UnityAction)Hints.createExitButton);
                }
                catch (Exception ex)
                {

                    MelonLogger.Error(ex);
                }

            }


            var sofa = MitaCore.worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/Sofa");
            var oam = ObjectAnimationMita.Create(sofa.gameObject, "Hall Sofa sit right", "Сесть на диван справа ближе к туалету", position: "right");
            oam.setStartPos(new Vector3(-0.9f, 0.6f, 0), new Vector3(270, 180, 0));
            oam.setAiMovePoint(new Vector3(0, 0, 0.5f), new Vector3(0, 0, 0));
            oam.setIdleAnimation("Mita SitIdle");
            oam.addEnqueAnimationAction("Mita SitIdle");
            oam.setRevertAOM("Hall Sofa stand up", "Слезть с дивана");

            //var sofaAP = PlayerAnimationModded.CopyObjectAmimationPlayerTo(sofa, "AnimationPlayer Sit", "right");
            ObjectInteractive objSofa;
            //if (sofaAP != null)
            //{
            //    sofaAP.transform.localEulerAngles = new Vector3(90, 0, 0);
            //    sofaAP.transform.localPosition = new Vector3(-0.8f, 1.4f, 0);
            //    objSofa = Interactions.FindOrCreateObjectInteractable(sofaAP.gameObject, false, 5, "Сесть на диван", false, useParent: false,boxSize:Vector3.one*0.3f);
            //    objSofa.eventClick.AddListener((UnityAction)sofaAP.AnimationPlay);
            //}


            //"AnimationPlayer Sit"

            sofa = MitaCore.worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/Sofa");
            oam = ObjectAnimationMita.Create(sofa.gameObject, "Hall Sofa sit center", "Сесть на диван по центру");
            oam.setStartPos(new Vector3(0f, 0.6f, 0), new Vector3(270, 180, 0));
            oam.setAiMovePoint(new Vector3(0, 0, 0.5f), new Vector3(0, 0, 0));
            oam.setIdleAnimation("Mita SitIdle");
            oam.addEnqueAnimationAction("Mita SitIdle");
            oam.setRevertAOM("Hall Sofa stand up", "Слезть с дивана");


            sofa = MitaCore.worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/Sofa");
            oam = ObjectAnimationMita.Create(sofa.gameObject, "Hall Sofa sit left", "Сесть на слева ближе к кухне", position: "left");
            oam.setStartPos(new Vector3(0.8f, 0.6f, 0), new Vector3(270, 180, 0));
            oam.setAiMovePoint(new Vector3(0, 0, 0.5f), new Vector3(0, 0, 0));
            oam.setIdleAnimation("Mita SitIdle");
            oam.addEnqueAnimationAction("Mita SitIdle");
            oam.setRevertAOM("Hall Sofa stand up", "Слезть с дивана");

           
            var sofaAP = PlayerAnimationModded.CopyObjectAmimationPlayerTo(sofa, "AnimationPlayer Sit", "left");
            if (sofaAP != null)
            {
                sofaAP.transform.localEulerAngles = new Vector3(90, 0, 0);
                sofaAP.transform.localPosition = new Vector3(0.8f, 1.4f, 0);
                objSofa = Interactions.FindOrCreateObjectInteractable(sofaAP.gameObject, false, 1, "Усесться", false, useParent: true);
                objSofa.eventClick.AddListener((UnityAction)sofaAP.AnimationPlay);
            }


            var TrashBox = MitaCore.worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Kitchen/TrashBox");
            oam = ObjectAnimationMita.Create(TrashBox.gameObject, "Kitchen TrashBox sit");
            oam.setIdleAnimation("Mita SitIdle");
            oam.addEnqueAnimationAction("Mita SitIdle");
            oam.setStartPos(new Vector3(0f, 0.0f, 0.1f), new Vector3(90, 0, 0));
            oam.setRevertAOM("TrashBox stend up", "Встать c мусорки");

            var Taburetka = MitaCore.worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Kitchen/Kitchen Stool 1");
            oam = ObjectAnimationMita.Create(Taburetka.gameObject, "Kitchen Stool sit", "Сесть на табуретку у окна");
            oam.setIdleAnimation("Mita SitIdle");
            oam.addEnqueAnimationAction("Mita SitIdle");
            oam.setStartPos(new Vector3(0, 0.0f, 0.1f), new Vector3(270, 180, 0));
            oam.setRevertAOM("Kitchen Stool stend up", "Встать c мусорки");


            var CornerSofa = MitaCore.worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/CornerSofa");
            oam = ObjectAnimationMita.Create(CornerSofa.gameObject, "CornerSofa Hall sit");
            oam.setAiMovePoint(new Vector3(0f, 0.0f, 0.6f));
            oam.setStartPos(new Vector3(0f, 0.25f, 0f), new Vector3(270, 180, 0));
            oam.setIdleAnimation("Mita SitIdle");
            oam.addEnqueAnimationAction("Mita SitIdle");
            oam.setRevertAOM("CornerSofa stend up", "Встать с углового дивана");

            CornerSofa = MitaCore.worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/CornerSofa");
            oam = ObjectAnimationMita.Create(CornerSofa.gameObject, "CornerSofa Hall sit right", "Сесть на угловой диван справа к лампе", position: "right");
            oam.setAiMovePoint(new Vector3(-0.6f, 0.0f, 0.6f));
            oam.setStartPos(new Vector3(0f, 0.25f, 0f), new Vector3(270, 180, 0));
            oam.setIdleAnimation("Mita SitIdle");
            oam.addEnqueAnimationAction("Mita SitIdle");
            oam.setRevertAOM("CornerSofa stend up", "Встать с углового дивана");

            CornerSofa = MitaCore.worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/CornerSofa");
            oam = ObjectAnimationMita.Create(CornerSofa.gameObject, "CornerSofa Hall sit left", "Сесть на угловой диван слева к окну");
            oam.setAiMovePoint(new Vector3(0.6f, 0.0f, 0.6f));
            oam.setStartPos(new Vector3(0f, 0.25f, 0f), new Vector3(270, 180, 0));
            oam.setIdleAnimation("Mita SitIdle");
            oam.addEnqueAnimationAction("Mita SitIdle");
            oam.setRevertAOM("CornerSofa stend up", "Встать с углового дивана");




            var OttomanSit = MitaCore.worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/SofaOttoman");
            oam = ObjectAnimationMita.Create(OttomanSit.gameObject, "SofaOttoman Hall sit", "Сесть на пуфик");
            oam.setAiMovePoint(new Vector3(0f, 0.0f, 0.6f));
            oam.setStartPos(new Vector3(0f, -0.2f, 0f), new Vector3(90, 0, 0));
            oam.setIdleAnimation("Mita SitIdle");
            oam.addEnqueAnimationAction("Mita SitIdle");
            oam.setRevertAOM("SofaOttoman stend up", "Встать с пуфика");



            var ChairRoom = MitaCore.worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Bedroom/BedroomChair");
            oam = ObjectAnimationMita.Create(ChairRoom.gameObject, "BedroomChair sit", "Сесть на кресло спальни");
            oam.setAiMovePoint(new Vector3(0f, 0.6f, 0.0f));
            oam.setStartPos(new Vector3(0f, 0.2f, 0f), new Vector3(270, 180, 0));
            oam.setIdleAnimation("Mita SitIdle");
            oam.addEnqueAnimationAction("Mita SitIdle");
            oam.setRevertAOM("BedroomChair stend up", "Встать с кресла");


            var LivingRoomBigTumb = MitaCore.worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/LivingRoomBigTumb");
            oam = ObjectAnimationMita.Create(LivingRoomBigTumb.gameObject, "LivingRoomBigTumb sit", "Залезть и сесть на тумбочку");
            oam.setAiMovePoint(new Vector3(0.5f, 0f, 0f));
            oam.setStartPos(new Vector3(-0.1f, 0.3f, 0.1f), new Vector3(0, 270, 270));
            oam.setIdleAnimation("Mita Sit High");
            oam.addEnqueAnimationAction("Mita Sit High");
            oam.setRevertAOM("LivingRoomBigTumb unsit", "Слезть с тумбочки с кресла");

            var sitBasement = MitaCore.worldBasement.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Basement/Bedroom MUChair");
            sitBasement.name = "MUChair";
            oam = ObjectAnimationMita.Create(sitBasement.gameObject, "MUChair Basement sit", "Сесть на табуретку");
            oam.setAiMovePoint(new Vector3(0f, 0.0f, 0.6f));
            oam.setStartPos(new Vector3(0f, 0f, 0f), new Vector3(90, 0, 0));
            oam.setIdleAnimation("Mita SitIdle");
            oam.addEnqueAnimationAction("Mita SitIdle");
            oam.setRevertAOM("MUChair Basement stend up", "Встать с табуретки");




            var objectInteractive = Interactions.FindOrCreateObjectInteractable(MitaCore.worldHouse.transform.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/RemoteTV").gameObject);
            objectInteractive.eventClick.AddListener((UnityAction)TVModded.turnTV);
            objectInteractive.active = true;

   
            //chairOIP.transform.localEulerAngles = new Vector3(0, 270, 270);
            //chairOIP.transform.localPosition = new Vector3(0.5f, 0.2f, 0);



            objectInteractive = Interactions.FindOrCreateObjectInteractable(GameObject.Find("Interactive Aihastion").gameObject,true,timeDeactivate:5);
            //var chairOIP = PlayerAnimationModded.CopyObjectAmimationPlayerTo(objectInteractive.transform, "Interactive Aihastion");
            //objectInteractive.eventClick.AddListener((UnityAction)chairOIP.GetComponent<ObjectAnimationPlayer>().AnimationPlay);
            objectInteractive.active = true;
            //objectInteractive.eventClick.AddListener((UnityAction)Hints.createExitButton);
            objectInteractive.eventClick.AddListener((UnityAction)TVModded.TurnControlKeys);
            //Interactions.FindOrCreateObjectInteractable(MitaCore.worldHouse.transform.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/LivingTable").gameObject);
            //Interactions.CreateObjectInteractable(Utils.TryfindChild(MitaCore.worldHouse, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/CornerSofa").gameObject);
            //Interactions.CreateObjectInteractable(Utils.TryfindChild(MitaCore.worldHouse, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Kitchen/Kitchen Table").gameObject);
            //Interactions.CreateObjectInteractable(Utils.TryfindChild(MitaCore.worldHouse, "Quests/Quest 1/Addon/Interactive Aihastion").gameObject);






            initCornerSofa(MitaCore.worldHouse);
            initConsole(MitaCore.worldBasement);




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
            var objectInteractive = Interactions.FindOrCreateObjectInteractable(sofa, true, 1, "Усесться");
            objectAnimationPlayer.angleHeadRotate = 70;
            //Utils.CopyComponentValues(exampleComponent, objectInteractive);

            objectAnimationPlayer.animationStart = PlayerAnimationModded.getPlayerAnimationClip("Player StartSit1");
            objectAnimationPlayer.animationLoop = PlayerAnimationModded.getPlayerAnimationClip("Player Sit");
            objectAnimationPlayer.animationStop = PlayerAnimationModded.getPlayerAnimationClip("Player Stand");
            objectInteractive.eventClick.AddListener((UnityAction)objectAnimationPlayer.AnimationPlay);

            //GameObject GameAihastion = Utils.TryfindChild(world, "Quests/Quest 1/Game Aihastion");

        }
        public static void initConsole(Transform worldBasement)
        {
            GameObject console = Utils.TryfindChild(MitaCore.worldBasement, "Act/Console");
            MitaCore.Console = console;
            MitaCore.Instance.cartridgeReader = console;

            var comp = console.AddComponent<Character>();
            comp.init_cartridge();
            comp.enabled = false;

            AudioControl.cartAudioSource = console.AddComponent<AudioSource>();

            console.GetComponent<Animator>().enabled = true;
            console.GetComponent<Outlinable>().enabled = true;
            ObjectInteractive objectInteractive = Interactions.FindOrCreateObjectInteractable(console, true, 1, "Переключить");

            objectInteractive.eventClick = new UnityEvent();
            objectInteractive.eventClick.AddListener((UnityAction)comp.changeActivation);

        }


        /*
         * 
         *  очень полезная функция, делающая прдемет кликабельным
         * 
         */
        public static ObjectInteractive FindOrCreateObjectInteractable(GameObject gameObject, bool remakeEvent = true, float timeDeactivate = 1, string tipText = null, bool addCollider = true, 
            bool useParent = false, Vector3 position = new Vector3(),Vector3 boxSize = new Vector3(), Vector3 CanvasPosition = new Vector3(),float distanceFloor = 1.4f)
        {
            if (gameObject == null)
            {
                MelonLogger.Msg("ERROR FIND: GameObject is null");
                return null;
            }

            var collider = gameObject.GetComponent<Collider>();
            if (collider == null && addCollider)
            {
                BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                if (!useParent)
                {
                    boxCollider.center = Vector3.zero;
                    if (boxSize != new Vector3()) boxCollider.size = boxSize;

                }
                MelonLogger.Msg($"Collider added to {gameObject.name}");
            }
   
            ObjectInteractive objectInteractive = gameObject.GetComponent<ObjectInteractive>();
            if (!objectInteractive)
            {
                //if (useParent) objectInteractive = gameObject.transform.parent.gameObject.AddComponent<ObjectInteractive>();
                //else
                objectInteractive = gameObject.AddComponent<ObjectInteractive>();

            }
  
            objectInteractive.active = true;
            if (position != new Vector3()) { 
                objectInteractive.transform.localPosition = position;
                objectInteractive.transform.localRotation = Quaternion.identity;
            }


            var outline = gameObject.GetComponent<Outlinable>();
            if (!outline)
            {
                outline = gameObject.AddComponent<Outlinable>();

            }
            objectInteractive.outline = outline;
            
            objectInteractive.objectInteractive = gameObject;
            if (useParent)
            {
                objectInteractive.objectInteractive = gameObject.transform.parent.gameObject;
            }
     

            objectInteractive.timeDeactive = timeDeactivate;

            if (objectInteractive.eventClick == null || remakeEvent)
                objectInteractive.eventClick = new UnityEvent();

            // objectInteractive.eventClick.AddListener((UnityAction)setAsLastOAP);

            objectInteractive.distanceFloor = distanceFloor;

            var caseInfoTransform = objectInteractive.transform.Find("Canvas");

            try
            {
                ObjectInteractive_CaseInfo caseInfo;
                if (caseInfoTransform == null)
                {
                    caseInfo = GameObject.Instantiate(tipTemplate, gameObject.transform).GetComponent<ObjectInteractive_CaseInfo>();
                    caseInfo.cameraT = MitaCore.Instance.playerPersonObject.transform;
                    caseInfoTransform = caseInfo.transform;
                }
                else
                {
                    caseInfo = caseInfoTransform.GetComponent<ObjectInteractive_CaseInfo>();
                }
                
                if (CanvasPosition != new Vector3())
                {
                    caseInfoTransform.localPosition = CanvasPosition;
                }
                else
                {
                    caseInfoTransform.position = objectInteractive.transform.parent.position+objectInteractive.transform.localPosition+new Vector3(0,0.15f,0f);
                }

                objectInteractive.caseInfo = caseInfo;


                caseInfo.interactiveMe = objectInteractive;

                



                caseInfo.dontDestroyAfter = true;
                
                

                //caseInfo.gameObject.AddComponent<LookAtPlayer>();

                //caseInfo.colorGradient1 = Color.green;
                //var cirle = caseInfo.transform.Find("Circle");
                //cirle.transform.localEulerAngles = new Vector3(60, 0, 0);

                //cirle.transform.localScale = Vector3.one * 1.5f;

                caseInfo.gameObject.active = true;
                caseInfo.active = true;
                var text = caseInfo.GetComponentInChildren<Text>();
                if (text != null && tipText != null)
                {

                    text.text = tipText;
                    text.m_Text = tipText;
                    try
                    {
                        caseInfo.Start();

                    }
                    catch { }

                    Utils.setTextTimed(text, tipText);
                }
                
               

            }
            catch (Exception ex2)
            {

                MelonLogger.Error(ex2);
            }
           


            return objectInteractive;
        }


        private static Dictionary<string, float> objectViewTime = new Dictionary<string, float>();

        public static void Update()
        {
            try
            {


                if (Camera.main != null && Camera.main.enabled && Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
                {
                    GameObject hitObject = hit.collider.gameObject;
                    string objectName = hitObject.name; // Используем имя как ключ


                    if (!objectViewTime.ContainsKey(objectName))
                    {
                        objectViewTime[objectName] = 0.0f;
                        //MelonLogger.Msg($"Adding new object: {objectName}");
                    }
                    else
                    {
                        // MelonLogger.Msg($"Object already tracked: {objectName}");
                    }
                    objectViewTime[objectName] += Time.unscaledDeltaTime;


                    if ( (objectName.Contains("Mita") || objectName.Contains("head)")) && objectViewTime[objectName] > 70f) EventsModded.LongWatching(objectName, objectViewTime[objectName]);
                    else if (objectViewTime[objectName] > 100f) EventsModded.LongWatching(objectName, objectViewTime[objectName]);

                    if (Input.GetMouseButtonDown(0))
                    {
                       // MelonLogger.Msg("Mouse clicked.");
                        OnGameObjectClicked(hitObject);
                    }

                    //MelonLogger.Msg($"{objectName}:{objectViewTime[objectName]}s.");
                    //MelonLogger.Msg($"objectViewTime count {objectViewTime.Count}.");
                }
            }
            catch (System.Exception ex)
            {

                MelonLogger.Error($"Interactions Update error: {ex}");
            }
        }

        public static string getObservedObjects()
        {
            
            //List<string> toRemove = new List<string>();
            try
            {
                StringBuilder answer = new StringBuilder("\nPlayer has observed since last answer (object:view time seconds):");
                foreach (var item in objectViewTime)
                {
                    if (item.Value >= 0.9f)
                    {
                        answer.Append($"{item.Key}:{item.Value.ToString("F2")}s\n");
                        //toRemove.Add(item.Key);
                    }
                }
                objectViewTime.Clear();
                return answer.ToString();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"getObservedObjects error: {ex}");

                try
                {
                    objectViewTime.Clear();

                }
                catch (System.Exception ex2)
                {

                    MelonLogger.Error($"getObservedObjects clear error: {ex2}"); 
                }
                
            }
            return "";

            

        }


        public static void OnGameObjectClicked(GameObject gameObject)
        {


            if (gameObject.GetComponent<ObjectInteractive>())
            {
                //MelonLogger.Msg("OnGameObjectClicked 1");
                //if (!gameObject.GetComponent<ObjectInteractive>().enabled) return;
                //MelonLogger.Msg("OnGameObjectClicked 2");
                if (Utils.getDistanceBetweenObjects(gameObject, MitaCore.Instance.playerPersonObject) >= 3f) return;

                MelonLogger.Msg("OnGameObjectClicked all");
                //UseSpecialCase(gameObject);

            }
        }

        private static void UseSpecialCase(GameObject gameObject)
        {
            MelonLogger.Msg("UseSpecialCase");
            switch (gameObject.name)
            {
                case "Console":
                    InteractionCases.caseConsoleStart(gameObject);
                    break;
                case "SofaChair":
                    InteractionCases.sofaStart(gameObject);
                    //ToggleBoolAfterTime(gameObject,5,true);
                    break;

            }
        }
        public static IEnumerator ToggleBoolAfterTime(GameObject gameObject, float delay, bool value)
        {
            // Ждём заданное время
            yield return new WaitForSeconds(delay);

            // Проверяем, есть ли компонент ObjectInteractive
            var objectInteractive = gameObject.GetComponent<ObjectInteractive>();
            if (objectInteractive != null)
            {
                objectInteractive.active = value;
                Debug.Log($"Object {gameObject.name} active set to {value}");
            }
            else
            {
                Debug.LogError($"ObjectInteractive component not found on {gameObject.name}");
            }
        }
    }
}


