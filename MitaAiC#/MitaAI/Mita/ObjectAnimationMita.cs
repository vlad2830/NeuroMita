using UnityEngine;
using System.Collections;
using MelonLoader;
using MitaAI.Mita;
using Il2Cpp;
using UnityEngine.Events;
using UnityEngine.Playables;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MitaAI
{

    [RegisterTypeInIl2Cpp]
    public class ObjectAnimationMita : MonoBehaviour
    {


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
                

                info += $"\n Available interactions (use <{command}>Name</{command}> to interact): ";
                foreach (var oam in allOAMs)
                {
                    if (Utils.getDistanceBetweenObjects(oam.Value.AmimatedObject,MitaCore.Instance.MitaPersonObject)>15f) continue;

                    if (oam.Value.enabled == false) continue; 

                    info += $"\n Name: '{oam.Key}' - {oam.Value.tip}";
                }
            }



            return info;
        }

        public static string processInteraction(string response)
        {

            List<string> commands = new List<string>();
            string pattern = $@"<{command}>(.*?)</{command}>";
            MatchCollection matches = Regex.Matches(response, pattern);

            MelonLogger.Msg($"processInteraction finding...");
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    string interaction = match.Groups[1].Value;
                    MelonLogger.Msg($"Interaction {interaction} found response!");
                    try
                    {
                        allOAMs[interaction].Play();
                    }
                    catch (Exception ex)
                    {

                        MelonLogger.Error(ex);
                    }

                    break;
                }
            }

            pattern = $@"<{command}>.*?</{command}>";
            string result = Regex.Replace(response, pattern, "");

            return result;
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

        static bool TestWithBalls = false;

        MitaAIMovePoint mitaAIMovePoint;

        public static ObjectAnimationMita createObjectAnimationMita(string name, GameObject gameObject, Vector3 localPos, Vector3 localRot)
        {
            var g = new GameObject(name);
            g.transform.SetParent(gameObject.transform, false);
            g.transform.SetLocalPositionAndRotation(localPos, Quaternion.EulerAngles(localRot));
            var oam = g.AddComponent<ObjectAnimationMita>();

            oam.startOAMPosition = localPos;
            oam.startOAMRotaton = localRot;
            oam.finalOAMPosition = localPos;
            oam.finalOAMRotaton = localRot;

            oam.Init();


            return oam;
        }
        public static ObjectAnimationMita createObjectAnimationMita(string name,GameObject gameObject, Vector3 localPos, Vector3 localRot, Vector3 localPosFinal)
        {
            var g = new GameObject(name); ;
            g.transform.SetParent(gameObject.transform, false);
            g.transform.SetLocalPositionAndRotation(localPos, Quaternion.EulerAngles(localRot));
            var oam = g.AddComponent<ObjectAnimationMita>();

            oam.startOAMPosition = localPos;
            oam.startOAMRotaton = localRot;
            oam.finalOAMPosition = localPosFinal;
            oam.finalOAMRotaton = localRot;


            oam.Init();

            return oam;
        }

        public static ObjectAnimationMita createObjectAnimationMita(string name, GameObject gameObject, Vector3 localPos, Vector3 localRot, Vector3 localPosFinal, Vector3 localRotFinal)
        {
            var g = new GameObject(name);
            g.transform.SetParent(gameObject.transform, false);
            g.transform.SetLocalPositionAndRotation(localPos, Quaternion.EulerAngles(localRot));
            var oam = g.AddComponent<ObjectAnimationMita>();

            oam.startOAMPosition = localPos;
            oam.startOAMRotaton = localRot;
            oam.finalOAMPosition = localPosFinal;
            oam.finalOAMRotaton = localRotFinal;

  
            oam.Init();

            return oam;
        }
        public void resetPosition()
        {
            transform.SetPositionAndRotation(startOAMPosition,Quaternion.EulerAngles(startOAMRotaton));
        }

        void Init()
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
        public void addEnqueAnimationAction(string animName)
        {
            // Что проиграет Мита, когда подойдет
            mitaAmimatedName = animName;
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
            MitaAnimationModded.EnqueueAnimation(mitaAmimatedName,2f);
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
            MitaCore.Instance.MitaPersonObject.GetComponent<Rigidbody>().isKinematic = true;
        }
        void commonAction()
        {
            enabled = false;
            if (backAnimation != null)
            {
                backAnimation.enabled = true;
            }
        }
        #endregion

    }
}