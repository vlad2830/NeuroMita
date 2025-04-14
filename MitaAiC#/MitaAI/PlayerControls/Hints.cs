using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using static MelonLoader.InteropSupport;

namespace MitaAI
{
    public static class Hints
    {
        public static GameObject interfaceHintTeplate;
        // 0 0 0 локальнйо позиции это центр


        public static GameObject flyingHintTeplate;
        public static Transform interfaceCanvas;

        static Transform GetInterfaceCanvas()
        {
            if (interfaceCanvas == null)
            {
                interfaceCanvas = GameObject.Find("GameController/Interface").transform;
            }
            return interfaceCanvas;
        }

        public static Interface_KeyHint_Key CreateHint(string text, string key, Vector3 localPosition, bool isFlyingHint = false, GameObject parentObject = null,bool destroyAfter = true)
        {
            MelonLogger.Msg("!!! 222 %%% YES 222");

            GameObject template = isFlyingHint ? flyingHintTeplate : interfaceHintTeplate;
            Transform parent = isFlyingHint ?
                (parentObject != null ? parentObject.transform : null) :
                GetInterfaceCanvas();

            if (template == null || parent == null)
            {
                Debug.LogError("Hint template or parent is not set!");
                return null;
            }

            GameObject hintObject = GameObject.Instantiate(template, parent);
            hintObject.transform.localPosition = localPosition;

            Interface_KeyHint_Key interfaceKeyHint = hintObject.GetComponent<Interface_KeyHint_Key>();
            interfaceKeyHint.textDescription.text = text;
            interfaceKeyHint.nameKey = key;
            interfaceKeyHint.textKey.text = key;
            interfaceKeyHint.destroyAfter = destroyAfter;
            if (interfaceKeyHint.eventKeyDown == null) interfaceKeyHint.eventKeyDown = new UnityEngine.Events.UnityEvent();
            interfaceKeyHint.eventKeyDown.RemoveAllListeners();

            return interfaceKeyHint;
        }


        public static void createExitButton()
        {
            MelonLogger.Msg("!!! %%% YES");
            var hint = CreateHint("Закончить", "E",new Vector3(-350,-350,0));
            hint.eventKeyDown.AddListener((UnityAction)PlayerAnimationModded.stopAnim);
        }
    }
}