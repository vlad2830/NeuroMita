using UnityEngine;
using System.Collections;
using MelonLoader;
using MitaAI.Mita;
using Il2Cpp;
using UnityEngine.Events;
using UnityEngine.Playables;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static Il2CppRootMotion.FinalIK.AimPoser;
using Il2CppSystem.Runtime.Serialization.Formatters.Binary;
using System.Linq;

namespace MitaAI
{

    [RegisterTypeInIl2Cpp]
    public class ObjectAnimationMita : MonoBehaviour
    {
        private const float InteractionDistance = 15f;

        public float AnimationTransitionDuration = 2f;
        public bool isWalking = false;
        static ObjectAnimationMita currentOAMc;
        static Dictionary<string,ObjectAnimationMita> allOAMs = new Dictionary<string, ObjectAnimationMita>();

        static string command = "interaction";

        public static string interactionGetCurrentInfo()
        {
            string info = "";

            if (currentOAMc != null) {
                info += $"Currently interacting with {currentOAMc.AmimatedObject.name} - {currentOAMc.tip}";
            }
            if (allOAMs.Count>0)
            {

                info += $"\n You have special commands <{command}> for animating your interactions like sitting, lying, taking something.";
                info += $"\n Current available interactions (use <{command}>Name</{command}> to interact): ";
                foreach (var oam in allOAMs)
                {
                    var distance = Utils.getDistanceBetweenObjects(oam.Value.AmimatedObject, MitaCore.Instance.MitaPersonObject);
                    if (distance > InteractionDistance) continue;

                    if (oam.Value.enabled == false) continue; 

                    info += $"\n Name: '{oam.Key}' Tip: {oam.Value.tip} distance :{distance.ToString("F2")}";
                }
            }



            return info;
        }

        public static string ProcessInteraction(string response)
        {
            try
            {
                var matches = Regex.Matches(response, $@"<{command}>(.*?)</{command}>");
                foreach (Match match in matches.Cast<Match>().Where(m => m.Success))
                {
                    var interaction = match.Groups[1].Value;
                    if (allOAMs.TryGetValue(interaction, out var oam))
                    {
                        MitaAnimationModded.EnqueueAnimation(oam);
                    }
                }
                return Regex.Replace(response, $@"<{command}>.*?</{command}>", "");
            }
            catch (KeyNotFoundException ex)
            {
                MelonLogger.Error($"Interaction not found: {ex.Message}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Critical error: {ex}");
            }
            return response;
        }



        public GameObject AmimatedObject;
        public GameObject mitaPerson;

        public ObjectAnimationMita backAnimation;

        public string text = "";
        public string tip = "";

        public string mitaAmimatedName;
        public string mitaAmimatedNameIdle;

        public float moveDuration = 2.0f;


        // Куда нужно подойти
        GameObject aiMovePoint;
        public Vector3 aiMovePosition;
        public Vector3 aiMoveRotation;

        // Где в начале Мита

        public Vector3 startOAMPosition;
        public Vector3 startOAMRotation;

        // Что происходит с анимируемым объектом
        public Vector3 MoveTarget;
        public Vector3 RotateTarget;

        // Где в итоге будет Мита
        bool hasObjectMoving = false;
        public Vector3 finalOAMPosition;
        public Vector3 finalOAMRotation;

        bool TestWithBalls = false;

        public MitaAIMovePoint mitaAIMovePoint;

        string advancedActionName = "";


        void testInit(string name, string tip = "", string AnimName= "Mita SitIdle", string idleAnim= "Mita SitIdle")
        {
            this.name = name;
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            startOAMPosition = transform.position;
            startOAMRotation = Quaternion.ToEulerAngles(transform.rotation);
            mitaAmimatedName = AnimName;
            Initialize();
            addEnqueAnimationAction(AnimName);
            if (!string.IsNullOrEmpty(idleAnim)) setIdleAnimation(idleAnim);
        }


        public static ObjectAnimationMita Create(GameObject parent,string name, string tip = "")

        {
            ObjectAnimationMita oam = new GameObject(name).AddComponent<ObjectAnimationMita>();
            oam.transform.SetParent(parent.transform, false);
            oam.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);


            oam.Initialize();
            oam.tip = tip;
            oam.name = name;


            return oam;
        }


