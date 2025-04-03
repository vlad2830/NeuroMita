using Il2Cpp;

using MelonLoader;
using System.Collections;
using UnityEngine;
using System.Text.RegularExpressions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine.Playables;
using UnityEngine.AI;
using Il2CppRootMotion.FinalIK;
namespace MitaAI.Mita
{
    public static class MitaAnimationModded
    {
        //static private Queue<(string,float,float)> animationQueue = new Queue<(string,float,float)>();
        static private Queue<MitaActionAnimation> animationQueue = new Queue<MitaActionAnimation>();

        static private bool isPlaying = false;
        static private Il2CppAssetBundle bundle;
        public static Animator_FunctionsOverride mitaAnimatorFunctions;
        public static Location34_Communication location34_Communication;
        static AnimationClip idleAnimation;
        static AnimationClip idleWalkAnimation;
        // Основной метод для добавления анимации в очередь
        public static RuntimeAnimatorController runtimeAnimatorController;
        public static AnimatorOverrideController overrideController;
        public static Animator animator;
        static NavMeshAgent mitaNavMeshAgent;      
        
        ///public static LookAtIK = 

        public static GameObject bat;
        public static GameObject pipe;

        static public string currentIdleAnim = "Idle";


        static public void init(Animator_FunctionsOverride _mitaAnimatorFunctions, Location34_Communication _location34_Communication, bool changeAnimationController = true, bool changeAnimation = true, MitaCore.character character = MitaCore.character.None)
        {
            // Получаем компонент Animator_FunctionsOverride из текущего объекта
            mitaAnimatorFunctions = _mitaAnimatorFunctions;
            location34_Communication = _location34_Communication;

            if (bundle == null)
            {
                bundle = MitaCore.bundle; //AssetBundleLoader.LoadAssetBundle("assetbundle");
            }
            
            if (mitaAnimatorFunctions == null)
            {
                MelonLogger.Msg("Animator_FunctionsOverride component not found on this object!");
            }

            if (changeAnimation)
            {
                idleAnimation = FindAnimationClipByName("Idle");
                // Пока что так))
                switch (character)
                {
                    
                    case MitaCore.character.Player:
                        break;
                    case MitaCore.character.None:
                        break;
                    case MitaCore.character.Crazy:
                        break;
                    case MitaCore.character.Cappy:
                        idleAnimation = FindAnimationClipByName("Mita Hands Down Idle");
                        idleWalkAnimation = FindAnimationClipByName("Mita Walk_7");
                        setIdleAnimation("Mita Hands Down Idle");
                        setIdleWalk("Mita Walk_7");
                        break;
                    case MitaCore.character.Kind:
                        break;
                    case MitaCore.character.Cart_portal:
                        break;
                    case MitaCore.character.ShortHair:
                        break;
                    case MitaCore.character.Cart_divan:
                        break;
                    case MitaCore.character.Mila:
                        idleAnimation = FindAnimationClipByName("MitaWalkMila");
                        idleWalkAnimation = FindAnimationClipByName("Mila Stay T");
                        setIdleAnimation("MitaWalkMila");
                        setIdleWalk("Mila Stay T");
                        break;
                    case MitaCore.character.Sleepy:
                        break;
                    case MitaCore.character.Creepy:
                        break;
                    case MitaCore.character.GameMaster:
                        break;
                    default:
                        break;
                }

 
                //idleAnimation = location34_Communication.mitaAnimationIdle;
                //if (idleAnimation == null) idleAnimation = AssetBundleLoader.LoadAnimationClipByName(bundle, "Mita Idle_2")

                //idleWalkAnimation = location34_Communication.mitaAnimationWalk;
                //if (idleWalkAnimation == null) idleWalkAnimation = AssetBundleLoader.LoadAnimationClipByName(bundle, "Mita Walk_1");
            }

            try
            {
                runtimeAnimatorController = AssetBundleLoader.LoadAnimatorControllerByName(bundle, "Mita_1.controller");
                if (changeAnimationController==true)
                {
                    MelonLogger.Msg("Change Animation controller");
                    

                    animator = MitaCore.Instance.MitaPersonObject.GetComponent<Animator>();
                    animator.runtimeAnimatorController = runtimeAnimatorController;
                    animator.SetTrigger("NextLerp");
                    animator.Rebind();


                }
                else
                {
                    MelonLogger.Msg("No Change Animation controller");
                    runtimeAnimatorController = animator.runtimeAnimatorController;
                }
               
                foreach (var item in runtimeAnimatorController.animationClips)
                {
                    setCustomAnimatiomEvents(item);
                }
                mitaNavMeshAgent = MitaCore.Instance.MitaPersonObject.GetComponent<NavMeshAgent>();

                MelonLogger.Msg("b!");
                //setIdleWalk("Mita Walk_1");

                if (changeAnimation){
                    animator.Rebind();
                    animator.Update(0);
                }
               

                
            }
            catch (Exception ex)
            {

                MelonLogger.Msg("Error custom controller"+ex);
            }
            
        }

