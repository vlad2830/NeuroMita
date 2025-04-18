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
using Harmony;
namespace MitaAI.Mita
{


    public static class MitaAnimationModded
    {
        //static private Queue<(string,float,float)> animationQueue = new Queue<(string,float,float)>();
        static private LinkedList<MitaActionAnimation> animationList = new LinkedList<MitaActionAnimation>();

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

        const string initIdle = "Mita Idle_2";
        const string initWalk = "Mita Walk_1";
        static public string currentIdleAnim = "Mita Idle_2";
        static public string currentWalkAnim = "Mita Walk_1";

        static public void init(Animator_FunctionsOverride _mitaAnimatorFunctions, Location34_Communication _location34_Communication, bool changeAnimationController = true, bool changeAnimation = true, characterType character = characterType.None)
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


            try
            {
                runtimeAnimatorController = AssetBundleLoader.LoadAnimatorControllerByName(bundle, "Mita_1.controller");
                overrideController = new AnimatorOverrideController(runtimeAnimatorController);
                
                mitaAnimatorFunctions.animOver = overrideController;
                if (changeAnimationController==true)
                {
                    MelonLogger.Msg("Change Animation controller");
                    

                    animator = MitaCore.Instance.MitaPersonObject.GetComponent<Animator>();
                    animator.runtimeAnimatorController = overrideController;
                    animator.SetTrigger("NextLerp");
                    animator.Rebind();

                    if (changeAnimation)
                    {
                        idleAnimation = FindAnimationClipInControllerByName("Idle");
                        // Пока что так))
                        switch (character)
                        {

                            case characterType.Player:
                                break;
                            case characterType.None:
                                break;
                            case characterType.Crazy:
                                break;
                            case characterType.Cappy:
                                idleAnimation = FindAnimationClipInControllerByName("Mita Hands Down Idle");
                                idleWalkAnimation = FindAnimationClipInControllerByName("Mita Walk_7");
                                setIdleAnimation("Mita Hands Down Idle");
                                setIdleWalk("Mita Walk_7");
                                break;
                            case characterType.Kind:
                                break;
                            case characterType.Cart_portal:
                                break;
                            case characterType.ShortHair:
                                break;
                            case characterType.Cart_divan:
                                break;
                            case characterType.Mila:
                                idleWalkAnimation = FindAnimationClipInControllerByName("MitaWalkMila");
                                idleAnimation = FindAnimationClipInControllerByName("Mila Stay T");
                                setIdleWalk("MitaWalkMila");
                                setIdleAnimation("Mila Stay T");
                                break;
                            case characterType.Sleepy:
                                break;
                            case characterType.Creepy:
                                break;
                            case characterType.GameMaster:
                                break;
                            default:
                                break;
                        }
                    }

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
            // Если запрещено двигаться
            //ObjectAnimationMita.finishWorkingOAM();
            //resetToIdleAnimation(false);

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
                        MitaMovement.movementStyle = MovementStyles.layingOnTheFloorAsDead;
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
                        PlayerAnimationModded.currentPlayerMovement = PlayerMovementType.normal;
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
                        MitaMovement.movementStyle = MovementStyles.cryingOnTheFloor;
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
                            if (MitaCore.Instance.currentCharacter == characterType.Kind)
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
                            MelonLogger.Warning($"Added to que defult case {animName}");
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
        
        // Даже головой не крутит
        private static readonly MovementStyles[] MovementStylesNoMovingAtAll =
        {
            MovementStyles.cryingOnTheFloor,
            MovementStyles.layingOnTheFloorAsDead

        };
        
        // Крутит головой
        private static readonly MovementStyles[] MovementStylesNoBodyLooking =
        {
            MovementStyles.sitting
        };

        // Крутит головой и телом, но не ходит
        private static readonly MovementStyles[] MovementStylesNoWalking =
        {
            //MovementStyles.sitting
        };

        // Отвечает за перемещение и поворот миты.
        public static void checkCanMoveRotateLook(bool ignoreInteractionCondition = false)
        {
            if (MitaState.currentMitaState == MitaStateType.interaction && !ignoreInteractionCondition) return;

            // Если запрещено двигаться
            if (MovementStylesNoMovingAtAll.Contains(MitaMovement.movementStyle))
            {
                MitaCore.Instance.MitaLook.enabled = false;
                location34_Communication.ActivationCanWalk(false);
            }
            else if (MovementStylesNoBodyLooking.Contains(MitaMovement.movementStyle))
            {
                MitaCore.Instance.MitaLook.enabled = true;
                MitaCore.Instance.MitaLook.canRotateBody = false;
                location34_Communication.ActivationCanWalk(false);
            }
            else if (MovementStylesNoWalking.Contains(MitaMovement.movementStyle))
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

        static public void EnqueueAnimation(string animName = "", float crossfadeLen = 0.25f,
                                           float timeAfter = 0, bool makeFirst = false,
                                           bool avoidStateSettings = false)
        {
            HandleAnimationEnqueue(() =>
                new MitaActionAnimation(
                    animName,
                    crossfadeLen,
                    crossfadeLen,
                    timeAfter,
                    timeAfter,
                    avoidStateSettings
                ),
                animName,
                makeFirst,
                $"Start Que cor from {animName}"
            );
        }

        static public void EnqueueAnimation(ObjectAnimationMita objectAnimationMita,
                                           float crossfadeLen = 0.25f, float timeAfter = 0,
                                           float delayAfter = 0, bool makeFirst = false)
        {
            HandleAnimationEnqueue(() =>
                new MitaActionAnimation(
                    objectAnimationMita.mitaAmimatedName,
                    objectAnimationMita.AnimationTransitionDuration,
                    crossfadeLen,
                    timeAfter,
                    objectAnimationMita,
                    delayAfter
                ),
                objectAnimationMita.mitaAmimatedName,
                makeFirst,
                $"Start Que cor from OAM {objectAnimationMita.mitaAmimatedName}",
                "objectAnimationMita "
            );
        }

        private static void HandleAnimationEnqueue(Func<MitaActionAnimation> animationFactory,
                                                  string animationName,
                                                  bool makeFirst,
                                                  string startMessage,
                                                  string logPrefix = "")
        {
            try
            {
                var animation = animationFactory();
                AddAnimationToList(animation, makeFirst);

                MelonLogger.Msg($"Added {logPrefix}to queue: {animationName}");

                StartQueueProcessingIfNeeded(startMessage);
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Animation error: {e}");
            }
        }

        private static void AddAnimationToList(MitaActionAnimation animation, bool makeFirst)
        {
            if (makeFirst)
                animationList.AddFirst(animation);
            else
                animationList.AddLast(animation);
        }

        private static void StartQueueProcessingIfNeeded(string startMessage)
        {
            if (isPlaying) return;

            MelonLogger.Msg(startMessage);
            MelonCoroutines.Start(ProcessQueue());
        }


        static AnimationClip FindAnimationClipInControllerByName(string animationName)
        {
            if (runtimeAnimatorController == null) return null;

            AnimationClip[] clips = runtimeAnimatorController.animationClips;

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

            while (animationList.Count > 0)
            {

                MitaActionAnimation animObject = animationList.First();
                animationList.RemoveFirst();

                MelonLogger.Msg($"ANIM LIST: count {animationList.Count} Now playing: {animObject.animName} {animObject.animationType} {animObject.ObjectAnimationMita}");

                if (animObject.ObjectAnimationMita != null)
                {
                    yield return MelonCoroutines.Start( processObjectAnimationMita(animObject) );
                }
                else
                {
                    yield return MelonCoroutines.Start( processAnimationMita(animObject) );

                }
                
                
                if (!animObject.avoidStateSettings) checkCanMoveRotateLook();
                
                yield return new WaitForSeconds(animObject.delay_after);

            }

            MitaMovement.ChoseStyle(currentIdleAnim);
            checkCanMoveRotateLook();
            MelonLogger.Msg($"Ended quque currentIdleAnim {currentIdleAnim}");
            animator.CrossFade("Idle",0.25f);
            
            
            isPlaying = false;
        }
        
        
        static IEnumerator processObjectAnimationMita(MitaActionAnimation animObject)
        {
            ObjectAnimationMita objectAnimationMita = animObject.ObjectAnimationMita;
            MelonLogger.Msg($"Now playing OAM: {objectAnimationMita.name} {objectAnimationMita.tip}");
            objectAnimationMita.Play();


            float beforeWalk = Time.unscaledTime;

            yield return new WaitForSeconds(0.25f);
            while (objectAnimationMita.isWalking && Time.unscaledTime - beforeWalk < 20f) yield return new WaitForSeconds(0.25f);
            

            MelonLogger.Msg($"Now ended walking OAM: {objectAnimationMita.name} {objectAnimationMita.tip}");
        }
        static IEnumerator processAnimationMita(MitaActionAnimation animObject)
        {

            string animName = animObject.animName;
            float crossfade_len = animObject.begin_crossfade;
            float delay_after = animObject.delay_after;
            bool avoidStateSettings = animObject.avoidStateSettings;
            var ObjectAnimationMita = animObject.ObjectAnimationMita;

            

            MelonLogger.Msg($"Crossfade");
            MelonLogger.Msg($"Now playing: {animName}");

            mitaAnimatorFunctions.anim.CrossFade(animName, crossfade_len);
                    
            AnimationClip anim = FindAnimationClipInControllerByName(animName);

            if (anim != null)
            { 
                // Вот это надо донастроить
                if (anim.events.Count > 0)
                {
                    // yield return new WaitForSeconds(anim.events[0].floatParameter);
                    MitaCore.Instance.MitaObject.GetComponent<EventsProxy>().OnAnimationEvent(anim.events[0].stringParameter);
                }

                MelonLogger.Msg($"Before WaitForAnimationCompletion");

                yield return new WaitForSeconds(Math.Min(anim.length + 0.25f, 30f));

                MelonLogger.Msg($"After WaitForAnimationCompletion");
            }

            

            if (animName == "Mita Kick")
            {
                // После завершения анимации удара деактивируем оба объекта
                bat.active = false;
                pipe.active = false;
            }
            


        }

        static public void resetToIdleAnimation(bool toInitAnim = true, bool total_clear = false,bool needEnque = false)
        {
            string anim = toInitAnim ? initIdle : currentIdleAnim;

            if (needEnque) EnqueueAnimation(anim);

            setIdleAnimation(anim);
            

            if (total_clear) ClearQueue();

            

        }
        static public void setIdleAnimation(string animName)
        {
           
            if (!string.IsNullOrEmpty(animName))
            {
                try
                {
                    currentIdleAnim = animName;
                    AnimationClip anim = FindAnimationClipInControllerByName(animName);
                    location34_Communication.mitaAnimationIdle = anim;
                    MitaMovement.ChoseStyle(animName);
                    checkCanMoveRotateLook();
                    mitaAnimatorFunctions.ReAnimationClip(initIdle, anim);

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
            
            AnimationClip anim = FindAnimationClipInControllerByName(animName);

            if (anim == null)
            {
                MelonLogger.Error($"setIdleWalk {animName} is null");
            }
            else
            {
                currentWalkAnim = animName;
                location34_Communication.mitaAnimationWalk = anim;
                mitaAnimatorFunctions.ReAnimationClip(initWalk, anim);
                location34_Communication.AnimationReWalk(anim);
            }       


        }

        // Очистка очереди (опционально)
        static public void ClearQueue()
        {
            animationList.Clear();
            isPlaying = false;
        }
    
    

    
    }

}
