"""

import speech_recognition as sr
import logging


class SpeechRecognition:

    @staticmethod
    def recognize_speech(audio_buffer):
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
    def recognize_speech_from_file(filename):
        recognizer = sr.Recognizer()
        try:
            with sr.AudioFile(filename) as source:
                audio = recognizer.record(source)
            text = recognizer.recognize_google(audio,
                                               language="ru-RU")  # Распознаём текст короче через google web speech api, потом может сменю
            print(Fore.Magenta + "Распознанный текст:", text)
            return text
        except sr.UnknownValueError:
            print(Fore.Magenta + "Google Web Speech API не смог распознать аудио.")
            return None
        except sr.RequestError as e:
            print(Fore.Magenta + f"Ошибка при запросе к Google Web Speech API: {e}")
            return None
    @staticmethod
    async def handle_voice_message(recognized_text):
        ...

    @staticmethod
    async def process_audio():
        try:
            async with audio_state.lock:
                if len(audio_state.audio_buffer) == 0:
                    logging.warning("Аудиобуфер пуст!")
                    return
                # Проверка данных перед конкатенацией
                non_empty_chunks = [chunk for chunk in audio_state.audio_buffer if chunk.size > 0]
                if not non_empty_chunks:
                    logging.warning("Нет данных для обработки.")
                    return
                audio_data = np.concatenate(non_empty_chunks)
                audio_state.audio_buffer.clear()

                with BytesIO() as audio_buffer:
                    sf.write(audio_buffer, audio_data, SAMPLE_RATE, format='WAV')
                    audio_buffer.seek(0)
                    recognized_text = recognize_speech(audio_buffer)

                    if recognized_text:
                        await SpeechRecognition.handle_voice_message(recognized_text)
        except Exception as e:
            logging.error(f"Ошибка обработки аудио: {e}")


"""