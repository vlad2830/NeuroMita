using Il2Cpp;
using MelonLoader;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MitaAI
{
    static class MitaGames
    {
        #region HuntingWithKnife


        #endregion

        #region Manekens

        public static bool manekenGame = false;
        public static List<GameObject> activeMakens = new List<GameObject>();
        public static float blinkTimer = 7f;
        public static GameObject ManekenTemplate;
        public static void spawnManeken()
        {
            GameObject someManeken = GameObject.Instantiate(ManekenTemplate, MitaCore.worldHouse.Find("House"));
            someManeken.SetActive(true);
            activeMakens.Add(someManeken);

            if (manekenGame == false)
            {
                manekenGame = true;
                MelonCoroutines.Start(CheckManekenGame());
            }
            someManeken.transform.SetPositionAndRotation(MitaCore.Instance.GetRandomLoc().position, MitaCore.Instance.GetRandomLoc().rotation);


        }
        public static void TurnAllMenekens(bool on)
        {
            if (activeMakens.Count <= 0) return;

            foreach (GameObject m in activeMakens)
            {
                if (on) m.transform.FindChild("MitaManeken 1").gameObject.GetComponent<Mob_Maneken>().ResetManeken();
                else m.transform.FindChild("MitaManeken 1").gameObject.GetComponent<Mob_Maneken>().DeactivationManeken();

            }
            manekenGame = on;
        }
        public static void removeAllMenekens()
        {
            foreach (GameObject m in activeMakens)
            {
                GameObject.Destroy(m);
            }
            activeMakens.Clear();
            manekenGame = false;
        }
        private static IEnumerator CheckManekenGame()
        {
            while (true)
            {
                try
                {
                    if (!manekenGame) yield break;

                    if (MitaCore.blackScreen != null && MitaCore.playerCamera != null)
                    {
                        MitaCore.blackScreen.BlackScreenAlpha(0.75f);
                        MitaCore.playerCamera.GetComponent<Camera>().enabled = false;
                        MelonCoroutines.Start(Utils.ToggleComponentAfterTime(MitaCore.playerCamera.GetComponent<Camera>(), 0.75f)); // Отключит playerCamera через 1 секунду
                    }

                    yield return new WaitForSeconds(blinkTimer); // Ждем 7 секунд перед следующим циклом
                }
                finally { }

            }
        }

        #endregion
    
    
    }
}