        static private void setCustomAnimatiomEvents(AnimationClip anim)
        { 
            if (anim.name.Contains("Click")){

                anim.events = new Il2CppReferenceArray<AnimationEvent>(0);
                MelonLogger.Msg("AddedAnimationEvent for" + anim.name);
                EventsProxy.AddAnimationEvent(MitaCore.Instance.MitaObject, anim, "Mita Click");
                return;
            }


            switch (anim.name)
            {
                case "Mita TakeMita":
                    MelonLogger.Msg("AddedAnimationEvent for" + anim.name);
                    EventsProxy.AddAnimationEvent(MitaCore.Instance.MitaObject, anim, "TakePlayer");
                    break;
                case "Mita Kick":
                    MelonLogger.Msg("AddedAnimationEvent for" + anim.name);
                    EventsProxy.AddAnimationEvent(MitaCore.Instance.MitaObject, anim, anim.name);
                    break;
                default:
                    anim.events = new Il2CppReferenceArray<AnimationEvent>(0);
                    break;
            }
            
        }


        public static string setAnimation(string response)
        {
            // Регулярное выражение для извлечения эмоций
            string pattern = @"<a>(.*?)</a>";
            Match match = Regex.Match(response, pattern);

            string cleanedResponse = Regex.Replace(response, @"<a>.*?</a>", ""); // Очищаем от всех тегов

            string animName = "";
            string param1 = "";
            string param2 = "";
            string param3 = "";


            if (match.Success)
            {
                // Если эмоция найдена, устанавливаем её в переменную faceStyle
                string[] parts = match.Groups[1].Value.Split(',');

                animName = parts[0];
                param1 = parts.Length > 1 ? parts[1] : "";
                param2 = parts.Length > 2 ? parts[2] : "";
                param3 = parts.Length > 3 ? parts[3] : "";

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
                        MitaCore.movementStyle = MitaCore.MovementStyles.layingOnTheFloorAsDead;
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
                    case "Скинуть игрока":
                        PlayerAnimationModded.currentPlayerMovement = PlayerAnimationModded.PlayerMovement.normal;
                        EnqueueAnimation("Mita ThrowPlayer");
                        //EnqueueAnimation("Mita TakeMita Idle");

                        setIdleAnimation("Mita Idle_2");
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
                        setIdleAnimation("Mita IdleAngry");
                        break;
                    case "Сесть и плакать":
                        EnqueueAnimation("Mila CryNo");
                        setIdleAnimation("Mila CryNo");
                        MitaCore.movementStyle = MitaCore.MovementStyles.cryingOnTheFloor;
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
                        // Проверяем, какой персонаж сейчас активен

                        try
                        {
                            if (MitaCore.Instance.currentCharacter == MitaCore.character.Kind)
                            {
                                // Если активен Kind персонаж, используем трубу
                                if (pipe == null){
                                    pipe = MitaCore.worldBasement.Find("Mita Future/MitaPerson Future/RightItem/Tube Basement").gameObject;
                                }
                                pipe.active = true;
                                pipe.transform.SetParent(MitaCore.getMitaHand(MitaCore.Instance.MitaPersonObject));
                                pipe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                            }
                            else
                            {
                                // Для других персонажей используем биту
                                bat.active = true;
                                pipe.transform.SetParent(MitaCore.getMitaHand(MitaCore.Instance.MitaPersonObject));
                                pipe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                            }
                        }
                        catch (Exception ex)
                        {

                            MelonLogger.Error(ex);
                        }
                       
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

                    case "Круто протанцевать":
                        EnqueueAnimation("Mita HandsDance");
                        //EnqueueAnimation("DanceTest");
                        break;
                    case "Начать круто танцевать":
                        EnqueueAnimation("Mita HandsDance");
                        setIdleAnimation("Mita HandsDance");
                        //EnqueueAnimation("DanceTest");
                        //setIdleAnimation("DanceTest");
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

            // Если запрещено двигаться
            checkCanMoveRotateLook();


            // Возвращаем кортеж: лицо и очищенный текст
            return cleanedResponse;
        }
        
        // Даже головой не крутит
        private static readonly MitaCore.MovementStyles[] MovementStylesNoMovingAtAll =
        {
            MitaCore.MovementStyles.cryingOnTheFloor,
            MitaCore.MovementStyles.layingOnTheFloorAsDead

        };
        
        // Крутит головой
        private static readonly MitaCore.MovementStyles[] MovementStylesNoBodyLooking =
        {
            MitaCore.MovementStyles.sitting
        };

        // Крутит головой и телом, но не ходит
        private static readonly MitaCore.MovementStyles[] MovementStylesNoWalking =
        {
            //MitaCore.MovementStyles.sitting
        };

        // Отвечает за перемещение и поворот миты.
        public static void checkCanMoveRotateLook()
        {     
        
            // Если запрещено двигаться
            if (MovementStylesNoMovingAtAll.Contains(MitaCore.movementStyle))
            {
                MitaCore.Instance.MitaLook.enabled = false;
                location34_Communication.ActivationCanWalk(false);
            }
            else if (MovementStylesNoBodyLooking.Contains(MitaCore.movementStyle))
            {
                MitaCore.Instance.MitaLook.enabled = true;
                MitaCore.Instance.MitaLook.canRotateBody = false;
                location34_Communication.ActivationCanWalk(false);
            }
            else if (MovementStylesNoWalking.Contains(MitaCore.movementStyle))
            {
                MitaCore.Instance.MitaLook.enabled = true;
                MitaCore.Instance.MitaLook.canRotateBody = true;
                location34_Communication.ActivationCanWalk(false);
            }
            else
            {
                MitaCore.Instance.MitaLook.canRotateBody = true;
                MitaCore.Instance.MitaLook.enabled = true;
                location34_Communication.ActivationCanWalk(true);

            }

        }

        static public void EnqueueAnimation(string animName = "",float crossfade_len = 0.25f, float timeAfter = 0)
        {

            try
            {

                //animationQueue.Enqueue((animName, crossfade_len,timeAfter
                animationQueue.Enqueue(new MitaActionAnimation(animName,crossfade_len, crossfade_len,timeAfter));
                MelonLogger.Msg($"Added to queue: {animName}");

                if (!isPlaying)
                {
                    MelonCoroutines.Start(ProcessQueue());
                }

            }
            catch (Exception e)
            {
                MelonLogger.Msg("Animation error: " + e);
            }
        }
        static public void EnqueueAnimation(ObjectAnimationMita objectAnimationMita, float crossfade_len = 0.25f, float timeAfter = 0,float delay_after = 0)
        {

            try
            {

                //animationQueue.Enqueue((animName, crossfade_len,timeAfter
                animationQueue.Enqueue(new MitaActionAnimation(objectAnimationMita.mitaAmimatedName, objectAnimationMita.AnimationTransitionDuration, crossfade_len, timeAfter, objectAnimationMita,delay_after));
                MelonLogger.Msg($"Added objectAnimationMita to queue: {objectAnimationMita.mitaAmimatedName}");

                if (!isPlaying)
                {
                    MelonCoroutines.Start(ProcessQueue());
                }

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
                    MelonLogger.Msg($"{animationName} found in runtimeAnimatorController");
                    return clip;
                }
            }

            return null;
        }



