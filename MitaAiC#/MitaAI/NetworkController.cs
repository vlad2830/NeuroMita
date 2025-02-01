using static MelonLoader.MelonLogger;
using System.Net.Sockets;
using System.Text;
using MelonLoader;
using UnityEngine;
namespace MitaAI
{
    public static class NetworkController
    {
        static MitaCore mitaCore;
        private const string ServerAddress = "127.0.0.1";
        private const int Port = 12345;

        static public void Initialize()
        {
            mitaCore = MitaCore.Instance;
        }

        static public async Task<(string, string)> GetResponseFromPythonSocketAsync(string input, string dataToSentSystem, string systemInfo)
        {
            // Ожидаем, чтобы получить доступ к ресурсу (сокету)

            using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                bool connected = await TryConnectAsync(clientSocket, ServerAddress, Port);
                if (!connected)
                {
                    return (string.Empty, string.Empty); // Возвращаем пустой ответ, если не удалось подключиться
                }

                bool waitResponse = input != "waiting" || dataToSentSystem != "-";
                // Дополнительная логика для подготовки данных



                string _currentInfo = waitResponse ? mitaCore.currentInfo : "-";
                if (string.IsNullOrEmpty(mitaCore.currentInfo)) _currentInfo = "-";
                if (string.IsNullOrEmpty(systemInfo)) systemInfo = "-";

                string total_input = $"{input}|||{dataToSentSystem}|||{systemInfo}|||{mitaCore.distance.ToString("F2")}|||{mitaCore.roomIDPlayer}|||{mitaCore.roomIDMita}|||{mitaCore.hierarchy}|||{_currentInfo}";

                byte[] messageBytes = Encoding.UTF8.GetBytes(total_input);
                await clientSocket.SendAsync(messageBytes, SocketFlags.None);

                byte[] buffer = new byte[4086];
                try
                {
                    int bytesRead = await clientSocket.ReceiveAsync(buffer, SocketFlags.None);

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    string[] parts = receivedMessage.Split(new string[] { "|||" }, StringSplitOptions.None);

                    // Логируем ответ
                    string response = parts[0];
                    string patch = "";
                    //MelonLogger.Msg("Reveiced data" + parts[0] + "" + parts[2]);
                    //waitForSounds = parts[1];

                    if (!string.IsNullOrEmpty(parts[2])) patch = parts[2];
                    //patch_to_sound_file = parts[1];
                    return (response, patch);
                }
                catch (Exception)
                {
                    //LoggerInstance.Msg($"Error receiving data: {ex.Message}");
                    return (string.Empty, string.Empty); // Возвращаем пустой ответ в случае ошибки при получении данных
                }
            }

        }
        static private async Task<bool> TryConnectAsync(Socket socket, string address, int port, int maxRetries = 3, int delayBetweenRetries = 2000)
        {
            int attempt = 0;
            while (attempt < maxRetries)
            {
                try
                {
                    await Task.Factory.FromAsync(socket.BeginConnect(address, port, null, null), socket.EndConnect);
                    return true; // Если подключение прошло успешно
                }
                catch (SocketException ex)
                {
                    attempt++;
                    if (attempt >= maxRetries)
                    {
                        //LoggerInstance.Msg($"Max retries reached. Unable to connect to {address}:{port}.{ex}");
                        return false; // После превышения количества попыток
                    }
                    await Task.Delay(delayBetweenRetries); // Задержка перед повторной попыткой
                }
                catch (Exception ex)
                {
                    //LoggerInstance.Msg($"Unexpected error connecting to {address}:{port}: {ex.Message}");
                    return false; // Если произошла неожиданная ошибка
                }
            }
            return false; // В случае завершения цикла
        }
        public static async Task<AudioClip> LoadAudioClipFromFileAsync(string filePath)
        {
            try
            {
                MelonLoader.MelonLogger.Msg("Loading audio file: " + filePath);

                // Читаем все байты файла
                byte[] fileData = File.ReadAllBytes(filePath);

                // Конвертируем байты в массив float
                float[] audioData = ConvertByteArrayToFloatArray(fileData);

                // Применяем нормализацию
                audioData = NormalizeAudioData(audioData);

                // Применяем fade-in (0.1 секунды на 44100 Hz)
                audioData = ApplyFadeIn(audioData, 44100 / 10);

                // Создаем аудиоклип
                AudioClip audioClip = AudioClip.Create("LoadedAudio", audioData.Length, 2, 44100, false);
                audioClip.SetData(audioData, 0);

                MelonLoader.MelonLogger.Msg("Audio returned");

                // Удаление исходного файла
                try
                {
                    await Task.Delay(100);
                    File.Delete(filePath);
                    MelonLoader.MelonLogger.Msg("Original file deleted: " + filePath);
                }
                catch (Exception ex)
                {
                    MelonLoader.MelonLogger.Error("Error deleting original file: " + ex.Message);
                }

                return audioClip;
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error("Error loading audio: " + ex.Message);
                return null;
            }
        }


        // Нормализация аудиоданных
        private static float[] NormalizeAudioData(float[] audioData)
        {
            float maxAmplitude = 0f;
            foreach (var sample in audioData)
            {
                if (Math.Abs(sample) > maxAmplitude)
                {
                    maxAmplitude = Math.Abs(sample);
                }
            }
            if (maxAmplitude > 0)
            {
                for (int i = 0; i < audioData.Length; i++)
                {
                    audioData[i] /= maxAmplitude;
                }
            }
            return audioData;
        }

        // Применение fade-in
        private static float[] ApplyFadeIn(float[] audioData, int fadeSamples)
        {
            for (int i = 0; i < fadeSamples && i < audioData.Length; i++)
            {
                float fadeFactor = (float)i / fadeSamples;
                audioData[i] *= fadeFactor;
            }
            return audioData;
        }

        // Преобразование байтов в массив float
        private static float[] ConvertByteArrayToFloatArray(byte[] byteArray)
        {
            float[] floatArray = new float[byteArray.Length / 2]; // предполагаем, что файл в 16-битах
            for (int i = 0; i < floatArray.Length; i++)
            {
                short sample = BitConverter.ToInt16(byteArray, i * 2);
                floatArray[i] = sample / 32768f; // Нормализация в диапазон [-1, 1]
            }
            return floatArray;
        }
    }
}