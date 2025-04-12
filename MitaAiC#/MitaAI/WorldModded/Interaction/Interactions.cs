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

            tipTemplate = GameObject.Find("Addon/Interactive Aihastion/Canvas");

            var objectInteractive = Interactions.FindOrCreateObjectInteractable(MitaCore.worldHouse.transform.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/RemoteTV").gameObject);
            objectInteractive.eventClick.AddListener((UnityAction)TVModded.turnTV);
            objectInteractive.active = true;

   
            //chairOIP.transform.localEulerAngles = new Vector3(0, 270, 270);
            //chairOIP.transform.localPosition = new Vector3(0.5f, 0.2f, 0);



            objectInteractive = Interactions.FindOrCreateObjectInteractable(GameObject.Find("Interactive Aihastion").gameObject,true);
            var chairOIP = PlayerAnimationModded.CopyObjectAmimationPlayerTo(objectInteractive.transform, "Interactive Aihastion");
            //objectInteractive.eventClick.AddListener((UnityAction)chairOIP.GetComponent<ObjectAnimationPlayer>().AnimationPlay);
            objectInteractive.active = true;
            //Interactions.FindOrCreateObjectInteractable(MitaCore.worldHouse.transform.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/LivingTable").gameObject);
            //Interactions.CreateObjectInteractable(Utils.TryfindChild(MitaCore.worldHouse, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Main/CornerSofa").gameObject);
            //Interactions.CreateObjectInteractable(Utils.TryfindChild(MitaCore.worldHouse, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Kitchen/Kitchen Table").gameObject);
            //Interactions.CreateObjectInteractable(Utils.TryfindChild(MitaCore.worldHouse, "Quests/Quest 1/Addon/Interactive Aihastion").gameObject);
        }

        public static ObjectInteractive FindOrCreateObjectInteractable(GameObject gameObject, bool clearEvent = true, float timeDeactive = 5, string tipText = null, bool addCollider = true, bool useParent = false, Vector3 position = new Vector3())
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
            if (useParent) objectInteractive.objectInteractive = gameObject.transform.parent.gameObject;

            objectInteractive.timeDeactive = timeDeactive;

            if (objectInteractive.eventClick == null)
                objectInteractive.eventClick = new UnityEvent();
            else
            {   
                if (clearEvent) objectInteractive.eventClick.RemoveAllListeners();
            }


            var caseInfoObj = objectInteractive.transform.Find("Canvas");

            try
            {
                if (caseInfoObj == null)
                {
                    var caseInfo = GameObject.Instantiate(tipTemplate, gameObject.transform).GetComponent<ObjectInteractive_CaseInfo>();

                    objectInteractive.caseInfo = caseInfo;


                    caseInfo.interactiveMe = objectInteractive;
                    caseInfo.active = false;
                    caseInfo.transform.localPosition = Vector3.zero;



                    caseInfo.dontDestroyAfter = true;
                    caseInfo.transform.localPosition = new Vector3(0, 0, 0.1f);
                    caseInfo.cameraT = MitaCore.Instance.playerPersonObject.transform;

                    //caseInfo.gameObject.AddComponent<LookAtPlayer>();

                    caseInfo.colorGradient1 = Color.green;
                    //var cirle = caseInfo.transform.Find("Circle");
                    //cirle.transform.localEulerAngles = new Vector3(60, 0, 0);

                    //cirle.transform.localScale = Vector3.one * 1.5f;
                    MelonLogger.Msg("FindOrCreateObjectInteractable 4");
                    var text = caseInfo.GetComponentInChildren<Text>();
                    if (text != null && tipText != null)
                    {
                        text.text = tipText;
                        text.m_Text = tipText;
                    }
                }
                else
                {
                    MelonLogger.Msg("FindOrCreateObjectInteractable 4");
                    var text = caseInfoObj.GetComponentInChildren<Text>();
                    if (text != null && tipText != null)
                    {
                        text.text = tipText;
                        text.m_Text = tipText;
                    }
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
                UseSpecialCase(gameObject);

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


