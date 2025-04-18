
using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace MitaAI
{
    [RegisterTypeInIl2Cpp]
    public class CommonInteractableObject : MonoBehaviour
    {
        /*
         *  Компонента для отслеживания, занято место кем-то или нет 
         *  А также логике на выходе из интеракции
         */

        public Dictionary<string,characterType> taker = new Dictionary<string, characterType> { {"center",characterType.None } };        
        public UnityEvent eventEnd = new UnityEvent();


        static List<(CommonInteractableObject,string)> lastPlayerCIAs = new List<(CommonInteractableObject, string)>();

        public static void addCIA(CommonInteractableObject CIA,string position = "center")
        {
            MelonLogger.Msg($"AddCia { CIA.gameObject.name}");
            lastPlayerCIAs.Add( (CIA,position) );
        }

        public static void endLastPlayersCIAs()
        {
            foreach (var item in lastPlayerCIAs)
            {
                MelonLogger.Msg($"freee {item.Item1.gameObject.name}");
                item.Item1.free(item.Item2);
            }
            lastPlayerCIAs.Clear();
        }

        public static CommonInteractableObject CheckCreate(GameObject gameObject,string position="center",UnityAction freeCase = null)
        {
            var CIO = gameObject.GetComponent<CommonInteractableObject>();
            if (CIO  == null)
            {
                CIO = gameObject.AddComponent<CommonInteractableObject>();
                
            }
            CIO.setTaken(characterType.None, position);
            
            if (freeCase != null)
            {
                CIO.eventEnd.AddListener(freeCase);
            }

            return CIO;

        }

        public bool isTaken(string position = "center")
        {

            if (taker.ContainsKey(position)) { 
                taker[position] = characterType.None; 
            }

            return taker[position] != characterType.None;


        }
        public void setTakenPlayer()
        {
            MelonLogger.Msg($"Set Taken CIA {gameObject.name}");

            taker["center"] = characterType.Player;

            endLastPlayersCIAs();
            addCIA(this);

            var obj = GetComponent<ObjectInteractive>();
            if (obj != null)
            {
                obj.active = false;

            }



        }

        public void setTaken(characterType character = characterType.Player,string position = "center")
        {
            MelonLogger.Msg($"Set Taken CIA {gameObject.name}");

            taker[position] = character;

            if (character == characterType.Player)
            {
                endLastPlayersCIAs();
                addCIA(this);
            }

            var obj = GetComponent<ObjectInteractive>();
            if (obj != null)
            {
                obj.active = false;
            }


        }


        public void free(string position = "center")
        {
            MelonLogger.Msg($"Free CIO {this.gameObject.name}");

            taker[position] = characterType.None;

            var obj = GetComponent<ObjectInteractive>();
            if (obj != null)
            {
                obj.active = true;
            }

            if (eventEnd == null) return;

            try
            { 
                eventEnd.Invoke();
            }
            catch (Exception ex)
            { 
                MelonLogger.Error(ex);
            }
            
        }

    }
}
