using Il2Cpp;

using MelonLoader;
using System.Collections;
using UnityEngine;
using System.Text.RegularExpressions;
namespace MitaAI.Mita
{
    public static class MitaAnimationModded
    {
        static private Queue<string> animationQueue = new Queue<string>();
        static private bool isPlaying = false;
        static private Il2CppAssetBundle bundle;
        static Animator_FunctionsOverride mitaAnimatorFunctions;
        static Location34_Communication location34_Communication;
        static AnimationClip idleAnimation;
        static AnimationClip idleWalkAnimation;
        // Основной метод для добавления анимации в очередь
        static RuntimeAnimatorController runtimeAnimatorController;
        static AnimatorOverrideController overrideController;
        static Animator animator;

        static public string currentIdleAnim = "Mita Idle_2";
        public enum IdleStates
        {
            normal = 0,
            talkWithPlayer = 1,

        }



        static public void init(Animator_FunctionsOverride _mitaAnimatorFunctions, Location34_Communication _location34_Communication)
        {
            // Получаем компонент Animator_FunctionsOverride из текущего объекта
            mitaAnimatorFunctions = _mitaAnimatorFunctions;
            location34_Communication = _location34_Communication;

            bundle = AssetBundleLoader.LoadAssetBundle("assetbundle");
            if (mitaAnimatorFunctions == null)
            {
                MelonLogger.Msg("Animator_FunctionsOverride component not found on this object!");
            }
            idleAnimation = location34_Communication.mitaAnimationIdle;
            if (idleAnimation == null) idleAnimation = AssetBundleLoader.LoadAnimationClipByName(bundle,"Mita Idle_2");
            idleWalkAnimation = location34_Communication.mitaAnimationWalk;
            if (idleWalkAnimation == null) idleWalkAnimation = AssetBundleLoader.LoadAnimationClipByName(bundle, "Mita Walk_1");
            try
            {
                runtimeAnimatorController = AssetBundleLoader.LoadAnimatorControllerByName(bundle, "Mita_1.controller");
                MelonLogger.Msg("a");
                animator = MitaCore.Instance.MitaPersonObject.GetComponent<Animator>();
                animator.runtimeAnimatorController = runtimeAnimatorController;
                animator.SetTrigger("NextLerp");
                foreach (var item in runtimeAnimatorController.animationClips)
                {
                    item.events = Array.Empty<AnimationEvent>();
                }
                setIdleWalk("Mita Walk_1");
                MelonLogger.Msg("b!");
            }
            catch (Exception ex)
            {

                MelonLogger.Msg("Error custom controller"+ex);
            }
            
        }

