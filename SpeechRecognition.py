import time
from io import BytesIO
import asyncio
import soundfile as sf
import numpy as np
import speech_recognition as sr
import sounddevice as sd

# Настройка логирования
import logging
import colorlog

# Настройка цветного логирования
handler = colorlog.StreamHandler()
handler.setFormatter(colorlog.ColoredFormatter(
    '%(log_color)s%(asctime)s - %(levelname)s - %(message)s',
    log_colors={
        'INFO': 'white',
        'WARNING': 'yellow',
        'ERROR': 'red',
        'CRITICAL': 'red,bg_white',
    }
))

logger = colorlog.getLogger(__name__)
logger.setLevel(logging.INFO)
logger.addHandler(handler)


class AudioState:
    def __init__(self):
        self.is_recording = False
        self.audio_buffer = []
        self.last_sound_time = None
        self.is_playing = False
        self.lock = asyncio.Lock()
        self.vc = None
        self.max_buffer_size = 10000

    async def add_to_buffer(self, data):
        async with self.lock:
            if len(self.audio_buffer) >= self.max_buffer_size:
                self.audio_buffer.pop(0)
            self.audio_buffer.append(data.copy())


audio_state = AudioState()


class SpeechRecognition:
    user_input = ""
    microphone_index = 0
    active = True
    audio_state = AudioState()

    SAMPLE_RATE = 48000
    CHUNK_SIZE = 1024
    TIMEOUT_MESSAGE = False
    SILENCE_THRESHOLD = 0.01
    SILENCE_DURATION = 4
    MIN_RECORDING_DURATION = 1

    @staticmethod
    def receive_text():
        user_input_to_send = SpeechRecognition.user_input
        SpeechRecognition.user_input = ""
        return user_input_to_send

    @staticmethod
    def list_microphones():
        return sr.Microphone.list_microphone_names()

    @staticmethod
    async def handle_voice_message(recognized_text: str) -> None:
        logger.info(f"Распознанный текст: {recognized_text}")
        SpeechRecognition.user_input += f" {recognized_text}"

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
                        lambda: recognizer.listen(source)
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
                    logger.info("Речь не распознана")
                except Exception as e:
                    logger.error(f"Ошибка при распознавании: {e}")
                    break

    @staticmethod
    async def async_audio_callback(indata):
        async with SpeechRecognition.audio_state.lock:
            if SpeechRecognition.audio_state.is_playing:
                return

            current_volume = np.max(np.abs(indata))
            if current_volume > SpeechRecognition.SILENCE_THRESHOLD:
                if not SpeechRecognition.audio_state.is_recording:
                    SpeechRecognition.audio_state.is_recording = True
                    logger.info("Начало записи.")
                await SpeechRecognition.audio_state.add_to_buffer(indata)
                SpeechRecognition.audio_state.last_sound_time = time.time()
            elif SpeechRecognition.audio_state.is_recording:
                silence_time = time.time() - SpeechRecognition.audio_state.last_sound_time
                if silence_time > SpeechRecognition.SILENCE_DURATION:
                    SpeechRecognition.audio_state.is_recording = False
                    logger.info("Запись завершена: тишина.")
                    await asyncio.sleep(0.5)
                    await SpeechRecognition.process_audio()

    @staticmethod
    async def process_audio():
        try:
            async with SpeechRecognition.audio_state.lock:
                if len(SpeechRecognition.audio_state.audio_buffer) == 0:
                    logger.warning("Аудиобуфер пуст!")
                    return

                non_empty_chunks = [chunk for chunk in SpeechRecognition.audio_state.audio_buffer if chunk.size > 0]
                if not non_empty_chunks:
                    logger.warning("Нет данных для обработки.")
                    return

                audio_data = np.concatenate(non_empty_chunks)
                SpeechRecognition.audio_state.audio_buffer.clear()

                with BytesIO() as audio_buffer:
                    sf.write(audio_buffer, audio_data, SpeechRecognition.SAMPLE_RATE, format='WAV')
                    audio_buffer.seek(0)
                    recognized_text = await SpeechRecognition.recognize_speech(audio_buffer)

                    if recognized_text:
                        await SpeechRecognition.handle_voice_message(recognized_text)
        except Exception as e:
            logger.error(f"Ошибка обработки аудио: {e}")

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
                await asyncio.sleep(0.25)
            except Exception as e:
                logger.error(f"Ошибка в speach_recognition_start_async_other_system: {e}")

    @staticmethod
    async def speach_recognition_start_async():
        await SpeechRecognition.live_recognition()

    @staticmethod
    def speach_recognition_start(device_id: int, loop):
        SpeechRecognition.microphone_index = device_id
        asyncio.run_coroutine_threadsafe(SpeechRecognition.speach_recognition_start_async(), loop)

    @staticmethod
    def start_audio_monitoring(device_id: int, loop):
        SpeechRecognition.microphone_index = device_id
        asyncio.run_coroutine_threadsafe(SpeechRecognition.speach_recognition_start_async(), loop)

    @staticmethod
    async def audio_monitoring():
        with sd.InputStream(
                callback=SpeechRecognition.async_audio_callback,
                channels=1,
                samplerate=SpeechRecognition.SAMPLE_RATE,
                dtype='float32',
                device=SpeechRecognition.microphone_index
        ):
            logger.info("Начинаем мониторинг аудиопотока...")
            while SpeechRecognition.active:
                await asyncio.sleep(0.1)


async def main():
    speech_recognition = SpeechRecognition()
    await speech_recognition.audio_monitoring()


if __name__ == "__main__":
    asyncio.run(main())
