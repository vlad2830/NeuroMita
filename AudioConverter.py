import subprocess


class AudioConverter:
    
    ffmpeg_path = "ffmpeg-7.1-essentials_build/bin/ffmpeg.exe"

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
