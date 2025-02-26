import speech_recognition as sr
import logging

import asyncio

SAMPLE_RATE = 16000
CHUNK_SIZE = 1024
TIMEOUT_MESSAGE = False


class SpeechRecognition:
    user_input = ""

    @staticmethod
    def receive_text():
        user_input_to_send = SpeechRecognition.user_input
        SpeechRecognition.user_input = ""
        return user_input_to_send

    @staticmethod
    def list_microphones():
        """Возвращает список доступных микрофонов"""
        return sr.Microphone.list_microphone_names()

    @staticmethod
    async def handle_voice_message(recognized_text: str) -> None:
        print(f"Распознанный текст: {recognized_text}")
        SpeechRecognition.user_input += f" {recognized_text}"

    @staticmethod
    async def live_recognition(microphone_index: int) -> None:
        """Потоковое распознавание с выбранного микрофона"""
        recognizer = sr.Recognizer()

        # Используем выбранный микрофон
        with sr.Microphone(device_index=microphone_index) as source:
            print(f"Используется микрофон: {sr.Microphone.list_microphone_names()[microphone_index]}")
            recognizer.adjust_for_ambient_noise(source)
            print("Скажите что-нибудь...")

            while True:
                try:
                    audio = await asyncio.get_event_loop().run_in_executor(
                        None,
                        lambda: recognizer.listen(source)
                    )

                    text = await asyncio.get_event_loop().run_in_executor(
                        None,
                        lambda: recognizer.recognize_google(audio, language="ru-RU")
                    )

                    if text:
                        await SpeechRecognition.handle_voice_message(text)

                except sr.WaitTimeoutError:
                    if TIMEOUT_MESSAGE:
                        print("Таймаут ожидания речи...")
                except sr.UnknownValueError:
                    print("Речь не распознана")
                except Exception as e:
                    logging.error(f"Ошибка при распознавании: {e}")
                    break

    @staticmethod
    async def speach_recognition_start_async(device_id: int):
        """Асинхронный запуск распознавания с указанным микрофоном"""
        await SpeechRecognition.live_recognition(device_id)

    @staticmethod
    def speach_recognition_start(device_id: int, loop):
        """Синхронный запуск распознавания с указанным микрофоном"""
        asyncio.run_coroutine_threadsafe(SpeechRecognition.speach_recognition_start_async(device_id), loop)
