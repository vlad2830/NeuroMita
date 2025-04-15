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
        public static void init(Transform playerObject)
        {
            RightItem = playerObject.Find("RightItem FixPosition");
            RightItem = playerObject.Find("RightItem FixPosition");
        }

        public static void takeInHand(GameObject gameObject, bool right, Vector3 locapPos, Vector3 locelRot)
        {
            gameObject.transform.SetParent(right ? RightItem : LeftItem);
            gameObject.transform.localPosition = locapPos;
            gameObject.transform.localEulerAngles = locelRot;
        }
        public static void free(GameObject gameObject, bool right)
        {
            //gameObject.transform.SetParent(right ? RightItem : LeftItem);

        }

    }


}