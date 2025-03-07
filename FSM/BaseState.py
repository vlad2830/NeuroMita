from abc import ABC, abstractmethod

from Events.MitaEvents import MitaEvents
from Events.PlayerEvents import PlayerEvents
from promptPart import PromptPart


class BaseState(ABC):
    """Абстрактный класс для состояний"""

    def __init__(self, prompts: list = None, sub_fsm=None):
        self.prompts = prompts
        self.sub_fsm = sub_fsm

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
        # if event == MitaEvents.StartHunt:
        #     return MitaMurderState()

        """Обработка событий, возвращает State"""
        pass

    @abstractmethod
    def process_logic(self, message_text):
        """Вычленяет команды из текста, а затем в соответствии с этим реагирует"""
        if self.sub_fsm:
            self.sub_fsm.process_logic(message_text)
        else:
            pass

    def get_prompts_text(self):
        combined_text = ""
        for prompt in self.prompts:
            if prompt.active:
                combined_text += str(prompt)

