### main.py

import os
import ssl
import certifi


from gui import ChatGUI


# Установка

# Теперь делаю файлом с папкой, так как антивирусы ругаются)
#pyinstaller --name NeuroMita --add-data "Prompts/*;Prompts" --add-data "Prompts/**/*;Prompts" Main.py

# Старый вариант
#pyinstaller --onefile --name NeuroMita --add-data "Prompts/*;Prompts" --add-data "Prompts/**/*;Prompts" Main.py

# Не забудь рядом папку промптов и ffmpeg

# Устанавливаем глобальные настройки SSL в самом начале приложения

def main():
    # Настройка SSL сертификатов для aiohttp
    os.environ['SSL_CERT_FILE'] = certifi.where()
    os.environ['REQUESTS_CA_BUNDLE'] = certifi.where()
    os.environ['CURL_CA_BUNDLE'] = certifi.where()
    
    # Отключение проверки SSL для aiohttp
    ssl_context = ssl.create_default_context()
    ssl_context.check_hostname = False
    ssl_context.verify_mode = ssl.CERT_NONE
    
    gui = ChatGUI()
    gui.run()


if __name__ == "__main__":
    main()
