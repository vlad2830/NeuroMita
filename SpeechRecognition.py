import speech_recognition as sr
import logging
import numpy as np
import soundfile as sf
from io import BytesIO
from typing import Optional
import asyncio


class AudioState:
    def __init__(self):
        self.audio_buffer = []
        self.lock = asyncio.Lock()


SAMPLE_RATE = 16000  # Частота дискретизации для аудио
CHUNK_SIZE = 1024  # Размер чанка для чтения с микрофона
TIMEOUT_MESSAGE = False


class SpeechRecognition:

    @staticmethod
    def recognize_speech(audio_buffer: BytesIO) -> Optional[str]:
        """Распознавание речи из аудио буфера"""
        recognizer = sr.Recognizer()
        try:
            with sr.AudioFile(audio_buffer) as source:
                audio = recognizer.record(source)
            return recognizer.recognize_google(audio, language="ru-RU")
        except sr.UnknownValueError:
            logging.error("Не удалось распознать речь")
            return None
        except sr.RequestError as e:
            logging.error(f"Ошибка API: {e}")
            return None

    @staticmethod
    async def handle_voice_message(recognized_text: str) -> None:
        """Обработка распознанного текста"""
        print(f"Распознанный текст: {recognized_text}")
        # Здесь можно добавить логику обработки текста

    @staticmethod
    async def process_audio(audio_state) -> None:
        """Асинхронная обработка аудио из буфера"""
        try:
            async with audio_state.lock:
                if not audio_state.audio_buffer:
                    logging.warning("Аудиобуфер пуст!")
                    return

                non_empty_chunks = [chunk for chunk in audio_state.audio_buffer if chunk.size > 0]
                if not non_empty_chunks:
                    logging.warning("Нет данных для обработки")
                    return

                audio_data = np.concatenate(non_empty_chunks)
                audio_state.audio_buffer.clear()

                with BytesIO() as audio_buffer:
                    sf.write(audio_buffer, audio_data, SAMPLE_RATE, format='WAV')
                    audio_buffer.seek(0)
                    recognized_text = SpeechRecognition.recognize_speech(audio_buffer)

                    if recognized_text:
                        await SpeechRecognition.handle_voice_message(recognized_text)
        except Exception as e:
            logging.error(f"Ошибка обработки аудио: {e}")

    @staticmethod
    async def live_recognition(audio_state) -> None:
        """Потоковое распознавание речи с микрофона в реальном времени"""
        recognizer = sr.Recognizer()
        with sr.Microphone() as source:
            recognizer.adjust_for_ambient_noise(source)
            print("Скажите что-нибудь...")

            while True:
                try:
                    # Асинхронно получаем аудио с микрофона
                    audio = await asyncio.get_event_loop().run_in_executor(
                        None,
                        lambda: recognizer.listen(source, timeout=3, phrase_time_limit=5)
                    )

                    # Распознавание через Google Web Speech API
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
    async def speach_recognition_start(selected_microphone):
        audio_state = AudioState()
        # Запуск потокового распознавания
        await SpeechRecognition.live_recognition(audio_state)

