from abc import ABC, abstractmethod

from Events.MitaEvents import MitaEvents
from Events.PlayerEvents import PlayerEvents


# from promptPart import PromptPart


class BaseState(ABC):
    """Абстрактный класс для состояний"""

    def __init__(self, prompts: list, sub_fsm=None):
        self._prompts = prompts
        self._sub_fsm = sub_fsm

    @abstractmethod
    def on_enter(self) -> None:
        """Вызывается при входе в состояние"""
        pass

    @abstractmethod
    def on_exit(self) -> None:
        """Вызывается при выходе из состояния"""
        pass

    @abstractmethod
    def handle_event(self, event: MitaEvents | PlayerEvents) -> 'BaseState':
        """Обработка событий, возвращает State"""
        pass

    def process_logic(self, message_text):
        """Вычленяет команды из текста, а затем в соответствии с этим реагирует"""
        if self._sub_fsm:
            self._sub_fsm.process_logic(message_text)
        else:
            # parsed_command = CommandParser.parse(message_text)
            # if parsed_command:
            #     self.handle_event(parsed_command)
            pass

    def get_prompts_text(self):
        combined_text = ""
        for prompt in self._prompts:
            if prompt.active:
                combined_text += str(prompt)

    def get_params(self, response, tag="state", split='|'):
        """
        Получает массив строк-параметров из сообщения
        """
        start_tag = f"<{tag}>"
        end_tag = f"</{tag}>"

        if start_tag in response and end_tag in response:
            # Извлекаем изменения переменных
            start_index = response.index(start_tag) + len(start_tag)
            end_index = response.index(end_tag, start_index)
            changes_str = response[start_index:end_index]

            # Разделяем строку на отдельные значения
            params = [float(x.strip()) for x in changes_str.split(split)]

            return params

        return []
