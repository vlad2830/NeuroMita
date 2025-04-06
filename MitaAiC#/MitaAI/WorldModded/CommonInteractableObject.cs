using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MitaAI
{
    [RegisterTypeInIl2Cpp]
    public class CommonInteractableObject : MonoBehaviour
    {
        bool isTakenByMita = false;
        bool isTakenByPlayer = false;


        public static CommonInteractableObject CheckCreate(GameObject gameObject)
        {
            if (gameObject.GetComponent<CommonInteractableObject>() == null)
            {

                return gameObject.AddComponent<CommonInteractableObject>();
            }

            return null;
        }

        public bool isTaken()
        {

            return isTakenByMita || isTakenByPlayer;
            
        }
        public void setTaken(bool byMita = true)
        {
            if (byMita) isTakenByMita = true;
            else isTakenByPlayer = true;
        }
        public void free()
        {
            isTakenByMita = false;
            isTakenByPlayer = false;
        }

    }
}