        void Initialize()
        {
            allOAMs[name] = this;

            // Дебаг, так проще вернуть
            if (TestWithBalls) Testing.makeTestingSphere(this.gameObject, Color.green);
   
            mitaPerson = MitaCore.Instance.MitaPersonObject;
            AmimatedObject = transform.parent.gameObject;

            aiMovePoint = new GameObject();
            aiMovePoint.transform.SetParent(transform);
            aiMovePoint.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            if (TestWithBalls) Testing.makeTestingSphere(aiMovePoint, Color.red);

            mitaAIMovePoint = aiMovePoint.AddComponent<MitaAIMovePoint>();
            mitaAIMovePoint.targetMove = aiMovePoint.transform;
            mitaAIMovePoint.magnetAfter = true;
            mitaAIMovePoint.eventFinish = new UnityEvent();
            mitaAIMovePoint.eventFinish.AddListener((UnityAction)commonAction);

            mitaAIMovePoint.mita = MitaCore.Instance.Mita;


            MoveTarget = transform.parent.localPosition;
            RotateTarget = Quaternion.ToEulerAngles(transform.parent.localRotation);
        }

        #region ЗаданиеПараметров

        // Относительно главного объекта!
        public void setAiMovePoint(Vector3 pos, Vector3 rot)
        {   
            aiMovePoint.transform.SetParent(transform);
            aiMovePoint.transform.SetLocalPositionAndRotation(pos,Quaternion.Euler(rot));
            //mitaAIMovePoint.targetMove = aiMovePoint.transform;

        }
        public void setAiMovePoint(Vector3 pos)
        {
            aiMovePoint.transform.SetParent(transform);
            aiMovePoint.transform.SetLocalPositionAndRotation(pos,Quaternion.identity);
            //mitaAIMovePoint.targetMove = aiMovePoint.transform;

        }

        public void setStartPos(Vector3 pos, Vector3 rot)
        {
            startOAMPosition = pos;
            startOAMRotation = rot;
            transform.SetLocalPositionAndRotation(pos, Quaternion.Euler(rot));

        }
        public void setFinalPos(Vector3 pos, Vector3 rot)
        {
            finalOAMPosition = pos;
            finalOAMRotation = rot;
            mitaAIMovePoint.eventFinish.AddListener((UnityAction)MoveRotateObjectOAM);
        }


        #endregion



        #region ДействияПоПриходуКТочке

        public void addSimpleAction(UnityAction unityAction)
        {
            // Что произодет, когда Мита дойдет до цели
            mitaAIMovePoint.eventFinish.AddListener(unityAction);
        }
        public void addAdvancedAction(string name)
        {
            advancedActionName = name;
            // Что произодет, когда Мита дойдет до цели
            mitaAIMovePoint.eventFinish.AddListener((UnityAction)advancedAction);
        }
        public void addEnqueAnimationAction(string animName)
        {
            // Что проиграет Мита, когда подойдет
            mitaAmimatedName = animName;
            // Что произодет, когда Мита дойдет до цели
            mitaAIMovePoint.eventFinish.AddListener((UnityAction)EnqueAnimation);
        }
        public void addEnqueAnimationAction(string animName, float transition_time)
        {
            // Что проиграет Мита, когда подойдет
            mitaAmimatedName = animName;
            AnimationTransitionDuration = transition_time;
            // Что произодет, когда Мита дойдет до цели
            mitaAIMovePoint.eventFinish.AddListener((UnityAction)EnqueAnimation);
        }
        public void setIdleAnimation(string animName,bool magnetAfter = true)
        {
            mitaAIMovePoint.magnetAfter = magnetAfter;
            // Что проиграет Мита, когда подойдет
            mitaAmimatedNameIdle = animName;
            // Что произодет, когда Мита дойдет до цели
            mitaAIMovePoint.eventFinish.AddListener((UnityAction)SetIdleAnimation);
        }
        public void addMoveRotateAction(Vector3 newlocalPos, Quaternion newlocalRot)
        {
            // Подвинет повернет что-то, когда мита подойдет.
            MoveTarget = newlocalPos;
            RotateTarget = newlocalPos;
            // Что произодет, когда Мита дойдет до цели
            mitaAIMovePoint.eventFinish.AddListener((UnityAction)MoveRotateObject);
            hasObjectMoving = true;
        }
        public void addReturningToNormal()
        {
            mitaAIMovePoint.eventFinish.AddListener((UnityAction)returnToNormalState);
        }
        
        
        // Обратное действие
        public ObjectAnimationMita setRevertAOM(string Name,string Tip,string idleAnim = "Mita Idle_2")
        {
            

            var oamBack = ObjectAnimationMita.Create(gameObject.transform.parent.gameObject, Name, Tip);
            


            oamBack.setIdleAnimation(idleAnim, false);
            oamBack.addEnqueAnimationAction(idleAnim);
            oamBack.addReturningToNormal();
            oamBack.enabled = false;

            backAnimation = oamBack;
            oamBack.backAnimation = this;

            return oamBack;
        }
        #endregion

