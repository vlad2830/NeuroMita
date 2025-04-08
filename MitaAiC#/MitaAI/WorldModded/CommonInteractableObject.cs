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
        /*
         *  Компонента для отслеживания, занято место кем-то или нет 
         * 
         */


        public characterType taker = characterType.None;        

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

            return taker != characterType.None;


        }
        public void setTaken(characterType character = characterType.Player)
        {
            taker = character;
        }
        public void free()
        {
            taker = characterType.None;
        }

    }
}
