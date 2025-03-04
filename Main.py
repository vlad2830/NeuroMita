### main.py

from gui import ChatGUI


# Установка

# Теперь делаю файлом с папкой, так как антивирусы ругаются)
#pyinstaller --name NeuroMita --add-data "Prompts/*;Prompts" --add-data "Prompts/**/*;Prompts" Main.py

# Старый вариант
#pyinstaller --onefile --name NeuroMita --add-data "Prompts/*;Prompts" --add-data "Prompts/**/*;Prompts" Main.py

# Не забудь рядом папку промптов и ffmpeg

# Устанавливаем глобальные настройки SSL в самом начале приложения

def main():
    gui = ChatGUI()
    gui.run()


if __name__ == "__main__":
    main()
