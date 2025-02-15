using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MitaAI.WorldModded
{
    using System.Collections;
    using System.IO;
    using UnityEngine;
    using Vosk;

    public class VoiceInput : MonoBehaviour
    {
        private VoskRecognizer recognizer;
        private AudioClip audioClip;
        private bool isRecording = false;

        void Start()
        {
            // Инициализация Vosk с моделью
            string modelPath = Path.Combine(Application.streamingAssetsPath, "model");
            Vosk.Vosk.SetLogLevel(0);
            Vosk.Vosk.Model model = new Vosk.Vosk.Model(modelPath);
            recognizer = new VoskRecognizer(model, 16000.0f);

            // Начните запись
            StartRecording();
        }

        void StartRecording()
        {
            audioClip = Microphone.Start(null, true, 10, 16000);
            isRecording = true;
        }

        void Update()
        {
            if (isRecording)
            {
                // Получение аудиоданных с микрофона
                int pos = Microphone.GetPosition(null);
                if (pos > 0)
                {
                    float[] samples = new float[pos * audioClip.channels];
                    audioClip.GetData(samples, 0);

                    // Преобразование в байты
                    byte[] byteData = new byte[samples.Length * 2];
                    for (int i = 0; i < samples.Length; i++)
                    {
                        short sample = (short)(samples[i] * short.MaxValue);
                        byteData[i * 2] = (byte)(sample & 0xff);
                        byteData[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
                    }

                    // Распознавание речи
                    if (recognizer.AcceptWaveform(byteData, byteData.Length))
                    {
                        string result = recognizer.Result();
                        Debug.Log("Recognized: " + result);
                    }
                }
            }
        }

        void OnDestroy()
        {
            Microphone.End(null);
        }
    }
}
