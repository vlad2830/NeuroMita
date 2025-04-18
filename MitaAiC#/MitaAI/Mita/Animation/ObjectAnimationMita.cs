﻿using UnityEngine;
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
        private const float InteractionDistanceVisible = 15f;


        static ObjectAnimationMita currentOAMc;
        static Dictionary<string,ObjectAnimationMita> allOAMs = new Dictionary<string, ObjectAnimationMita>();

        static string command = "interaction";

        public static string interactionGetCurrentInfo()
        {
            string info = "";

            if (currentOAMc != null) {
                info += $"Currently interacting with {currentOAMc.AmimatedObject.name} - {currentOAMc.tip}";
                info += $"Attention: all move commands will finish iteraction! Thus use finishing they only intentionally!";
            }
            if (allOAMs.Count>0)
            {

                info += $"\n You have special commands <{command}> for animating your interactions like sitting, lying, taking something.";
                info += $"\n Current available interactions (use <{command}>Name</{command}> to interact): ";
                
                foreach (var oam in allOAMs)
                {
                    var distance = Utils.getDistanceBetweenObjects(oam.Value.AmimatedObject, MitaCore.Instance.MitaPersonObject);
                    if (distance > InteractionDistanceVisible) continue;

                    if (oam.Value.enabled == false) continue;

                    if (oam.Value.commonInteractableObject.isTaken(oam.Value.position))
                    {
                        // TODO сделать лучше
                        info += $"\n Name: '{oam.Key}' distance :{distance.ToString("F2")} is taken by {oam.Value.commonInteractableObject.taker}";
                        continue;
                    }

                    info += $"\n Name: '{oam.Key}' Tip: {oam.Value.tip} distance :{distance.ToString("F2")}";
                    
                }
            }



            return info;
        }

        public static string ProcessInteraction(string response)
        {
            try
            {
                MelonLogger.Msg($"Inside ProcessInteraction");
                var matches = Regex.Matches(response, $@"<{command}>(.*?)</{command}>");
                foreach (Match match in matches.Cast<Match>().Where(m => m.Success))
                {
                    var interaction = match.Groups[1].Value;
                    if (allOAMs.TryGetValue(interaction, out var oam))
                    {
                        MelonLogger.Msg($"Found Interaction {oam.text} {oam.tip}");
                        MitaAnimationModded.EnqueueAnimation(oam,delayAfter:0.5f);
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

        public static void finishWorkingOAM()
        {
            if (currentOAMc == null) return;

            if (currentOAMc.backAnimation != null)
            {
                if (currentOAMc.backAnimation.isEndingObject) currentOAMc.backAnimation.Play();

                MelonLogger.Msg("Ending working OAM");
            }
        }

        public CommonInteractableObject commonInteractableObject;

        public GameObject AmimatedObject;
        public GameObject mitaPerson;

        public ObjectAnimationMita backAnimation;
        public bool isEndingObject = false;

        public string text = "";
        public string tip = "";
        public string position = "center";

        public string mitaAmimatedName;
        public string mitaAmimatedNameIdle;

        public float moveDuration = 2.0f;
        public float AnimationTransitionDuration = 1f;

        public bool needWalking = true;
        public bool NeedMovingToIdle = true;
        public bool isWalking = false;


        // Куда нужно подойти
        GameObject aiMovePoint;
        public Vector3 aiMoveInitialPosition;
        public Vector3 aiMoveInitialRotation;

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

        static bool TestWithBalls = false;

        public MitaAIMovePoint mitaAIMovePoint;

        string advancedActionName = "";

        public static ObjectAnimationMita CurrentOAMc { get => currentOAMc; set => currentOAMc = value; }

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


        public static ObjectAnimationMita Create(GameObject parent,string name, string tip = "",bool needWalking = true, bool NeedMovingToIdle = true,bool isEndingObject = false,string position = "center")

        {
            ObjectAnimationMita oam = new GameObject(name).AddComponent<ObjectAnimationMita>();
            oam.transform.SetParent(parent.transform, false);
            oam.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);


            
            oam.tip = tip;
            oam.name = name;
            oam.needWalking = needWalking;
            oam.NeedMovingToIdle = NeedMovingToIdle;
            oam.position = position;
            oam.commonInteractableObject = CommonInteractableObject.CheckCreate(oam.gameObject.transform.parent.gameObject);
            

            oam.Initialize();
            
            if (!isEndingObject && TestWithBalls)
            {
                Testing.makeTestingSphere(oam.gameObject, Color.green);
                Testing.makeTestingSphere(oam.aiMovePoint, Color.red);
            }

            return oam;
        }


        void Initialize()
        {
            allOAMs[name] = this;

            // Дебаг, так проще вернуть
            
   
            mitaPerson = MitaCore.Instance.MitaPersonObject;
            AmimatedObject = transform.parent.gameObject;

            aiMovePoint = new GameObject();
            aiMovePoint.transform.SetParent(transform);
            aiMovePoint.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            



            mitaAIMovePoint = aiMovePoint.AddComponent<MitaAIMovePoint>();
            mitaAIMovePoint.targetMove = aiMovePoint.transform;
            mitaAIMovePoint.magnetAfter = false;
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
            aiMoveInitialPosition = pos;
            aiMoveInitialRotation = rot;

            aiMovePoint.transform.SetParent(transform);
            aiMovePoint.transform.SetLocalPositionAndRotation(pos,Quaternion.Euler(rot));
            //mitaAIMovePoint.targetMove = aiMovePoint.transform;

        }
        public void setAiMovePoint(Vector3 pos)
        {
            aiMoveInitialPosition = pos;
            aiMoveInitialRotation = Vector3.zero;

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

        public void addSimpleAction(UnityAction unityAction,bool setToBackOAM = true)
        {
            // Что произодет, когда Мита дойдет до цели
            mitaAIMovePoint.eventFinish.AddListener(unityAction);
            if (setToBackOAM && backAnimation != null)
            {
                backAnimation.mitaAIMovePoint.eventFinish.AddListener(unityAction);
            }
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
            //mitaAIMovePoint.magnetAfter = magnetAfter;
            // Что проиграет Мита, когда подойдет
            mitaAmimatedNameIdle = animName;
            // Что произодет, когда Мита дойдет до цели
            mitaAIMovePoint.eventFinish.AddListener((UnityAction)SetIdleAnimation);
        }
        public void resetIdleAnimation()
        {
            mitaAIMovePoint.eventFinish.AddListener((UnityAction)ResetIdleAnimation);
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
        public ObjectAnimationMita setRevertAOM(string Name,string Tip,string idleAnim = "Mita Idle_2",bool needWalking = false, bool NeedMovingToIdle = false)
        {
            

            var oamBack = ObjectAnimationMita.Create(gameObject.transform.parent.gameObject, Name, Tip,isEndingObject:true);

            oamBack.resetIdleAnimation();
            oamBack.addReturningToNormal();
            oamBack.enabled = false;
            oamBack.needWalking = needWalking;
            oamBack.NeedMovingToIdle = NeedMovingToIdle;
            oamBack.isEndingObject = true;
            
            backAnimation = oamBack;
            oamBack.backAnimation = this;
            return oamBack;
        }
        #endregion

        public void resetPosition()
        {
            transform.SetLocalPositionAndRotation(startOAMPosition, Quaternion.Euler(startOAMRotation));
            mitaAIMovePoint.transform.SetLocalPositionAndRotation(aiMoveInitialPosition, Quaternion.Euler(aiMoveInitialRotation));
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

                currentOAMc = this;
                MitaCore.Instance.Mita.MagnetOff();
                MitaCore.Instance.MitaPersonObject.GetComponent<Rigidbody>().isKinematic = true;
                MitaState.currentMitaState = MitaStateType.interaction;
                MitaAnimationModded.location34_Communication.ActivationCanWalk(false);

                if (needWalking)
                {
                    isWalking = true;

                    //Отправляет Миту в Путь
                    MelonLogger.Msg("Start Play Anim Object Mita");
                    mitaAIMovePoint.mita = MitaCore.Instance.Mita;



                    try
                    {
                        MitaCore.Instance.Mita.AiWalkToTargetRotate(aiMovePoint.transform, mitaAIMovePoint.eventFinish);
                    }
                    catch (Exception ex2)
                    {
                        MelonLogger.Error($"Error AiWalkToTargetTranform {ex2}");
                    }

                    if (!isEndingObject) commonInteractableObject.setTaken(MitaCore.Instance.currentCharacter,position);
                }
                else
                {
                    mitaAIMovePoint.eventFinish.Invoke();
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
            MitaAnimationModded.EnqueueAnimation(mitaAmimatedName, AnimationTransitionDuration,makeFirst:true,avoidStateSettings:true);

            
        }
        void SetIdleAnimation()
        {
            
            MitaAnimationModded.setIdleAnimation(mitaAmimatedNameIdle);
            MitaAnimationModded.checkCanMoveRotateLook(ignoreInteractionCondition: true);
            if (NeedMovingToIdle) Utils.StartObjectAnimation(MitaCore.Instance.MitaPersonObject, transform.position, transform.eulerAngles, AnimationTransitionDuration+0.5f, false);

        }

        void ResetIdleAnimation()
        {
            
            MitaAnimationModded.resetToIdleAnimation(needEnque:true);
            MitaAnimationModded.checkCanMoveRotateLook(ignoreInteractionCondition:true);
        }
        void returnToNormalState()
        {

            backAnimation.commonInteractableObject.free(position);

            MitaCore.Instance.Mita.MagnetOff();
            
            MitaAnimationModded.location34_Communication.gameObject.active = true;
            
            
            MitaState.currentMitaState = MitaStateType.normal;

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
            //aiMovePoint.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            MitaCore.Instance.Mita.MagnetToTarget(gameObject.transform);
           

        }

        void advancedAction()
        {
            if (advancedActionName == "123")
            {
                //
            }
        }


        #endregion

        #region Other

        IEnumerator brootforceMagnetToOAM(MitaPerson mita,Transform transform,float seconds = 1f,float repeatTimer = 0.1f,bool offMagnetAfter = false)
        {

            int i = 0;
            float times = (seconds / repeatTimer);

            mita.MagnetToTarget(transform);
            while (i< times)
            {
                i++;
                yield return new WaitForSeconds(repeatTimer);
                mita.MagnetToTarget(transform);
            }
            if (offMagnetAfter) MitaCore.Instance.Mita.MagnetOff();

        }


        #endregion



    }
}