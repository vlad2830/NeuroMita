using Il2Cpp;
using MelonLoader;
using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using MelonLoader.ICSharpCode.SharpZipLib;
using Il2CppSystem.Threading.Tasks;


namespace MitaAI
{
    public static class PlayerHands
    {
        static Transform RightItem;
        static Transform LeftItem;
        static Dictionary<int,(Transform,Vector3,Vector3)> OldParents = new Dictionary<int, (Transform, Vector3, Vector3)>();
        public static void init(Transform playerObject)
        {
            RightItem = playerObject.Find("RightItem FixPosition");
            LeftItem  = playerObject.Find("LeftItem FixPosition");
        }

        public static void takeInHand(GameObject gameObject, bool right, Vector3 localPos, Vector3 localRot)
        {
            var Item = right ? RightItem : LeftItem;
            if (Item == null) { MelonLogger.Error("Hand is null!!!"); }
            else MelonLogger.Msg("Hand is non null)");

            OldParents[gameObject.GetInstanceID()] = (gameObject.transform, gameObject.transform.localPosition, gameObject.transform.eulerAngles);
            gameObject.transform.SetParent(Item);
            gameObject.transform.localPosition = localPos;
            gameObject.transform.localEulerAngles = localRot;
        }
        public static void free(GameObject gameObject, bool right)
        {
            var Item = (right ? RightItem : LeftItem);

            if (Item == null) { MelonLogger.Error("Hand is null!!!"); }

            if (OldParents.ContainsKey(gameObject.GetInstanceID()))
            {
                MelonLogger.Msg("Found in dict");
                Item.transform.SetParent(OldParents[gameObject.GetInstanceID()].Item1);
                Item.localPosition = OldParents[gameObject.GetInstanceID()].Item2;
                Item.localEulerAngles = OldParents[gameObject.GetInstanceID()].Item3;
            }


        }
        public static void free(GameObject gameObject, bool right, Transform newParent,Vector3 localPos,Vector3 localRot)
        {
            var Item = (right ? RightItem : LeftItem);
            Item.SetParent(newParent);
            Item.localPosition = localPos;
            Item.localEulerAngles = localRot;

        }

    }


}