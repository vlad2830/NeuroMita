using Il2Cpp;

using MelonLoader;
using System.Collections;
using UnityEngine;
using System.Text.RegularExpressions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine.Playables;
using UnityEngine.AI;
using Il2CppRootMotion.FinalIK;
using UnityEngine.Events;
using MitaAI.Mita;

namespace MitaAI
{
    public enum MovementStyles
    {
        walkNear = 0,
        follow = 1,
        stay = 2,
        noclip = 3,
        layingOnTheFloorAsDead = 4,
        sitting,
        cryingOnTheFloor

    }

    public static class MitaMovement
    {
        public static MovementStyles movementStyle = MovementStyles.walkNear;


        public static string SetMovementStyle(string response)
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
                if (MitaCore.Instance.Mita == null || MitaCore.Instance.Mita.gameObject == null)
                {
                    MelonLogger.Error("Mita object is null or Mita.gameObject is not active.");
                    return cleanedResponse; // Возвращаем faceStyle и очищенный текст
                }
                // Устанавливаем лицо, если оно найдено
                switch (MovementStyle)
                {
                    case "Следовать рядом с игроком":
                        movementStyle = MovementStyles.walkNear;
                        MitaCore.Instance.location34_Communication.ActivationCanWalk(true);
                        break;
                    case "Следовать за игроком":
                        movementStyle = MovementStyles.follow;
                        MitaCore.Instance.location34_Communication.ActivationCanWalk(false);
                        MelonCoroutines.Start(FollowPlayer());
                        MelonCoroutines.Start(LookOnPlayer());
                        break;
                    case "Стоять на месте":
                        MitaSetStaing();
                        break;
                    case "NoClip":
                        movementStyle = MovementStyles.noclip;
                        MitaCore.Instance.location34_Communication.ActivationCanWalk(false);
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
                MelonLogger.Error($"Problem with SetMovementStyle: {ex.Message}");
            }

            // Возвращаем кортеж: лицо и очищенный текст
            return cleanedResponse;
        }
        public static void MitaSetStaing()
        {
            movementStyle = MovementStyles.stay;
            MitaCore.Instance.location34_Communication.ActivationCanWalk(false);
            MelonCoroutines.Start(LookOnPlayer());
            MitaAnimationModded.getMitaAnimationModded(MitaCore.Instance.currentCharacter).resetToIdleAnimation();
        }
        public static void ChoseStyle(string animName)
        {

            if (animName.Contains("sit") || animName.Contains("Sit"))
            {
                movementStyle = MovementStyles.sitting;
            }
            else if(animName.Contains("Fall") || animName.Contains("Fall"))
            {
                movementStyle = MovementStyles.layingOnTheFloorAsDead;
                
            }
            else
            {
                movementStyle = MovementStyles.walkNear;
            }
           // else if (movementStyle == MovementStyles.sitting)
            //{
              //  movementStyle = MovementStyles.walkNear;
           // }
        }

        public static IEnumerator LookOnPlayer()
        {
            while (movementStyle != MovementStyles.walkNear)
            {
                if (!MitaCore.Instance.Mita.GetComponent<NavMeshAgent>().enabled)
                {
                    try
                    {
                        MitaCore.Instance.MitaLook.LookOnPlayerAndRotate();
                    }
                    catch (Exception e)
                    {

                        MelonLogger.Msg(e);
                    }
                    //if (Utils.Random(6, 10))

                    //else MitaLook.LookRandom();

                }
                yield return new WaitForSecondsRealtime(1);
            }
        }


        public static IEnumerator FollowPlayer(float distance = 1f)
        {

            while (movementStyle == MovementStyles.follow)
            {
                if (MitaCore.Instance.getDistanceToPlayer() > distance)
                {
                    MitaCore.Instance.Mita.AiWalkToTarget(MitaCore.Instance.playerPersonObject.transform);

                }
                else
                {
                    MitaCore.Instance.Mita.AiShraplyStop();
                    yield return new WaitForSeconds(2f);
                }

                yield return new WaitForSeconds(0.55f);
            }


        }
        public static IEnumerator FollowPlayerNoclip(float distance = 1.1f)
        {
            MelonLogger.Msg("Begin noClip");
            MitaCore.Instance.MitaPersonObject.GetComponent<CapsuleCollider>().enabled = false;
            while (movementStyle == MovementStyles.noclip && MitaCore.Instance.getDistanceToPlayer() > distance)
            {

                yield return MelonCoroutines.Start(MoveToPositionNoClip(25));

                yield return new WaitForSeconds(1f);
            }
            MitaCore.Instance.MitaPersonObject.GetComponent<CapsuleCollider>().enabled = true;

        }
        private static IEnumerator MoveToPositionNoClip(float speed)
        {
            while (movementStyle == MovementStyles.noclip && MitaCore.Instance.getDistanceToPlayer() > 0.9f)
            {
                Vector3 targetPosition = MitaCore.Instance.playerPerson.gameObject.transform.position;
                // Двигаем персонажа напрямую к цели (без учета препятствий)
                MitaCore.Instance.MitaPersonObject.transform.position = Vector3.MoveTowards(MitaCore.Instance.MitaPersonObject.transform.position, targetPosition, speed * Time.deltaTime);

                // Можно добавить поворот персонажа в направлении движения (опционально)
                Vector3 direction = (targetPosition - MitaCore.Instance.MitaPersonObject.transform.position).normalized;
                if (direction != Vector3.zero)
                    MitaCore.Instance.MitaPersonObject.transform.rotation = Quaternion.LookRotation(direction);

                yield return null; // Ждем следующий кадр
            }

            // Когда достигли цели
            Debug.Log("NoClip movement completed!");
        }














    }

}
