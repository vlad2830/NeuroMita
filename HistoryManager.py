import json
import os
from datetime import datetime
import shutil


class HistoryManager:
    """ В работе, пока неактивно"""

    def __init__(self, history_file_name=""):

        self.history_file_path = history_file_name

        if self.history_file_path != "":
            self.load_history(self.history_file_path)

    def load_history(self, history_file):
        """Загружаем историю из файла, создаем пустую структуру, если файл пуст или не существует."""
        try:
            with open(history_file, 'r', encoding='utf-8') as f:
                data = json.load(f)
                print("Загрузка истории")

                if self.history_format_correct(data):

                    print("Историю получится заполнить")
                    return data

                else:
                    print("Ошибка загрузки истории")
                    return self._default_history()
        except (json.JSONDecodeError, FileNotFoundError):
            # Если файл пуст или не существует, возвращаем структуру по умолчанию
            print("Ошибка загрузки истории")
            return self._default_history()

    def history_format_correct(self, data):

        if (isinstance(data.get('messages'), list)
                and isinstance(data.get('currentInfo'), dict)
                and isinstance(data.get('MitaSystemMessages'), list)
                and isinstance(data.get('Variables'), list)):
            return True

        else:
            print("Формат истории неправильный")
            return False

    def save_history(self, data):
        """Сохраняем историю в файл с явной кодировкой utf-8."""
        # Убедимся, что структура данных включает 'messages', 'currentInfo' и 'MitaSystemMessages'
        history_data = {
            'messages': data.get('messages', []),
            'currentInfo': data.get('currentInfo', {}),
            'MitaSystemMessages': data.get('MitaSystemMessages', []),
            'Variables': data.get('Variables', []),
        }

        with open(self.history_file_path, 'w', encoding='utf-8') as f:
            json.dump(history_data, f, ensure_ascii=False, indent=4)

    def save_chat_history(self):
        print("save_chat_history")
        # Папка для сохранения историй
        target_folder = "SavedHistories"
        # Проверяем, существует ли папка SavedHistories, и создаём её, если нет
        os.makedirs(target_folder, exist_ok=True)

        # Формируем имя файла с таймингом
        timestamp = datetime.datetime.now().strftime("%d.%m.%Y_%H.%M")
        target_file = f"chat_history_{timestamp}.json"

        # Полный путь к новому файлу
        target_path = os.path.join(target_folder, target_file)

        # Копируем файл
        shutil.copy(self.history_file_path, target_path)
        print(f"Файл сохранён как {target_path}")

    def clear_history(self):
        print("Сброс файла истории")

        self.save_history(self._default_history())

    def _default_history(self):
        print("ИСТОРИЧЕСКИЙ ДЕФОЛТ!!!")
        """Создаем структуру истории по умолчанию."""
        return {
            'messages': [],
            'currentInfo': {},
            'MitaSystemMessages': [],
            'Variables': []
        }