        // Корутина для последовательного проигрывания
        static private IEnumerator ProcessQueue()
        {
            isPlaying = true;
            location34_Communication.ActivationCanWalk(false);
            while (animationQueue.Count > 0)
            {

                MitaActionAnimation animObject = animationQueue.Dequeue();
                string animName = animObject.animName;
                float crossfade_len = animObject.begin_crossfade;
                float delay_after = animObject.delay_after;
                ObjectAnimationMita objectAnimationMita = animObject.ObjectAnimationMita;
                AnimationClip anim = FindAnimationClipByName(animName);
                
                if (animObject.animationType == MitaActionAnimation.ActionAnimationType.ObjectAnimation)
                {
                    objectAnimationMita.Play();
                    //checkCanMoveRotateLook();
                    float beforeWalk = Time.unscaledDeltaTime;

                    while (objectAnimationMita.isWalking && Time.unscaledDeltaTime-beforeWalk<30f) yield return new WaitForSeconds(0.25f);

                    
                }
                else
                {
                    if (anim != null)
                    {

                        // while (isMitaWalking()) yield return null;

                        //if (mitaAnimatorFunctions.anim.runtimeAnimatorController != runtimeAnimatorController) mitaAnimatorFunctions.anim.runtimeAnimatorController = runtimeAnimatorController;
                        MelonLogger.Msg($"Crossfade");
                        MelonLogger.Msg($"Now playing: {animName}");
                        try
                        {
                            mitaAnimatorFunctions.anim.CrossFade(animName, crossfade_len);
                            MitaCore.Instance.Mita.MagnetOff();
                            if (anim.events.Count > 0)
                            {
                                MitaCore.Instance.MitaObject.GetComponent<EventsProxy>().OnAnimationEvent(anim.events[0]);
                            }

                            MelonLogger.Msg($"Finded animation event");
                        }
                        catch (Exception ex)
                        {

                            MelonLogger.Msg(ex);
                        }


                        MelonLogger.Msg($"zzz2");

                        yield return WaitForAnimationCompletion(anim, false, 0.25f);

                        if (animName == "Mita Kick")
                        {
                            // После завершения анимации удара деактивируем оба объекта
                            bat.active = false;
                            pipe.active = false;
                        }
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
                checkCanMoveRotateLook();
                yield return new WaitForSeconds(delay_after);




            }
            checkCanMoveRotateLook();
            MelonLogger.Msg($"Ended quque currentIdleAnim {currentIdleAnim}");
            animator.CrossFade(currentIdleAnim,0.25f);
            //location34_Communication.enabled = true;
            isPlaying = false;
        }
        static private bool isMitaWalking()
        {
            if (mitaAnimatorFunctions != null) return mitaNavMeshAgent.enabled;
            return false;
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
        static public void resetToIdleAnimation()
        {
            ClearQueue();
            EnqueueAnimation("Mita Idle_2");
            setIdleAnimation("Mita Idle_2");
        }
        static public void setIdleAnimation(string animName)
        {
           
            if (!string.IsNullOrEmpty(animName))
            {
                try
                {
                    currentIdleAnim = animName;
                    AnimationClip anim = FindAnimationClipByName(animName);
                    if (anim == null) anim = AssetBundleLoader.LoadAnimationClipByName(bundle, animName);
                    location34_Communication.mitaAnimationIdle = anim;

                    if (animName.Contains("sit") || animName.Contains("Sit"))
                    {
                        MitaCore.movementStyle = MitaCore.MovementStyles.sitting;
                    }
                    checkCanMoveRotateLook();

                }
                catch (Exception e)
                {

                    MelonLogger.Error($"setIdleAnimation: {e}");
                }

            }

        }
        static public void setIdleWalk(string animName)
        {

            if (string.IsNullOrEmpty(animName)) return;
            
            AnimationClip anim = FindAnimationClipByName(animName);
            if (anim == null) anim = AssetBundleLoader.LoadAnimationClipByName(bundle, animName);

            if (anim == null)
            {
                MelonLogger.Msg($"setIdleWalk {animName} is null");
            }
            else
            {
                location34_Communication.mitaAnimationWalk = anim;
                mitaAnimatorFunctions.ReAnimationClip("Mita Walk_1", anim);
                location34_Communication.AnimationReWalk(anim);
            }       


        }

        // Очистка очереди (опционально)
        static public void ClearQueue()
        {
            animationQueue.Clear();
            isPlaying = false;
        }
    
    
    
    
    
    
        public static void animateMitaObject(string anim)
        {




        }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    }

}