        public void resetPosition()
        {
            transform.SetLocalPositionAndRotation(startOAMPosition, Quaternion.Euler(startOAMRotation));
            mitaAIMovePoint.transform.SetLocalPositionAndRotation(aiMovePosition, Quaternion.Euler(aiMoveRotation));
        }
        public void clearAllActions()
        {
            mitaAIMovePoint.eventFinish.RemoveAllListeners();
        }

        public void Play()
        {
            try
            {
                try
                {
                    if (currentOAMc != null)
                    {
                        if (currentOAMc.backAnimation != null && currentOAMc.hasObjectMoving)
                        {
                            currentOAMc.backAnimation.MoveRotateObject();
                        }


                    }
                }
                catch (Exception ex1)
                {

                    MelonLogger.Error($"Error Play Anim Object Mita {ex1}"); 
                }
               

                isWalking = true;
                currentOAMc = this;
                //Отправляет Миту в Путь
                MelonLogger.Msg("Start Play Anim Object Mita");
                mitaAIMovePoint.mita = MitaCore.Instance.Mita;
                MitaAnimationModded.location34_Communication.gameObject.active = false;
                //MitaCore.Instance.MitaPersonObject.GetComponent<Collider>().enabled = false;
                MitaCore.Instance.Mita.MagnetOff();
                MitaCore.Instance.MitaPersonObject.GetComponent<Rigidbody>().isKinematic = true;

                try
                {
                    mitaAIMovePoint.PlayRotateAndWalk();
                }
                catch (Exception ex2)
                {
                    MelonLogger.Error($"Error PlayRotateAndWalk {ex2}");
                }

                
                MelonLogger.Msg("Ended Play Anim Object Mita");
            }
            catch (Exception ex)
            {

                MelonLogger.Error($"Error Play Anim Object Mita {ex}");
            }
           

        }



        #region Обертки

        void MoveRotateObject()
        {
            MelonLogger.Msg($"MoveRotateObjectOAM {gameObject.name}");
            Utils.StartObjectAnimation(AmimatedObject, MoveTarget, RotateTarget, 1);
        }
        void MoveRotateObjectOAM()
        {
            MelonLogger.Msg($"MoveRotateObjectOAM {gameObject.name}");
            Utils.StartObjectAnimation(gameObject, finalOAMPosition, finalOAMRotation, 1,false);

            
        }
        void EnqueAnimation()
        {
            MitaAnimationModded.EnqueueAnimation(mitaAmimatedName, AnimationTransitionDuration);

            
        }
        void SetIdleAnimation()
        {

            
            //MitaCore.Instance.MitaPersonObject.GetComponent<Rigidbody>().isKinematic = false;
            MitaAnimationModded.setIdleAnimation(mitaAmimatedNameIdle);
            
        }
        void returnToNormalState()
        {
            MitaCore.Instance.Mita.MagnetOff();

            MitaAnimationModded.location34_Communication.gameObject.active = true;
            //MitaAnimationModded.location34_Communication.mitaCanWalk = true;
            MitaAnimationModded.checkCanMoveLook();
            MitaCore.Instance.MitaPersonObject.GetComponent<Rigidbody>().isKinematic = false;
        }
        void commonAction()
        {
            enabled = false;
            if (backAnimation != null)
            {
                backAnimation.enabled = true;
            }
            isWalking = false;

            
            // Для магнита где надо
            aiMovePoint.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);


        }

        void advancedAction()
        {
            if (advancedActionName == "123")
            {
                //
            }
        }


        #endregion

    }
}