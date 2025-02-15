using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MitaAI.WorldModded.Games
{
    internal class MitaGames
    {
        public bool manekenGame = false;
        public void spawnManeken()
        {
            GameObject someManeken = GameObject.Instantiate(ManekenTemplate, worldHouse.Find("House"));
            someManeken.SetActive(true);
            activeMakens.Add(someManeken);





            if (manekenGame == false)
            {
                manekenGame = true;
                MelonCoroutines.Start(CheckManekenGame());
            }
            someManeken.transform.SetPositionAndRotation(GetRandomLoc().position, GetRandomLoc().rotation);


        }
        public void TurnAllMenekens(bool on)
        {
            if (activeMakens.Count <= 0) return;

            foreach (GameObject m in activeMakens)
            {
                if (on) m.transform.FindChild("MitaManeken 1").gameObject.GetComponent<Mob_Maneken>().ResetManeken();
                else m.transform.FindChild("MitaManeken 1").gameObject.GetComponent<Mob_Maneken>().DeactivationManeken();

            }
            manekenGame = on;
        }
        public void removeAllMenekens()
        {
            foreach (GameObject m in activeMakens)
            {
                GameObject.Destroy(m);
            }
            activeMakens.Clear();
            manekenGame = false;
        }
    }
}
