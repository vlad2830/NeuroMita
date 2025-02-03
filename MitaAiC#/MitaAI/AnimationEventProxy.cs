using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Il2Cpp;

namespace MitaAI
{

    [RegisterTypeInIl2Cpp]
    public class AnimationEventProxy : MonoBehaviour
    {

        // Функция, которая будет вызываться через AnimationEvent
        public void OnAnimationEvent(UnityEngine.AnimationEvent evt)
        {
            MelonLogger.Msg("Proxi worked");
            MitaCore.HandleAnimationEvent(evt);
        }

        private UnityEvent _unityEvent;
        private string _eventName;

        // Настройка события
        public void SetupEvent(UnityEvent unityEvent, string eventName)
        {
            _unityEvent = unityEvent;
            _eventName = eventName;

            // Создаем делегат UnityAction и подписываемся на событие
            _unityEvent.AddListener((UnityAction)OnEventTriggered);
        }

        // Метод, который будет вызван при срабатывании UnityEvent
        private void OnEventTriggered()
        {
            MelonLogger.Msg($"UnityEvent triggered! Event Name: {_eventName}");

            // Здесь можно вызвать функции основного класса
            MitaCore.Instance.HandleCustomEvent(_eventName);
        }
    }

}
