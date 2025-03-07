from abc import ABC, abstractmethod

from Events.MitaEvents import MitaEvents
from Events.PlayerEvents import PlayerEvents


class BaseState(ABC):
    """Абстрактный класс для состояний"""
    def __init__(self, prompts=None, sub_fsm=None):
        self.prompts = prompts
        self.sub_fsm = sub_fsm

    @abstractmethod
    async def on_enter(self) -> None:
        """Вызывается при входе в состояние"""
        pass

    @abstractmethod
    async def on_exit(self) -> None:
        """Вызывается при выходе из состояния"""
        pass

    @abstractmethod
    async def handle_event(self, event: MitaEvents | PlayerEvents) -> 'BaseState':
        # if event == MitaEvents.StartHunt:
        #     return MitaMurderState()

        """Обработка событий, возвращает State"""
        pass
