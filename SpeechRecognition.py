import time
from io import BytesIO
import asyncio
import logging
import soundfile as sf
import numpy as np
import speech_recognition as sr
import sounddevice as sd
from collections import deque
from threading import Lock

logging.basicConfig()
logger = logging.getLogger(__name__)


class AudioState:
    def __init__(self):
        self.is_recording = False
        self.audio_buffer = []
        self.last_sound_time = time.time()
        self.is_playing = False
        self.lock = asyncio.Lock()
        self.vc = None
        self.max_buffer_size = 9999999

    async def add_to_buffer(self, data):
        async with self.lock:
            if len(self.audio_buffer) >= self.max_buffer_size:
                self.audio_buffer = self.audio_buffer[-self.max_buffer_size // 2:]  # Сохраняем последние 50%
            self.audio_buffer.append(data.copy())


audio_state = AudioState()


class SpeechRecognition:
    user_input = ""
    microphone_index = 0
    active = True

    SAMPLE_RATE = 44000
    CHUNK_SIZE = 512
    TIMEOUT_MESSAGE = True
    SILENCE_THRESHOLD = 0.02  # Порог тишины
    SILENCE_DURATION = 4  # Длительность тишины для завершения записи
    SILENCE_TIMEOUT = 2.0
    MIN_RECORDING = 1.0
    MIN_RECORDING_DURATION = 1  # Минимальная длительность записи
    BUFFER_TIMEOUT = 0.05
    _text_lock = Lock()
    _text_buffer = deque(maxlen=10)  # Храним последние 10 фраз
    _current_text = ""
    _last_delimiter = ". "

    @staticmethod
    def receive_text() -> str:
        """Получение и сброс текста (синхронный метод)"""
        with SpeechRecognition._text_lock:
            result = " ".join(SpeechRecognition._text_buffer).strip()
            SpeechRecognition._text_buffer.clear()
            SpeechRecognition._current_text = ""
            #logger.debug(f"Returned text: {result}")
            return result

    @staticmethod
    def list_microphones():
        return sr.Microphone.list_microphone_names()

    @staticmethod
    async def handle_voice_message(recognized_text: str) -> None:
        """Асинхронная обработка текста"""
        text_clean = recognized_text.strip()
        if text_clean:
            with SpeechRecognition._text_lock:
                # Определение разделителя
                last_char = SpeechRecognition._current_text[-1] if SpeechRecognition._current_text else ""
                delimiter = "" if last_char in {'.', '!', '?', ','} else " "

                SpeechRecognition._text_buffer.append(text_clean)
                SpeechRecognition._current_text += f"{delimiter}{text_clean}"

    @staticmethod
    async def live_recognition() -> None:
        recognizer = sr.Recognizer()

        with sr.Microphone(device_index=SpeechRecognition.microphone_index, sample_rate=SpeechRecognition.SAMPLE_RATE,
                           chunk_size=SpeechRecognition.CHUNK_SIZE) as source:
            logger.info(
                f"Используется микрофон: {sr.Microphone.list_microphone_names()[SpeechRecognition.microphone_index]}")
            recognizer.adjust_for_ambient_noise(source)
            logger.info("Скажите что-нибудь...")

            while SpeechRecognition.active:
                try:
                    audio = await asyncio.get_event_loop().run_in_executor(
                        None,
                        lambda: recognizer.listen(source, timeout=5)  # Увеличим таймаут
                    )

                    text = await asyncio.get_event_loop().run_in_executor(
                        None,
                        lambda: recognizer.recognize_google(audio, language="ru-RU")
                    )
                    if not text:
                        text = await asyncio.get_event_loop().run_in_executor(
                            None,
                            lambda: recognizer.recognize_google(audio, language="en-EN")
                        )

                    if text:
                        await SpeechRecognition.handle_voice_message(text)

                except sr.WaitTimeoutError:
                    if SpeechRecognition.TIMEOUT_MESSAGE:
                        logger.info("Таймаут ожидания речи...")
                except sr.UnknownValueError:
                    ...
                    #logger.info("Речь не распознана")
                except Exception as e:
                    logger.error(f"Ошибка при распознавании: {e}")
                    break

    @staticmethod
    async def async_audio_callback(indata):
        try:
            current_time = time.time()
            rms = np.sqrt(np.mean(indata ** 2))

            async with audio_state.lock:
                if rms > SpeechRecognition.SILENCE_THRESHOLD:
                    audio_state.last_sound_time = current_time
                    if not audio_state.is_recording:
                        logger.debug("🟢 Начало записи")
                        audio_state.is_recording = True
                    await audio_state.add_to_buffer(indata)

                elif audio_state.is_recording:
                    silence_duration = 4
                    audio_state.is_recording = False
                    await SpeechRecognition.process_audio()
                else:
                    logger.debug("❌ Слишком короткая запись, сброс")
                    audio_state.audio_buffer.clear()
                    audio_state.is_recording = False

        except Exception as e:
            logger.error(f"Ошибка в колбэке: {str(e)}")

    @staticmethod
    async def process_audio():
        try:
            async with audio_state.lock:
                if not audio_state.audio_buffer:
                    return

                audio_data = np.concatenate(audio_state.audio_buffer)
                audio_state.audio_buffer.clear()

                with BytesIO() as buffer:
                    sf.write(buffer, audio_data, SpeechRecognition.SAMPLE_RATE, format='WAV')
                    buffer.seek(0)

                    try:
                        recognizer = sr.Recognizer()
                        with sr.AudioFile(buffer) as source:
                            audio = recognizer.record(source)
                            text = recognizer.recognize_google(audio, language="ru-RU")
                            logger.info(f"Распознано: {text}")
                            await SpeechRecognition.handle_voice_message(text)  # Исправленный вызов
                    except sr.UnknownValueError:
                        ...
                        #logger.warning("Речь не распознана")
                    except Exception as e:
                        logger.error(f"Ошибка распознавания: {str(e)}")
        except Exception as e:
            logger.error(f"Ошибка обработки: {str(e)}")

    @staticmethod
    async def recognize_speech(audio_buffer):
        recognizer = sr.Recognizer()
        try:
            with sr.AudioFile(audio_buffer) as source:
                audio = recognizer.record(source)

            text = recognizer.recognize_google(audio, language="ru-RU")
            if not text:
                text = recognizer.recognize_google(audio, language="en-EN")
            return text
        except sr.UnknownValueError:
            logger.error("Не удалось распознать речь")
            return None
        except sr.RequestError as e:
            logger.error(f"Ошибка API: {e}")
            return None

    @staticmethod
    async def speach_recognition_start_async_other_system():
        while SpeechRecognition.active:
            try:
                await SpeechRecognition.async_audio_callback(0)
                await asyncio.sleep(0.1)  # Уменьшим интервал
            except Exception as e:
                logger.error(f"Ошибка в speach_recognition_start_async_other_system: {e}")

    @staticmethod
    async def speach_recognition_start_async():
        await SpeechRecognition.live_recognition()

    @staticmethod
    def speach_recognition_start(device_id: int, loop):
        SpeechRecognition.microphone_index = device_id
        asyncio.run_coroutine_threadsafe(SpeechRecognition.speach_recognition_start_async(), loop)

    # @staticmethod
    # def start_audio_monitoring(device_id: int, loop):
    #  SpeechRecognition.microphone_index = device_id
    # asyncio.run_coroutine_threadsafe(SpeechRecognition.speach_recognition_start_async(), loop)

    @staticmethod
    async def audio_monitoring():
        try:
            logger.info("🚀 Запуск аудиомониторинга")
            with sd.InputStream(
                    callback=lambda indata, *_: asyncio.create_task(SpeechRecognition.async_audio_callback(indata)),
                    channels=1,
                    samplerate=SpeechRecognition.SAMPLE_RATE,
                    blocksize=SpeechRecognition.CHUNK_SIZE,
                    device=SpeechRecognition.microphone_index
            ):
                while SpeechRecognition.active:
                    await asyncio.sleep(0.1)
        except Exception as e:
            logger.critical(f"Критическая ошибка: {str(e)}")

    @staticmethod
    async def get_current_text() -> str:
        async with SpeechRecognition._text_lock:
            return SpeechRecognition._current_text.strip()


async def main():
    speech_recognition = SpeechRecognition()


if __name__ == "__main__":
    speech_recognition = SpeechRecognition()
    asyncio.run(SpeechRecognition.audio_monitoring())
    asyncio.run(main())
