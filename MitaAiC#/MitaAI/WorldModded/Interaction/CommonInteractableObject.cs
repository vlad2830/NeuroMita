
using Il2Cpp;
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

        public Dictionary<string,characterType> taker = new Dictionary<string, characterType> { {"center",characterType.None } };        

        public static CommonInteractableObject CheckCreate(GameObject gameObject,string position="center")
        {
            var CIO = gameObject.GetComponent<CommonInteractableObject>();
            if (CIO  == null)
            {
                CIO = gameObject.AddComponent<CommonInteractableObject>();
                
            }
            CIO.setTaken(characterType.None, position);
            
            return CIO;

        }

        public bool isTaken(string position = "center")
        {

            if (taker.ContainsKey(position)) { 
                taker[position] = characterType.None; 
            }

            return taker[position] != characterType.None;


        }
        public void setTaken(characterType character = characterType.Player,string position = "center")
        {
            taker[position] = character;

            var obj = GetComponent<ObjectInteractive>();
            if (obj != null)
            {
                obj.active = false;
            }
        }
        public void free(string position = "center")
        {
            taker[position] = characterType.None;

            var obj = GetComponent<ObjectInteractive>();
            if (obj != null)
            {
                obj.active = true;
            }
        }

    }
}
