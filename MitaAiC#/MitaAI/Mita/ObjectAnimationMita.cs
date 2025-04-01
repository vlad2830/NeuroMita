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
        public Vector3 MoveTarget;
        public Vector3 RotateTarget;
        public float moveDuration = 2.0f;

        public Vector3 startOAMPosition;
        public Vector3 startOAMRotaton;


        public Vector3 finalOAMPosition;
        public Vector3 finalOAMRotaton;

        static bool TestWithBalls = true;

        public MitaAIMovePoint mitaAIMovePoint;

        string advancedActionName = "";


        void testInit(string name, GameObject parent,string AnimName,string idleAnim="")
        {
            this.name = name;
            startOAMPosition = parent.transform.position;
            startOAMRotaton = parent.transform.position;
            mitaAmimatedName = AnimName;
            Initialize();
            addEnqueAnimationAction(AnimName);
            if (!string.IsNullOrEmpty(idleAnim)) setIdleAnimation(idleAnim);
        }

        public static ObjectAnimationMita Create(string name, GameObject parent,
            Vector3 pos, Vector3 rot,
            Vector3? finalPos = null, Vector3? finalRot = null)
        {
            var oam = new GameObject(name).AddComponent<ObjectAnimationMita>();
            oam.transform.SetParent(parent.transform, false);

            oam.startOAMPosition = pos;
            oam.startOAMRotaton = rot;
            oam.finalOAMPosition = finalPos ?? pos;
            oam.finalOAMRotaton = finalRot ?? rot;

            oam.Initialize();
            return oam;
        }
        public void resetPosition()
        {
            transform.SetPositionAndRotation(startOAMPosition,Quaternion.EulerAngles(startOAMRotaton));
        }

        void Initialize()
        {
            allOAMs[name] = this;

            // Дебаг, так проще вернуть
            if (TestWithBalls)
            {
                // Добавляем сферу для визуализации (только для отладки)
                var debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugSphere.transform.SetParent(transform, false);
                debugSphere.transform.localScale = Vector3.one * 0.5f; // Масштабируем до небольшого размера
                debugSphere.GetComponent<Collider>().enabled = false; // Отключаем коллизию
                debugSphere.GetComponent<Renderer>().material.color = Color.red; // Делаем красным для заметности


            }

            mitaPerson = MitaCore.Instance.MitaPersonObject;
            AmimatedObject = transform.parent.gameObject;

            mitaAIMovePoint = gameObject.AddComponent<MitaAIMovePoint>();
            mitaAIMovePoint.targetMove = gameObject.transform;
            mitaAIMovePoint.eventFinish = new UnityEvent();
            mitaAIMovePoint.eventFinish.AddListener((UnityAction)commonAction);

            mitaAIMovePoint.mita = MitaCore.Instance.Mita;
            
            if (finalOAMPosition!=transform.localPosition || Quaternion.Euler(finalOAMRotaton) !=  transform.localRotation)
            {
                MelonLogger.Msg("Add MoveRotateObjectOAM");
                mitaAIMovePoint.eventFinish.AddListener((UnityAction)MoveRotateObjectOAM);

            }

        }

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
        }
        public void addReturningToNormal()
        {
            mitaAIMovePoint.eventFinish.AddListener((UnityAction)returnToNormalState);
        }


        public void clearAllActions()
        {
            mitaAIMovePoint.eventFinish.RemoveAllListeners();
        }

        public void Play()
        {
            try
            {
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
            Utils.StartObjectAnimation(AmimatedObject, MoveTarget, RotateTarget, 1);
        }
        void MoveRotateObjectOAM()
        {
            
            Utils.StartObjectAnimation(gameObject, finalOAMPosition, finalOAMRotaton, 1,false);
            MitaCore.Instance.Mita.MagnetOff();
            MitaCore.Instance.Mita.magnetTarget = null;
        }
        void EnqueAnimation()
        {
            MitaAnimationModded.EnqueueAnimation(mitaAmimatedName, AnimationTransitionDuration);
            MitaCore.Instance.Mita.MagnetOff();
            MitaCore.Instance.Mita.magnetTarget = null;
        }
        void SetIdleAnimation()
        {
            MitaCore.Instance.Mita.MagnetOff();
            MitaCore.Instance.Mita.magnetTarget = null;
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