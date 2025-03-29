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
        public Vector3 MoveChair;
        public Vector3 RotateChair;
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
            
        }

        public void addAction(UnityAction unityAction)
        {
            // Что произодет, когда Мита дойдет до цели
            mitaAIMovePoint.eventFinish.AddListener(unityAction);
        }

        public void Play()
        {
            //Отправляет Миту в Путь

            mitaAIMovePoint.mita = MitaCore.Instance.Mita;
            mitaAIMovePoint.PlayRotateAndWalk();
        }




        public void test()
        {

            try
            {
                var e = new UnityEngine.Events.UnityEvent();

                e.AddListener((UnityAction)testEnque);
                //№e.AddListener((UnityAction)MitaCore.Instance.Mita.AiShraplyStop);
                // или
                e.AddListener((UnityAction)StartMovingChair);
                mitaAIMovePoint.eventFinish = e;
                mitaAIMovePoint.mita = MitaCore.Instance.Mita;
            }
            catch (Exception ex)
            {

                MelonLogger.Msg(ex);
            }
   

        }
        void testEnque()
        {
            MitaAnimationModded.EnqueueAnimation("Mita StartSitChair");
        }

        void StartMovingChair()
        {
            MelonCoroutines.Start(moveChairTimed());
        }
        IEnumerator moveChairTimed()
        {
            GameObject chair = AmimatedObject; // Находим стул (лучше ссылку в инспекторе)
            if (chair == null) yield break;

            Vector3 startPosition = chair.transform.position;
            Quaternion startRotation = chair.transform.rotation;

            Quaternion targetRotation = startRotation * Quaternion.Euler(RotateChair);
            Vector3 targetPosition = startPosition + MoveChair;

            float elapsedTime = 0f;

            while (elapsedTime < moveDuration)
            {
                float progress = elapsedTime / moveDuration;

                // Плавное перемещение и вращение
                chair.transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
                chair.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, progress);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Гарантируем точное достижение цели
            chair.transform.position = targetPosition;
            chair.transform.rotation = targetRotation;
        }

    }
}