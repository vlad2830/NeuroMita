import json
import os


class HistoryManager:
    def __init__(self, history_file="chat_history.json"):
        self.history_file = history_file

    def load_history(self):
        try:
            with open(self.history_file, 'r', encoding='utf-8') as f:
                data = json.load(f)
                print("Загрузка истории")
                if (isinstance(data.get('messages'), list) and
                        isinstance(data.get('currentInfo'), dict) and
                        isinstance(data.get('MitaSystemMessages'), list)):
                    print("Историю получится заполнить")
                    return data
                else:
                    print("Ошибка загрузки истории")
                    return self._default_history()
        except (json.JSONDecodeError, FileNotFoundError):
            print("Ошибка загрузки истории")
            return self._default_history()

    def save_history(self, data):
        history_data = {
            'messages': data.get('messages', []),
            'currentInfo': data.get('currentInfo', {}),
            'MitaSystemMessages': data.get('MitaSystemMessages', []),
            'attitude': data.get('attitude', 60),
            'boredom': data.get('boredom', 0),
            'stress': data.get('stress', 0),
            'secretExposed': data.get('secretExposed', False),
            'secretExposedFirst': data.get('secretExposedFirst', False)
        }
        with open(self.history_file, 'w', encoding='utf-8') as f:
            json.dump(history_data, f, ensure_ascii=False, indent=4)

    def _default_history(self):
        print("ИСТОРИЧЕСКИЙ ДЕФОЛТ!!!")
        return {
            'messages': [],
            'currentInfo': {},
            'MitaSystemMessages': [],
            'attitude': 60,
            'boredom': 0,
            'stress': 0,
            'secretExposed': False,
            'secretExposedFirst': False,
        }
