using Il2Cpp;
using MelonLoader;
using System.Collections;
using UnityEngine;

public static class MitaAnimationModded
{
    static private Queue<AnimationClip> animationQueue = new Queue<AnimationClip>();
    static private bool isPlaying = false;
    static private Il2CppAssetBundle bundle;
    static Animator_FunctionsOverride mitaAnimatorFunctions;
    static Location34_Communication location34_Communication;
    // Основной метод для добавления анимации в очередь

    static public void init(Animator_FunctionsOverride _mitaAnimatorFunctions, Location34_Communication _location34_Communication)
    {
        // Получаем компонент Animator_FunctionsOverride из текущего объекта
        mitaAnimatorFunctions = _mitaAnimatorFunctions;
        location34_Communication = _location34_Communication;

        bundle = AssetBundleLoader.LoadAssetBundle("assetbundle");
        if (mitaAnimatorFunctions == null)
        {
            Debug.LogError("Animator_FunctionsOverride component not found on this object!");
        }
    }

    static public void EnqueueAnimation(string animName = "")
    {
        if (bundle == null)
        {
            bundle = AssetBundleLoader.LoadAssetBundle("assetbundle");
        }

        AnimationClip anim = null;
        try
        {
            if (!string.IsNullOrEmpty(animName))
            {
                anim = AssetBundleLoader.LoadAnimationClipByName(bundle, animName);
            }
            else
            {
                anim = AssetBundleLoader.LoadRandomAnimationClip(bundle);
            }

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
        location34_Communication.enabled = false;
        while (animationQueue.Count > 0)
        {
            AnimationClip currentAnim = animationQueue.Dequeue();

            if (currentAnim != null)
            {
                MelonLogger.Msg($"Now playing: {currentAnim.name} ({currentAnim.length}s)");
                mitaAnimatorFunctions.AnimationClipSimpleNext(currentAnim);

                // Ждем завершения анимации
                yield return new WaitForSeconds(currentAnim.length);
            }
        }
        location34_Communication.enabled = true;
        isPlaying = false;
    }

    // Очистка очереди (опционально)
    static public void ClearQueue()
    {
        animationQueue.Clear();
        isPlaying = false;
    }
}