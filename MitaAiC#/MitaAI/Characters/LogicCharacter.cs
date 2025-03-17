using MelonLoader;
using Il2Cpp;
using UnityEngine;
using UnityEngine.AI;

namespace MitaAI
{
    public class LogicCharacter
    {
        private const float DEFAULT_CHECK_INTERVAL = 2.0f;
        private const float UNSAFE_VERTICAL_SPEED = -10f;
        private const float UNSAFE_Y_POSITION = -100f;
        private const float NAV_MESH_SAMPLE_DISTANCE = 100f;

        private static LogicCharacter instance;
        public static LogicCharacter Instance => instance ??= new LogicCharacter();

        private bool isInitialized;
        private GameObject characterObject;
        private float lastCheckTime;
        private NavMeshAgent navMeshAgent;
        private Animator animator;
        private MitaCore.character characterType; // Тип персонажа (Creepy, Sleepy и т.д.)

        private LogicCharacter() { } //пока-что нечего

        public void Initialize(GameObject character, MitaCore.character type)
        {
            if (isInitialized) return;
            SetCharacterObject(character);
            characterType = type;
            AdjustCharacterSettings(); // Настройка параметров в зависимости от типа
            isInitialized = true;
        }

        private void SetCharacterObject(UnityEngine.GameObject character)
        {
            characterObject = character;
            if (characterObject != null)
            {
                navMeshAgent = characterObject.GetComponent<NavMeshAgent>();
                animator = characterObject.GetComponent<Animator>();
            }
        }

        private void AdjustCharacterSettings()
        {
            if (navMeshAgent == null) return;
            switch (characterType)
            {
                case MitaCore.character.Creepy:
                    // Сюда будет прописывать логика в зависимости от персонажа
                    break;
                case MitaCore.character.Sleepy:
                    // ...
                    break; //позже для других персонажей
                default:
                    // ...
                    break;
            }
        }

        public void Update()
        {
            if (!isInitialized) return;

            float currentTime = Time.time;
            if (currentTime - lastCheckTime >= DEFAULT_CHECK_INTERVAL)
            {
                lastCheckTime = currentTime;
                MonitorCharacterState();
            }
        }

        // Остановка движения например для логики с сонной
        //public void StopMovement()
        //{
        //    if (navMeshAgent != null && navMeshAgent.enabled)
        //    {
        //        navMeshAgent.ResetPath();
        //    }
        //}

        // Установка скорости
        //public void SetSpeed(float speed)
        //{
        //    moveSpeed = speed;
        //    if (navMeshAgent != null) navMeshAgent.speed = speed;
        //}

        private void MonitorCharacterState() //пусть будет возможно на будующее
        {
            if (characterObject == null) return;
            CheckAnimationState();
        }

        private void CheckAnimationState() //пример как можно было бы сделать с анимациями
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;

            var currentClipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (currentClipInfo.Length == 0) return;

            string currentAnim = currentClipInfo[0].clip.name;
            if (currentAnim.Contains("Fall") || currentAnim.Contains("Idle"))
            {
                // Падение обнаружено
            }
        }

        //private void CheckCharacterPosition() //тож пример с использованием позиции
        //{
        //    Vector3 position = characterObject.transform.position;
        //    if (position.y < UNSAFE_Y_POSITION || float.IsInfinity(position.y) || float.IsNaN(position.y))
        //    {
        //        ResetCharacterPosition();
        //    }
        //}

    }
}