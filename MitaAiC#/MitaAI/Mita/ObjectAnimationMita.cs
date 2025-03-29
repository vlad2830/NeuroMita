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
        public Vector3 MoveTarget;
        public Vector3 RotateTarget;
        public float moveDuration = 2.0f;

        

        MitaAIMovePoint mitaAIMovePoint;

        public static ObjectAnimationMita createObjectAnimationMita(GameObject gameObject, Vector3 localPos, Quaternion localRot)
        {
            var g = new GameObject($"{gameObject.name} OAM");
            g.transform.SetParent(gameObject.transform, false);
            g.transform.SetPositionAndRotation(localPos, localRot);
            return g.AddComponent<ObjectAnimationMita>();
        }


        public void Start()
        {
            mitaPerson = MitaCore.Instance.MitaPersonObject;
            AmimatedObject = transform.parent.gameObject;

            mitaAIMovePoint = gameObject.AddComponent<MitaAIMovePoint>();
            mitaAIMovePoint.targetMove = gameObject.transform;
            mitaAIMovePoint.eventFinish = new UnityEvent();
            mitaAIMovePoint.mita = MitaCore.Instance.Mita;
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
            //Отправляет Миту в Путь

            mitaAIMovePoint.mita = MitaCore.Instance.Mita;
            mitaAIMovePoint.PlayRotateAndWalk();
        }
        public void Play(MitaPerson mita)
        {
            //Отправляет Миту в Путь

            mitaAIMovePoint.mita = mita;
            mitaAIMovePoint.PlayRotateAndWalk();
        }


        #region Обертки

        void MoveRotateObject()
        {
            Utils.StartObjectAnimation(AmimatedObject, MoveTarget, RotateTarget, 2);
        }
        void EnqueAnimation()
        {
            MitaAnimationModded.EnqueueAnimation(mitaAmimatedName);
        }

        #endregion

    }
}