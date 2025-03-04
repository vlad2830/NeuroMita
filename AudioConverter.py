import subprocess
import os
import ffmpeg
import sys

class AudioConverter:
    # Проверяем, где лежит ffmpeg
    ffmpeg_rel_path = os.path.join("ffmpeg-7.1-essentials_build", "bin", "ffmpeg.exe")

    if getattr(sys, 'frozen', False):
        # Если программа собрана в exe, получаем путь к исполняемому файлу
        base_dir = os.path.dirname(sys.executable)

        # Альтернативный вариант: если ffmpeg всё же упакован в _MEIPASS
        alt_base_dir = sys._MEIPASS
    else:
        # Если программа запускается как скрипт
        base_dir = os.path.dirname(__file__)
        alt_base_dir = base_dir  # Для единообразия
    ffmpeg_path = os.path.join(base_dir, ffmpeg_rel_path)
    #ffmpeg_path = "ffmpeg-7.1-essentials_build/bin/ffmpeg.exe"

    @staticmethod
    async def convert_to_wav(input_file, output_file):
        print(f"Начинаю конвертацию {input_file} в {output_file} с помощью {AudioConverter.ffmpeg_path}")

        try:
            command = [
                AudioConverter.ffmpeg_path,
                '-i', input_file,
                '-f', 'wav',
                '-acodec', 'pcm_s16le',
                '-ar', '44100', # Стандартная частота дискретизации
                '-ac', '2', # Стерео
                '-q:a', '0', # Высокое качество
                '-threads', '4', # Используем многопоточность
                '-preset', 'ultrafast', # Самый быстрый пресет
                output_file,
                '-y'
            ]
            subprocess.run(command, check=True, capture_output=True)
            return True
        except subprocess.CalledProcessError as e:
            print(f"Ошибка при конвертации аудио: {e}")
            return False

    @staticmethod
    async def convert_mp3_to_wav(input_path, output_path):
        """Конвертирует MP3 в WAV с использованием ffmpeg."""
        try:
            if not os.path.exists(input_path):
                print(f"Файл {input_path} не найден при попытке конвертации.")
                return

            # Указываем путь к ffmpeg

            print(f"Начинаю конвертацию {input_path} в {output_path} с помощью {AudioConverter.ffmpeg_path}")

            # Выполняем команду конвертации с нужными параметрами
            (
                ffmpeg
                .input(input_path)
                .output(
                    output_path,
                    format="wav",  # Указываем формат WAV
                    acodec="pcm_s16le",  # 16-битный PCM
                    ar="44100",  # Частота дискретизации 44100 Hz
                    ac=2  # Количество каналов (2 = стерео, 1 = моно)
                )
                .run(cmd=AudioConverter.ffmpeg_path)
            )

            print(f"Конвертация завершена: {output_path}")
        except Exception as e:
            print(f"Ошибка при конвертации: {e}")