        public static string setAnimation(string response)
        {
            // Регулярное выражение для извлечения эмоций
            string pattern = @"<a>(.*?)</a>";
            Match match = Regex.Match(response, pattern);

            string animName = string.Empty;
            string cleanedResponse = Regex.Replace(response, @"<a>.*?</a>", ""); // Очищаем от всех тегов

            if (match.Success)
            {
                // Если эмоция найдена, устанавливаем её в переменную faceStyle
                animName = match.Groups[1].Value;
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
                switch (animName)
                {
                    case "Щелчек":
                        int randomIndex = UnityEngine.Random.Range(0, 4); // Генерация числа от 0 до 3
                        string animationName;

                        if (randomIndex == 5)
                            animationName = "Mita Click_0"; // Не то
                        else if (randomIndex == 1)
                            animationName = "Mita Click 1";
                        else if (randomIndex == 6) // Будет
                            animationName = "Mita Click_2";
                        else
                            animationName = "Mita Click"; // Четвёртый кейс

                        EnqueueAnimation(animationName);
                        break;
                    case "Похлопать в ладоши":
                        EnqueueAnimation("Mita Cheerful");
                        break;
                    case "Помахать в приветствие":
                        EnqueueAnimation("Mita Hello");
                        break;
                    case "Указать направление":
                        EnqueueAnimation("Mita ShowTumb");
                        break;
                    case "Смотреть с презрением":
                        EnqueueAnimation("Mita IdleBat");
                        setIdleAnimation("Mita IdleBat");
                        break;
                    case "Показать усталость":
                        EnqueueAnimation("Mita Start Tired");
                        setIdleAnimation("Mita Tired");
                        break;
                    case "Притвориться отключенной и упасть":
                        EnqueueAnimation("Mita Fall Start");
                        setIdleAnimation("Mita Fall Idle");
                        break;
                    case "Взять предмет":
                        EnqueueAnimation("Mita TakeBat");
                        
                        break;
                    case "Жест пальцами":
                        MitaCore.Instance.MitaLook.LookOnPlayerAndRotate();
                        EnqueueAnimation("Mita FingersGesture");
                        break;
                    case "Кивнуть да":
                        MitaCore.Instance.MitaLook.Nod(true);
                        break;
                    case "Кивнуть нет":
                        MitaCore.Instance.MitaLook.Nod(false);
                        break;
                    case "Глянуть глазами в случайном направлении":
                        MitaCore.Instance.MitaLook.EyesLookOffsetRandom(90);
                        break;
                    case "Повернуться в случайном направлении":
                        MitaCore.Instance.MitaLook.LookRandom();
                        break;
                    case "Развести руки":
                        //EnqueueAnimation("Mita StartShow Knifes");
                        EnqueueAnimation("Mita Throw Knifes");
                        break;
                    case "Руки по бокам":
                        EnqueueAnimation("Mita Idle NoKnifes");
                        setIdleAnimation("Mita Idle NoKnifes");
                        break;
                    case "Руки сложены в ладони перед собой":
                        EnqueueAnimation("Mita Idle Cheerful");
                        setIdleAnimation("Mita Idle Cheerful");
                        break;
                    case "Одна рука прижата, вторая сзади":
                        EnqueueAnimation("Mita Hairbrash");
                        setIdleAnimation("Mita Hairbrash");
                        break;
                    case "Поднести палец к подбородку":
                        EnqueueAnimation("Mita TalkWithPlayer");
                        setIdleAnimation("Mita TalkWithPlayer");
                        break;
                    case "Поднять игрока одной рукой":
                        EnqueueAnimation("Mita TakeMita");
                        //EnqueueAnimation("Mita TakeMita Idle");
                        setIdleAnimation("Mita TakeMita Idle");
                        //EnqueueAnimation("Mita ThrowPlayer");
                        break;
                    case "Руки вперед по бокам":
                        EnqueueAnimation("Mita Hands Down Idle");
                        setIdleAnimation("Mita Hands Down Idle");
                        break;
                    case "Сложить руки перед собой":
                        EnqueueAnimation("Mita Idle_2");
                        setIdleAnimation("Mita Idle_2");
                        break;
                    case "Показать предмет":
                        EnqueueAnimation("Mita Selfi");
                        break;
                    case "Стать разочарованной":
                        EnqueueAnimation("Mita StartDisappointment");
                        setIdleAnimation("Mita IdleDisappointment");
                        break;
                    case "Руки в кулаки":
                        EnqueueAnimation("Mita StartAngry");
                        setIdleAnimation("Idle Angry");
                        break;
                    case "Сесть и плакать":
                        EnqueueAnimation("Mila CryNo");
                        setIdleAnimation("Mila CryNo");
                        break;
                    case "Дружески ударить":
                        EnqueueAnimation("Mila Kick");
                        break;
                    case "Посмотреть по сторонам":
                        EnqueueAnimation("Mita IdleCheck");
                        break;
                    case "Прикрыть глаза":
                        EnqueueAnimation("Mita Close Eyes");
                        //int randomIndex = UnityEngine.Random.Range(0, 4); // Генерация числа от 0 до 3
                        EnqueueAnimation("Mita Open Eyes");
                        //EnqueueAnimation("Mita Open Shar Eyes");
                        break;
                    case "Обнять":
                        EnqueueAnimation("Mita StartHug");
                        EnqueueAnimation("Mita HugIdle");
                        EnqueueAnimation("Mita StopHug");
                        break;
                    case "Удар":
                        EnqueueAnimation("Mita Kick");
                        break;
                    case "Помахать перед лицом":
                        EnqueueAnimation("Hey");
                        break;
                    case "Помахать руками в стороны":
                        EnqueueAnimation("Mita HandsDance");
                        break;
                    case "Начать махать руками в стороны":
                        EnqueueAnimation("Mita HandsDance");
                        setIdleAnimation("Mita HandsDance");
                        break;
                    case "Похвастаться предметом":
                        EnqueueAnimation("Mita Take Recorder");
                        break;
                    case "Прикрыть рот и помахать рукой":
                        EnqueueAnimation("Mita Oi");
                        //EnqueueAnimation("Mita Idle");
                        EnqueueAnimation("Mita Oi Heh");
                        break;
                    case "Стать уставшей":
                        EnqueueAnimation("Mita Start Tired");
                        //EnqueueAnimation("Mita Idle");
                        setIdleAnimation("Mita Tired");
                        break;

                    case "Случайная анимация":
                        EnqueueAnimation("");
                        break;
                    default:
                        if (animName != "")
                        {
                            EnqueueAnimation(animName);
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Problem with Animation: {ex.Message}");
            }

            // Возвращаем кортеж: лицо и очищенный текст
            return cleanedResponse;
        }


        static public void EnqueueAnimation(string animName = "",float lenght = 0, float timeAfter = 0)
        {
           /* if (bundle == null)
            {
                bundle = AssetBundleLoader.LoadAssetBundle("assetbundle");
            }*/

            //AnimationClip anim = null;
            try
            {
                /*if (!string.IsNullOrEmpty(animName))
                {
                    anim = AssetBundleLoader.LoadAnimationClipByName(bundle, animName);
                }
                else
                {
                    anim = AssetBundleLoader.LoadRandomAnimationClip(bundle);
                }*/

                /*if (anim != null)
                {*/
                    //anim.events = Array.Empty<AnimationEvent>();
                    animationQueue.Enqueue(animName);
                    MelonLogger.Msg($"Added to queue: {animName}");

                    if (!isPlaying)
                    {
                        MelonCoroutines.Start(ProcessQueue());
                    }
               // }
            }
            catch (Exception e)
            {
                MelonLogger.Msg("Animation error: " + e);
            }
        }
        static AnimationClip FindAnimationClipByName(string animationName)
        {
            if (runtimeAnimatorController == null) return null;
            // Получаем все анимации из Animator Controller
            // AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            AnimationClip[] clips = runtimeAnimatorController.animationClips;
            // Проверяем, есть ли анимация с указанным именем
            foreach (AnimationClip clip in clips)
            {
                if (clip.name == animationName)
                {
                    return clip;
                }
            }

            return null;
        }

        // Корутина для последовательного проигрывания
        static private IEnumerator ProcessQueue()
        {
            isPlaying = true;
            location34_Communication.enabled = false;
            while (animationQueue.Count > 0)
            {
                string animName = animationQueue.Dequeue();
                AnimationClip anim = FindAnimationClipByName(animName);
                if ( anim!=null)
                {
                    //if (mitaAnimatorFunctions.anim.runtimeAnimatorController != runtimeAnimatorController) mitaAnimatorFunctions.anim.runtimeAnimatorController = runtimeAnimatorController;
                    MelonLogger.Msg($"Crossfade");
                    MelonLogger.Msg($"Now playing: {animName}");
                    mitaAnimatorFunctions.anim.CrossFade(animName, 0.25f);
                    yield return WaitForAnimationCompletion(anim, false, 0.25f);
                }

                else 
                {
                    anim = AssetBundleLoader.LoadAnimationClipByName(bundle, animName);
                    if (anim != null)
                    {
                        MelonLogger.Msg($"Usual case");
                        mitaAnimatorFunctions.AnimationClipSimpleNext(anim);
                        yield return WaitForAnimationCompletion(anim, false, 0.25f);
                    }
                    else
                    {
                        MelonLogger.Msg($"Not found state or clip");
                    }

                }
                // Ждем завершения анимации
                
                
            }
            MelonLogger.Msg("Ended quque");
            animator.CrossFade(currentIdleAnim,0.25f);
            location34_Communication.enabled = true;
            isPlaying = false;
        }

        static private IEnumerator WaitForAnimationCompletion(AnimationClip animation, bool isCustomAnimation, float fadeDuration)
        {
            MelonLogger.Msg($"Begin WaitForAnimationCompletion");

            // Пока не работает, идеале отследить переходы
            if (isCustomAnimation)
            {

                
                // Для анимаций через Animator Controller
                float startTime = Time.time;

                MelonLogger.Msg($"Before while");
                // Ожидаем начала перехода
                while (animator.IsInTransition(0) && Time.time - startTime < fadeDuration)
                {
                    yield return null;
                }
                MelonLogger.Msg($"After while");
                // Ожидаем завершения анимации
                AnimatorStateInfo stateInfo;
                do
                {
                    stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    MelonLogger.Msg($"State = {stateInfo.nameHash},{stateInfo.shortNameHash}");
                    yield return null;
                }
                while (stateInfo.IsName(animation.name) && stateInfo.normalizedTime < 1.0f);
            }
            else
            {
                // Для обычных анимаций без transitions
                yield return new WaitForSeconds(animation.length + fadeDuration);
            }
        }

        static public void setIdleAnimation(string animName)
        {

            

            if (bundle == null)
            {
                bundle = AssetBundleLoader.LoadAssetBundle("assetbundle");
            }

            
            if (!string.IsNullOrEmpty(animName))
            {
                try
                {
                    currentIdleAnim = animName;
                    MelonLogger.Error($"111");
                    AnimationClip anim = FindAnimationClipByName(animName);
                    if (anim == null) anim = AssetBundleLoader.LoadAnimationClipByName(bundle, animName);
                    MelonLogger.Error($"222: {anim.name}");
                    location34_Communication.mitaAnimationIdle = anim;
                   // if (overrideController == null) overrideController = animator.runtimeAnimatorController as AnimatorOverrideController;
                   // MelonLogger.Error($"333: {overrideController.name}");
                   // overrideController.SetClip(idleAnimation, anim, true);
                   // MelonLogger.Error($"444");
                   
                }
                catch (Exception e)
                {

                    MelonLogger.Error($"setIdleAnimation: {e}");
                }

            }

        }
        static public void setIdleWalk(string animName)
        {
            if (bundle == null)
            {
                bundle = AssetBundleLoader.LoadAssetBundle("assetbundle");
            }

            if (!string.IsNullOrEmpty(animName))
            {
                AnimationClip anim = FindAnimationClipByName(animName);
                if (anim == null) anim = AssetBundleLoader.LoadAnimationClipByName(bundle, animName);

                location34_Communication.mitaAnimationWalk = anim;
                mitaAnimatorFunctions.AnimationClipWalk(anim);
            }


        }

        // Очистка очереди (опционально)
        static public void ClearQueue()
        {
            animationQueue.Clear();
            isPlaying = false;
        }
    }

}