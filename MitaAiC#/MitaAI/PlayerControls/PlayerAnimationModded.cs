using Il2Cpp;
using Il2CppSteamworks;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using Il2CppColorful;

namespace MitaAI
{



    public static class PlayerAnimationModded
    {
        public enum PlayerMovement
        {
            normal,
            sit
        }
        public static PlayerMovement currentPlayerMovement = PlayerMovement.normal;

        static ObjectAnimationPlayer objectAnimationPlayer;
        static private Queue<AnimationClip> animationQueue = new Queue<AnimationClip>();
        static private bool isPlaying = false;
        static PlayerMove playerMove;
        public static Dictionary<string, AnimationClip> PlayerAnimations { get; private set; } = new Dictionary<string, AnimationClip>();

        public static AnimationClip getPlayerAnimationClip(string name) {

            if (PlayerAnimations.ContainsKey(name)) return PlayerAnimations[name];
            else return null;
        }


        public static void Init(GameObject player, Transform worldHouse, PlayerMove _playerMove)
        {
            objectAnimationPlayer = player.AddComponent<ObjectAnimationPlayer>();
            playerMove = _playerMove;
            FindPlayerAnimationsRecursive(worldHouse);
            //FindPlayerAnimationsRecursive(MitaCore.worldTogether);
            // Ищем анимации в DontDestroyOnLoad

            FindPlayerAnimationsInDontDestroyOnLoad();
            foreach (var el in PlayerAnimations) MelonLogger.Msg($"Player clip {el.Key}");

        }

        public static void Check()
        {
            foreach (var el in PlayerAnimations) MelonLogger.Msg($"Player clip {el.Key}");
        }

        private static void FindPlayerAnimationsInDontDestroyOnLoad()
        {
            // Получаем корневой объект сцены DontDestroyOnLoad
            GameObject[] dontDestroyOnLoadObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (var obj in dontDestroyOnLoadObjects)
            {
                // Проверяем, что объект находится в сцене DontDestroyOnLoad
                if (obj.hideFlags == HideFlags.None && obj.scene.name == "DontDestroyOnLoad")
                {
                    // Рекурсивно ищем анимации
                    FindPlayerAnimationsRecursive(obj.transform);
                }
            }
        }
        public static void FindPlayerAnimationsRecursive(Transform parent)
        {
            if (parent == null) return;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null) continue;

                var animator = child.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    var clips = animator.runtimeAnimatorController.animationClips;
                    if (clips != null)
                    {
                        foreach (var clip in clips)
                        {
                            //MelonLogger.Msg($"Some clip {clip.name}");
                            if (clip != null && clip.name.Contains("Player", StringComparison.OrdinalIgnoreCase) && !PlayerAnimations.ContainsKey(clip.name))
                            {
                                if (!PlayerAnimations.ContainsKey(clip.name)) PlayerAnimations[clip.name] = clip;
                                else if (clip != PlayerAnimations[clip.name]) PlayerAnimations[clip.name + "1"] = clip;
                            }
                        }
                    }
                }
                ObjectAnimationPlayer oap = child.GetComponent<ObjectAnimationPlayer>();
                if (oap != null)
                {
                    if (oap.animationStart != null) 
                    { 
                        if (!PlayerAnimations.ContainsKey(oap.animationStart.name) ) PlayerAnimations[oap.animationStart.name] = oap.animationStart; 
                        else if( oap.animationStart!= PlayerAnimations[oap.animationStart.name] ) PlayerAnimations[oap.animationStart.name+"1"] = oap.animationStart;
                    }
                    if (oap.animationLoop != null)
                    {
                        if (!PlayerAnimations.ContainsKey(oap.animationLoop.name)) PlayerAnimations[oap.animationLoop.name] = oap.animationLoop;
                        else if (oap.animationLoop != PlayerAnimations[oap.animationLoop.name])  PlayerAnimations[oap.animationLoop.name + "1"] = oap.animationLoop;
                    }
                    if (oap.animationStop != null)
                    {
                        if (!PlayerAnimations.ContainsKey(oap.animationStop.name)) PlayerAnimations[oap.animationStop.name] = oap.animationStop;
                        else if (oap.animationStop != PlayerAnimations[oap.animationStop.name])  PlayerAnimations[oap.animationStop.name + "1"] = oap.animationStop;
                    }
                }

                FindPlayerAnimationsRecursive(child); // Рекурсивный вызов для вложенных объектов
            }
        }
        static public void EnqueueAnimation(string animName)
        {
            MelonLogger.Msg("EnqueueAnimation start");
            if (!PlayerAnimations.ContainsKey(animName)) return;
            AnimationClip anim = PlayerAnimations[animName];

            MelonLogger.Msg($"Found {anim.name}");

            try
            {


                if (anim != null)
                {
                    anim.events = Array.Empty<AnimationEvent>();
                    animationQueue.Enqueue(anim);
                    MelonLogger.Msg($"Added to queue: {anim.name}");

                    if (!isPlaying)
                    {
                        MelonCoroutines.Start(ProcessQueue());
                    }
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg("Animation error: " + e);
            }
        }

        // Корутина для последовательного проигрывания
        static private IEnumerator ProcessQueue()
        {
            isPlaying = true;
            while (animationQueue.Count > 0)
            {
                AnimationClip currentAnim = animationQueue.Dequeue();

                if (currentAnim != null)
                {
                    MelonLogger.Msg($"Now playing: {currentAnim.name} ({currentAnim.length}s)");
                    playAnim(currentAnim);

                    // Ждем завершения анимации
                    yield return new WaitForSeconds(currentAnim.length);
                }
            }
            isPlaying = false;

            yield return new WaitForSeconds(0.5f);
            MelonLogger.Msg($"animationQueue ended");
            try
            {

                playerMove.AnimationStop();
                MelonLogger.Msg($"AnimationStop stopped");
            }
            catch (Exception ex)
            {
                MelonLogger.Msg($"Problem with AnimationStop {ex}");

            }
            
            
        }


        public static void playAnim(AnimationClip animStart, AnimationClip animLoop=null,AnimationClip animEnd= null)
        {
            try
            {
                objectAnimationPlayer.animationStart = animStart;
                objectAnimationPlayer.animationLoop = animLoop;
                objectAnimationPlayer.animationStop = animEnd;

                objectAnimationPlayer.AnimationPlay();
            }
            catch (Exception ex)
            {

                MelonLogger.Msg($"Problem with playAnim {ex}");
            }

        }
        public static void playAnimObject(GameObject gameObject)
        {
            try
            {
                gameObject.GetComponent<ObjectAnimationPlayer>().AnimationPlay();

            }
            catch (Exception ex)
            {

                MelonLogger.Msg($"Problem with playAnimObject {ex}");
            }

        }

        public static void stopAnim() {
            playerMove.AnimationStop();
        }
    }
}
