using MelonLoader;
using MelonLoader.Utils;
using System.IO;
using UnityEngine;

public static class AssetBundleLoader
{
    /// <summary>
    /// Загружает AssetBundle и возвращает его.
    /// </summary>
    /// <param name="bundleName">Имя файла AssetBundle (с расширением).</param>
    /// <returns>Загруженный AssetBundle или null, если загрузка не удалась.</returns>
    public static Il2CppAssetBundle LoadAssetBundle(string bundleName)
    {
        // Путь к AssetBundle (например, рядом с файлом мода)
        string bundlePath = Path.Combine(MelonEnvironment.ModsDirectory, bundleName);

        if (!File.Exists(bundlePath))
        {
            MelonLoader.MelonLogger.Error($"Not found {bundleName}  {bundlePath}");
            return null;
        }

        MelonLoader.MelonLogger.Error("Before loading.");
        // Загрузка AssetBundle
        Il2CppAssetBundle bundle = Il2CppAssetBundleManager.LoadFromFile(bundlePath);

        if (bundle == null)
        {
            MelonLoader.MelonLogger.Error("Not loaded AssetBundle.");
            return null;
        }

        MelonLoader.MelonLogger.Msg($"AssetBundle {bundleName} успешно загружен.");
        return bundle;
    }

    /// <summary>
    /// Загружает объект из указанного AssetBundle.
    /// </summary>
    /// <param name="bundle">Загруженный AssetBundle.</param>
    /// <param name="assetName">Имя объекта в AssetBundle.</param>
    /// <returns>Созданный объект или null, если загрузка не удалась.</returns>
    public static GameObject LoadAndInstantiateAsset(AssetBundle bundle, string assetName)
    {
        if (bundle == null)
        {
            MelonLoader.MelonLogger.Error("AssetBundle не задан.");
            return null;
        }

        GameObject prefab = bundle.LoadAsset<GameObject>(assetName);
        if (prefab != null)
        {
            GameObject instance = GameObject.Instantiate(prefab);
            MelonLoader.MelonLogger.Msg($"Успешно загружен и создан объект: {prefab.name}");
            return instance;
        }
        else
        {
            MelonLoader.MelonLogger.Error($"Не удалось найти объект с именем {assetName} в AssetBundle.");
            return null;
        }
    }
    public static List<AnimationClip> LoadAllAnimationClips(Il2CppAssetBundle bundle)
    {

        List<AnimationClip> animationClips = new List<AnimationClip>();

        if (bundle == null)
        {
            MelonLogger.Msg("AssetBundle not recieced!");
            return animationClips;
        }

        // Получаем список всех имён ассетов в бандле
        string[] assetNames = bundle.GetAllAssetNames();

        foreach (string assetName in assetNames)
        {
            // Попытка загрузить ассет как AnimationClip
            AnimationClip clip = bundle.LoadAsset<AnimationClip>(assetName);
            if (clip != null)
            {
                animationClips.Add(clip);
                Debug.Log($"Loaded AnimationClip: {clip.name}");
            }
        }

        if (animationClips.Count == 0)
        {
            Debug.LogWarning("AssetBundle count = 0");
        }

        return animationClips;
    }

    public static AnimationClip LoadAnimationClipByName(Il2CppAssetBundle bundle, string clipName)
    {
        if (bundle == null)
        {
            MelonLogger.Msg("AssetBundle null!");
            return null;
        }

        if (string.IsNullOrEmpty(clipName))
        {
            MelonLogger.Msg("name empty AnimationClip!");
            return null;
        }

        // Добавляем расширение .anim, если его нет
        if (!clipName.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
        {
            clipName += ".anim";
        }

        // Получаем список всех имён ассетов в бандле
        string[] assetNames = bundle.GetAllAssetNames();

        foreach (string assetName in assetNames)
        {
            // Проверка имени ассета без расширения и сравнение с искомым
            if (string.Equals(Path.GetFileName(assetName), clipName, StringComparison.OrdinalIgnoreCase))
            {
                // Загружаем ассет как AnimationClip
                AnimationClip clip = bundle.LoadAsset<AnimationClip>(assetName);
                if (clip != null)
                {
                    MelonLogger.Msg($"Found and loaded AnimationClip: {clip.name}");
                    return clip;
                }
            }
        }

        MelonLogger.Msg($"AnimationClip '{clipName}' not found in AssetBundle.");
        return null;
    }

    static List<string> animationClipNames = new List<string>();
    public static AnimationClip LoadRandomAnimationClip(Il2CppAssetBundle bundle)
    {
        if (bundle == null)
        {
            MelonLogger.Msg("AssetBundle is null!");
            return null;
        }

        if (animationClipNames==null || animationClipNames.Count == 0)
        {
            // Получаем список всех ресурсов в AssetBundle
            string[] allAssetNames = bundle.GetAllAssetNames();
            if (allAssetNames == null || allAssetNames.Length == 0)
            {
                MelonLogger.Msg("No assets found in the AssetBundle.");
                return null;
            }

            // Фильтруем только анимации

            animationClipNames = allAssetNames
                .Where(name => name.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
 

        if (animationClipNames.Count == 0)
        {
            MelonLogger.Msg("No AnimationClips found in the AssetBundle.");
            return null;
        }

        // Выбираем случайное имя анимации
        int randomIndex = new System.Random().Next(animationClipNames.Count);
        string randomClipName = animationClipNames[randomIndex];

        // Загружаем анимацию по имени
        AnimationClip randomClip = bundle.LoadAsset<AnimationClip>(randomClipName);

        if (randomClip != null)
        {
            MelonLogger.Msg($"Random AnimationClip selected: {randomClip.name}");
        }
        else
        {
            MelonLogger.Msg($"Failed to load AnimationClip: {randomClipName}");
        }

        return randomClip;
    }

}
