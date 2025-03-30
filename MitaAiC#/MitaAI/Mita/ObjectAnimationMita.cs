using UnityEngine;
using System.Collections;
using MelonLoader;
using MitaAI.Mita;
using Il2Cpp;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace MitaAI
{

    [RegisterTypeInIl2Cpp]
    public class ObjectAnimationMita : MonoBehaviour
    {

        public GameObject AmimatedObject;
        public GameObject mitaPerson;


        public string mitaAmimatedName;
        public string mitaAmimatedNameIdle;
        public Vector3 MoveTarget;
        public Vector3 RotateTarget;
        public float moveDuration = 2.0f;

        public Vector3 finalOAMPosition;
        public Vector3 finalOAMRotaton;


        MitaAIMovePoint mitaAIMovePoint;

        public static ObjectAnimationMita createObjectAnimationMita(GameObject gameObject, Vector3 localPos, Quaternion localRot)
        {
            var g = new GameObject($"{gameObject.name} OAM");
            g.transform.SetParent(gameObject.transform, false);
            g.transform.SetLocalPositionAndRotation(localPos, localRot);
            var oam = g.AddComponent<ObjectAnimationMita>();
            oam.Init();

            oam.finalOAMPosition = localPos;
            oam.finalOAMRotaton = Quaternion.ToEulerAngles(localRot);

            return oam;
        }
        public static ObjectAnimationMita createObjectAnimationMita(GameObject gameObject, Vector3 localPos, Quaternion localRot, Vector3 localPosFinal)
        {
            var g = new GameObject($"{gameObject.name} OAM");
            g.transform.SetParent(gameObject.transform, false);
            g.transform.SetLocalPositionAndRotation(localPos, localRot);
            var oam = g.AddComponent<ObjectAnimationMita>();

            oam.finalOAMPosition = localPosFinal;
            oam.finalOAMRotaton = Quaternion.ToEulerAngles(localRot);

            oam.Init();

            return oam;
        }

        public static ObjectAnimationMita createObjectAnimationMita(GameObject gameObject, Vector3 localPos, Quaternion localRot, Vector3 localPosFinal, Vector3 localRotFinal)
        {
            var g = new GameObject($"{gameObject.name} OAM");
            g.transform.SetParent(gameObject.transform, false);
            g.transform.SetLocalPositionAndRotation(localPos, localRot);
            var oam = g.AddComponent<ObjectAnimationMita>();

            oam.finalOAMPosition = localPosFinal;
            oam.finalOAMRotaton = localRotFinal;

        oam.Init();

            return oam;
        }


        void Init()
        {
            mitaPerson = MitaCore.Instance.MitaPersonObject;
            AmimatedObject = transform.parent.gameObject;

            mitaAIMovePoint = gameObject.AddComponent<MitaAIMovePoint>();
            mitaAIMovePoint.targetMove = gameObject.transform;
            mitaAIMovePoint.eventFinish = new UnityEvent();
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
        public void setIdleAnimation(string animName)
        {
            mitaAIMovePoint.magnetAfter = true;
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


        public void clearAllActions()
        {
            mitaAIMovePoint.eventFinish.RemoveAllListeners();
        }

        public void Play()
        {
            try
            {
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
        public void Play(MitaPerson mita)
        {
            //Отправляет Миту в Путь
            MelonLogger.Msg("Start Anim Object Mita");
            mitaAIMovePoint.mita = mita;
            MitaAnimationModded.location34_Communication.gameObject.active = false;
            MitaCore.Instance.MitaPersonObject.GetComponent<Rigidbody>().isKinematic = true;
            MitaCore.Instance.Mita.MagnetOff();
            mitaAIMovePoint.PlayRotateAndWalk();
            MelonLogger.Msg("Ended Play Anim Object Mita");


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
            MitaAnimationModded.EnqueueAnimation(mitaAmimatedName);
            MitaCore.Instance.Mita.MagnetOff();
            MitaCore.Instance.Mita.magnetTarget = null;
        }
        void SetIdleAnimation()
        {
            MitaCore.Instance.Mita.MagnetOff();
            MitaCore.Instance.Mita.magnetTarget = null;
            MitaCore.Instance.MitaPersonObject.GetComponent<Rigidbody>().isKinematic = false;
            MitaAnimationModded.setIdleAnimation(mitaAmimatedNameIdle);
            
        }

        #endregion

    }
}